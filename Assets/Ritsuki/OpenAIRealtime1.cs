
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
        "プレイヤーと協力して事件を推理してください。";

    private RTCPeerConnection peerConnection;
    private AudioStreamTrack audioTrack;
    private RTCDataChannel dataChannel;

    private bool isConnected;
    private bool isConnecting;
    private bool microphoneStarted;

    private const string ServerUrl = "http://localhost:3000/session";

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

        Debug.Log("Lキー：接続 / Oキー：マイク開始・停止 / Pキー：証拠送信");
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
                Debug.LogWarning("先にLキーで接続してください。");
            else if (!microphoneStarted)
                StartMicrophone();
            else
                StopMicrophone();
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
            SendEvidence();
    }

    private void StartMicrophone()
    {
        if (inputAudioSource == null)
        {
            Debug.LogError("Input Audio Sourceが設定されていません。");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません。");
            return;
        }

        string device = Microphone.devices[0];

        inputAudioSource.clip = Microphone.Start(device, true, 10, 48000);

        if (inputAudioSource.clip == null)
        {
            Debug.LogError("マイクの録音を開始できませんでした。");
            return;
        }

        inputAudioSource.loop = true;
        inputAudioSource.Play();
        microphoneStarted = true;

        Debug.Log("マイクを開始しました。Oキーで停止します。");
    }

    private void StopMicrophone()
    {
        if (inputAudioSource != null)
            inputAudioSource.Stop();

        if (Microphone.IsRecording(null))
            Microphone.End(null);

        microphoneStarted = false;
        Debug.Log("マイクを停止しました。");
    }

    private IEnumerator ConnectCoroutine()
    {
        isConnecting = true;
        Debug.Log("OpenAI Realtimeに接続しています...");

        var configuration = new RTCConfiguration();
        peerConnection = new RTCPeerConnection(ref configuration);

        peerConnection.OnTrack = e =>
        {
            if (e.Track is AudioStreamTrack receivedAudioTrack)
            {
                Debug.Log("AIの音声トラックを受信しました。");

                if (outputAudioSource != null)
                {
                    outputAudioSource.SetTrack(receivedAudioTrack);
                    outputAudioSource.loop = true;
                    outputAudioSource.Play();
                }
            }
        };

        peerConnection.OnConnectionStateChange = state =>
        {
            Debug.Log("WebRTC接続状態：" + state);

            if (state == RTCPeerConnectionState.Connected)
            {
                isConnected = true;
                isConnecting = false;
            }
            else if (
                state == RTCPeerConnectionState.Failed ||
                state == RTCPeerConnectionState.Closed ||
                state == RTCPeerConnectionState.Disconnected)
            {
                isConnected = false;
            }
        };

        if (inputAudioSource == null || outputAudioSource == null)
        {
            Debug.LogError("音声用AudioSourceをInspectorで設定してください。");
            isConnecting = false;
            yield break;
        }

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
            Debug.Log("AIイベント：" + message);
            HandleServerEvent(message);
        };

        var offerOperation = peerConnection.CreateOffer();
        yield return offerOperation;

        if (offerOperation.IsError)
        {
            Debug.LogError("Offer作成失敗：" + offerOperation.Error.message);
            isConnecting = false;
            yield break;
        }

        var offer = offerOperation.Desc;
        var localOperation = peerConnection.SetLocalDescription(ref offer);
        yield return localOperation;

        if (localOperation.IsError)
        {
            Debug.LogError("LocalDescription設定失敗：" + localOperation.Error.message);
            isConnecting = false;
            yield break;
        }

        while (peerConnection.GatheringState != RTCIceGatheringState.Complete)
            yield return null;

        string json = JsonUtility.ToJson(new SessionRequest
        {
            sdp = peerConnection.LocalDescription.sdp,
            type = peerConnection.LocalDescription.type.ToString()
        });

        using (UnityWebRequest request = new UnityWebRequest(ServerUrl, "POST"))
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

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
                peerConnection.SetRemoteDescription(ref remoteDescription);

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

    private void SendSessionUpdate()
    {
        if (dataChannel == null ||
            dataChannel.ReadyState != RTCDataChannelState.Open)
            return;

        string json =
            "{\"type\":\"session.update\",\"session\":{" +
            "\"type\":\"realtime\"," +
            "\"instructions\":\"" + EscapeJson(systemPrompt) + "\"," +
            "\"audio\":{\"output\":{\"voice\":\"sage\"}}" +
            "}}";

        dataChannel.Send(json);
        Debug.Log("AI設定を送信しました。");
    }

    private void SendEvidence()
    {
        if (dataChannel == null ||
            dataChannel.ReadyState != RTCDataChannelState.Open)
        {
            Debug.LogWarning("AIに接続してからPキーを押してください。");
            return;
        }

        string evidence = "事件現場で赤い手袋が発見されました。";

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
        if (message.Contains("\"type\":\"error\""))
            Debug.LogError("OpenAIからエラーイベントが届きました：" + message);
        else if (message.Contains("\"type\":\"response.done\""))
            Debug.Log("AIの応答が完了しました。");
    }

    private string EscapeJson(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r");
    }

    private void OnDestroy()
    {
        StopMicrophone();

        if (dataChannel != null)
        {
            dataChannel.Close();
            dataChannel.Dispose();
        }

        if (peerConnection != null)
        {
            peerConnection.Close();
            peerConnection.Dispose();
        }

        if (audioTrack != null)
            audioTrack.Dispose();
    }

    [Serializable]
    private class SessionRequest
    {
        public string sdp;
        public string type;
    }
}