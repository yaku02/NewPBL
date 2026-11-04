using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.InputSystem;
using Unity.WebRTC;

public class OpenAIRealtime : MonoBehaviour
{
    [Header("音声設定")]
    [SerializeField] private AudioSource inputAudioSource;
    [SerializeField] private AudioSource outputAudioSource;

    [Header("AI設定")]
    [TextArea(3, 10)]
    [SerializeField]
    private string systemPrompt =
        "あなたは探偵ゲームのAI相棒です。" +
        "親しみやすく自然な日本語で会話してください。" +
        "プレイヤーと協力して事件を推理してください。" +
        "プレイヤーが「こっちに来て」「こっち来て」など、" +
        "自分のところへ来るように頼んだ場合は、" +
        "必ずcome_to_player機能を呼び出してください。" +
        "単に言葉で返事をするだけでなく、機能を実行してください。" +
        "それ以外の会話では普通に返答してください。";

    [Header("目的地生成設定")]
    [SerializeField] private DestinationSpawner destinationSpawner;

    // Inspectorに以前のsystemPromptが保存されていても、
    // 教授の家に関するルールを必ずInstructionsへ追加する。
    private const string DestinationInstructions =
        "\n\n【目的地の決定ルール】" +
        "\nプレイヤーと協力して事件を推理してください。" +
        "\n事件の情報や証拠を検討し、教授の家に行くことが有効な捜査方針だと判断した場合は、まずプレイヤーに提案してください。" +
        "\nプレイヤーと相談した結果、教授の家に行く方針が決定した場合に限り、spawn_destination機能を呼び出してください。" +
        "\n単に「教授の家」という言葉が会話に出ただけでは、spawn_destinationを呼び出してはいけません。" +
        "\n教授の家に行くことをまだ検討している段階や、プレイヤーが同意していない段階では呼び出してはいけません。" +
        "\n教授の家の生成を一度依頼した後は、同じ目的地について何度もspawn_destinationを呼び出さないでください。" +
        "\nspawn_destinationを呼び出すときは、destination_idにprofessor_houseを指定してください。";

    private RTCPeerConnection peerConnection;
    private AudioStreamTrack audioTrack;
    private RTCDataChannel dataChannel;

    private bool isConnected;
    private bool isConnecting;
    private bool microphoneStarted;
    private bool isWaitingForMicrophone;

    private string currentMicDevice;

    // Buddy2Voiceが購読するイベント
    public event Action OnComeToPlayerRequested;

    private const string ServerUrl =
        "http://localhost:3000/session";

    private void Start()
    {
        if (inputAudioSource != null)
        {
            inputAudioSource.playOnAwake = false;
            inputAudioSource.loop = true;
        }

        if (outputAudioSource != null)
        {
            outputAudioSource.playOnAwake = false;
            outputAudioSource.loop = true;
        }

        Debug.Log(
            "Lキー：接続 / Oキー：マイク開始・停止 / Pキー：証拠送信"
        );
    }

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.lKey.wasPressedThisFrame)
        {
            if (!isConnected && !isConnecting)
                StartCoroutine(ConnectCoroutine());
            else
                Debug.Log("接続済み、または接続処理中です。");
        }

        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            if (!isConnected)
            {
                Debug.LogWarning("先にLキーで接続してください。");
            }
            else if (!microphoneStarted &&
                     !isWaitingForMicrophone)
            {
                StartMicrophone();
            }
            else if (microphoneStarted)
            {
                StopMicrophone();
            }
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            SendEvidence();
        }
    }

    private void StartMicrophone()
    {
        if (inputAudioSource == null)
        {
            Debug.LogError(
                "Input Audio Sourceが設定されていません。"
            );
            return;
        }

        string[] devices = Microphone.devices;

        if (devices == null || devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません。");
            return;
        }

        currentMicDevice = devices[0];
        Debug.Log("使用するマイク：" + currentMicDevice);

        AudioClip micClip = Microphone.Start(
            currentMicDevice, true, 10, 48000
        );

        if (micClip == null)
        {
            Debug.LogError("マイク録音を開始できませんでした。");
            return;
        }

        inputAudioSource.Stop();
        inputAudioSource.clip = micClip;
        inputAudioSource.loop = true;

        microphoneStarted = true;
        isWaitingForMicrophone = true;

        Debug.Log("マイク録音を開始しました。");

        StartCoroutine(WaitForMicrophone());
    }

    private IEnumerator WaitForMicrophone()
    {
        float elapsed = 0f;
        const float timeout = 5f;

        while (microphoneStarted &&
               Microphone.GetPosition(currentMicDevice) <= 0)
        {
            if (elapsed >= timeout)
            {
                Debug.LogError(
                    "マイクの録音位置が進みません。"
                );
                StopMicrophone();
                yield break;
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!microphoneStarted)
        {
            isWaitingForMicrophone = false;
            yield break;
        }

        inputAudioSource.Play();
        isWaitingForMicrophone = false;

        Debug.Log(
            "マイク入力を開始しました。録音位置：" +
            Microphone.GetPosition(currentMicDevice)
        );
    }

    private void StopMicrophone()
    {
        if (inputAudioSource != null)
            inputAudioSource.Stop();

        if (!string.IsNullOrEmpty(currentMicDevice) &&
            Microphone.IsRecording(currentMicDevice))
        {
            Microphone.End(currentMicDevice);
        }

        microphoneStarted = false;
        isWaitingForMicrophone = false;

        Debug.Log("マイクを停止しました。");
    }

    private IEnumerator ConnectCoroutine()
    {
        isConnecting = true;
        Debug.Log("OpenAI Realtimeに接続しています...");

        if (inputAudioSource == null ||
            outputAudioSource == null)
        {
            Debug.LogError(
                "Input Audio SourceとOutput Audio Sourceを設定してください。"
            );

            isConnecting = false;
            yield break;
        }

        var configuration = new RTCConfiguration();
        peerConnection = new RTCPeerConnection(ref configuration);

        peerConnection.OnTrack = e =>
        {
            if (e.Track is AudioStreamTrack receivedAudioTrack)
            {
                Debug.Log("AIの音声トラックを受信しました。");

                outputAudioSource.SetTrack(receivedAudioTrack);
                outputAudioSource.loop = true;
                outputAudioSource.Play();
            }
        };

        peerConnection.OnConnectionStateChange = state =>
        {
            Debug.Log("WebRTC接続状態：" + state);

            if (state == RTCPeerConnectionState.Connected)
            {
                isConnected = true;
                isConnecting = false;
                Debug.Log("OpenAI Realtimeに接続しました。");
            }
            else if (
                state == RTCPeerConnectionState.Failed ||
                state == RTCPeerConnectionState.Closed ||
                state == RTCPeerConnectionState.Disconnected)
            {
                isConnected = false;
                isConnecting = false;
                Debug.LogWarning("WebRTC接続が切断されました。");
            }
        };

        audioTrack = new AudioStreamTrack(inputAudioSource);
        peerConnection.AddTrack(audioTrack);

        dataChannel = peerConnection.CreateDataChannel("oai-events");

        dataChannel.OnOpen = () =>
        {
            Debug.Log("データチャンネルが接続されました。");
            SendSessionUpdate();
        };

        dataChannel.OnMessage = bytes =>
        {
            string message = Encoding.UTF8.GetString(bytes);
            HandleServerEvent(message);
        };

        var offerOperation = peerConnection.CreateOffer();
        yield return offerOperation;

        if (offerOperation.IsError)
        {
            Debug.LogError(
                "Offer作成失敗：" + offerOperation.Error.message
            );
            isConnecting = false;
            yield break;
        }

        var offer = offerOperation.Desc;
        var localOperation =
            peerConnection.SetLocalDescription(ref offer);

        yield return localOperation;

        if (localOperation.IsError)
        {
            Debug.LogError(
                "LocalDescription設定失敗：" +
                localOperation.Error.message
            );
            isConnecting = false;
            yield break;
        }

        while (peerConnection.GatheringState !=
               RTCIceGatheringState.Complete)
        {
            yield return null;
        }

        string json = JsonUtility.ToJson(new SessionRequest
        {
            sdp = peerConnection.LocalDescription.sdp,
            type = peerConnection.LocalDescription.type.ToString()
        });

        using (UnityWebRequest request =
               new UnityWebRequest(ServerUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);

            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader(
                "Content-Type", "application/json"
            );

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Node.js接続失敗：" + request.error);
                Debug.LogError(request.downloadHandler.text);
                isConnecting = false;
                yield break;
            }

            string answerSdp = request.downloadHandler.text;

            if (string.IsNullOrWhiteSpace(answerSdp))
            {
                Debug.LogError("OpenAIからSDP応答が返っていません。");
                isConnecting = false;
                yield break;
            }

            RTCSessionDescription remoteDescription =
                new RTCSessionDescription
                {
                    type = RTCSdpType.Answer,
                    sdp = answerSdp
                };

            var remoteOperation =
                peerConnection.SetRemoteDescription(
                    ref remoteDescription
                );

            yield return remoteOperation;

            if (remoteOperation.IsError)
            {
                Debug.LogError(
                    "RemoteDescription設定失敗：" +
                    remoteOperation.Error.message
                );
                isConnecting = false;
                yield break;
            }

            Debug.Log("接続応答を設定しました。");
        }
    }

    // Realtime APIのセッション設定とFunction Callingを登録する
    private void SendSessionUpdate()
    {
        if (dataChannel == null ||
            dataChannel.ReadyState != RTCDataChannelState.Open)
        {
            Debug.LogWarning(
                "データチャンネルが開いていません。"
            );
            return;
        }

        string instructions = systemPrompt + DestinationInstructions;

        string json =
            "{\"type\":\"session.update\",\"session\":{" +
            "\"type\":\"realtime\"," +
            "\"instructions\":\"" + EscapeJson(instructions) + "\"," +
            "\"audio\":{" +
                "\"input\":{" +
                    "\"turn_detection\":{" +
                        "\"type\":\"server_vad\"," +
                        "\"create_response\":true," +
                        "\"interrupt_response\":true" +
                    "}" +
                "}," +
                "\"output\":{" +
                    "\"voice\":\"sage\"" +
                "}" +
            "}," +
            "\"tools\":[" +
                "{" +
                    "\"type\":\"function\"," +
                    "\"name\":\"come_to_player\"," +
                    "\"description\":\"プレイヤーが相棒を自分のところへ呼んだときに実行する。\"," +
                    "\"parameters\":{" +
                        "\"type\":\"object\"," +
                        "\"properties\":{}," +
                        "\"required\":[]," +
                        "\"additionalProperties\":false" +
                    "}" +
                "}," +
                "{" +
                    "\"type\":\"function\"," +
                    "\"name\":\"spawn_destination\"," +
                    "\"description\":\"プレイヤーと相談した結果、教授の家に行く方針が決定した場合に、教授の家の目的地オブジェクトをUnityに生成させる。\"," +
                    "\"parameters\":{" +
                        "\"type\":\"object\"," +
                        "\"properties\":{" +
                            "\"destination_id\":{" +
                                "\"type\":\"string\"," +
                                "\"enum\":[\"professor_house\"]," +
                                "\"description\":\"生成する目的地。教授の家の場合はprofessor_houseを指定する。\"" +
                            "}" +
                        "}," +
                        "\"required\":[\"destination_id\"]," +
                        "\"additionalProperties\":false" +
                    "}" +
                "}" +
            "]," +
            "\"tool_choice\":\"auto\"" +
            "}}";

        dataChannel.Send(json);

        Debug.Log(
            "音声会話・come_to_player・spawn_destination機能を設定しました。"
        );
    }

    private void SendEvidence()
    {
        if (dataChannel == null ||
            dataChannel.ReadyState != RTCDataChannelState.Open)
        {
            Debug.LogWarning("AIに接続してからPキーを押してください。");
            return;
        }

        string evidence =
            "事件現場で赤い手袋が発見されました。";

        string json =
            "{\"type\":\"conversation.item.create\",\"item\":{" +
            "\"type\":\"message\",\"role\":\"user\",\"content\":[" +
            "{\"type\":\"input_text\",\"text\":\"" +
            EscapeJson(evidence) + "\"}]}}";

        dataChannel.Send(json);
        dataChannel.Send("{\"type\":\"response.create\"}");

        Debug.Log("証拠情報をAIに送信しました。");
    }

    private void HandleServerEvent(string message)
    {
        RealtimeServerEvent serverEvent;

        try
        {
            serverEvent =
                JsonUtility.FromJson<RealtimeServerEvent>(message);
        }
        catch (Exception e)
        {
            Debug.LogWarning("イベント解析失敗：" + e.Message);
            return;
        }

        if (serverEvent == null)
            return;

        if (serverEvent.type == "error")
        {
            Debug.LogError("OpenAIエラー：" + message);
            return;
        }

        if (serverEvent.type == "session.updated")
        {
            Debug.Log("Realtimeセッション設定が適用されました。");
        }
        else if (
            serverEvent.type ==
            "response.function_call_arguments.done")
        {
            Debug.Log("Function Callingのイベントを受信");
            HandleFunctionCall(serverEvent);
        }
    }

    private void HandleFunctionCall(RealtimeServerEvent serverEvent)
    {
        if (serverEvent.name == "come_to_player")
        {
            Debug.Log("AI命令：プレイヤーのところへ移動");

            OnComeToPlayerRequested?.Invoke();

            SendFunctionCallOutput(
                serverEvent.call_id,
                "プレイヤーのところへ移動する命令をUnityに送信しました。"
            );

            return;
        }

        if (serverEvent.name == "spawn_destination")
        {
            Debug.Log("AI命令：目的地オブジェクトを生成");

            DestinationArguments arguments = null;

            try
            {
                arguments =
                    JsonUtility.FromJson<DestinationArguments>(
                        serverEvent.arguments
                    );
            }
            catch (Exception e)
            {
                Debug.LogError("目的地引数の解析に失敗：" + e.Message);
            }

            if (arguments == null ||
                arguments.destination_id != "professor_house")
            {
                Debug.LogWarning("不正な目的地IDです：" + serverEvent.arguments);

                SendFunctionCallOutput(
                    serverEvent.call_id,
                    "目的地IDが不正なため、オブジェクトを生成できませんでした。"
                );

                return;
            }

            if (destinationSpawner == null)
            {
                Debug.LogError(
                    "DestinationSpawnerがInspectorに設定されていません。"
                );

                SendFunctionCallOutput(
                    serverEvent.call_id,
                    "Unity側にDestinationSpawnerが設定されていないため、生成に失敗しました。"
                );

                return;
            }

            bool success = destinationSpawner.SpawnProfessorHouse();

            SendFunctionCallOutput(
                serverEvent.call_id,
                success
                    ? "教授の家の目的地オブジェクトをUnityに生成しました。"
                    : "教授の家の生成に失敗しました。UnityのPrefabと生成位置の設定を確認してください。"
            );

            return;
        }

        Debug.LogWarning("未対応のAI機能：" + serverEvent.name);

        SendFunctionCallOutput(
            serverEvent.call_id,
            "未対応の機能名です。"
        );
    }

    // Function Callingの実行結果をRealtime APIへ返す
    private void SendFunctionCallOutput(string callId, string output)
    {
        if (dataChannel == null ||
            dataChannel.ReadyState != RTCDataChannelState.Open)
        {
            Debug.LogWarning(
                "Function Callingの結果を返せません。データチャンネルが開いていません。"
            );
            return;
        }

        string outputJson =
            "{\"type\":\"conversation.item.create\"," +
            "\"item\":{" +
                "\"type\":\"function_call_output\"," +
                "\"call_id\":\"" + EscapeJson(callId) + "\"," +
                "\"output\":\"" + EscapeJson(output) + "\"" +
            "}}";

        dataChannel.Send(outputJson);
        dataChannel.Send("{\"type\":\"response.create\"}");

        Debug.Log("Function Callingの実行結果をAIに返しました。");
    }

    private string EscapeJson(string value)
    {
        if (value == null)
            return "";

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private void OnDestroy()
    {
        StopMicrophone();

        if (dataChannel != null)
        {
            dataChannel.Close();
            dataChannel.Dispose();
            dataChannel = null;
        }

        if (peerConnection != null)
        {
            peerConnection.Close();
            peerConnection.Dispose();
            peerConnection = null;
        }

        if (audioTrack != null)
        {
            audioTrack.Dispose();
            audioTrack = null;
        }
    }

    [Serializable]
    private class SessionRequest
    {
        public string sdp;
        public string type;
    }

    [Serializable]
    private class RealtimeServerEvent
    {
        public string type;
        public string name;
        public string call_id;
        public string arguments;
    }

    [Serializable]
    private class DestinationArguments
    {
        public string destination_id;
    }
}
