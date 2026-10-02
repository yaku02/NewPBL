using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;

public class GeminiConnecter : MonoBehaviour
{
    [Header("API設定")]
    [SerializeField] private string apiKey = "AQ.Ab8RN6IXaTc5sGXthza14IJ8INwwbLNv9GNWSX11ScXmUedw4g";

    [Header("キャラクター設定（性格付け）")]
    [TextArea(3, 5)]
    [SerializeField] private string systemInstruction = "あなたはゲーム内の頼もしい相棒です。フレンドリーで少しフランクな口調で、1〜2文程度で短く答えてください。";

    // Gemini 3.6 Flash のエンドポイント
    private const string ApiUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.6-flash:generateContent";

    /// <summary>
    /// 録音データを Gemini に送信してテキスト応答を受け取る
    /// </summary>
    public void SendAudioToGemini(AudioClip clip, string prompt, Action<string> onSuccess = null, Action<string> onError = null)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip が null です。");
            return;
        }

        StartCoroutine(PostAudioCoroutine(clip, prompt, onSuccess, onError));
    }

    private IEnumerator PostAudioCoroutine(AudioClip clip, string prompt, Action<string> onSuccess, Action<string> onError)
    {
        Stopwatch totalSw = Stopwatch.StartNew();
        Stopwatch stepSw = Stopwatch.StartNew();

        // 1. AudioClip を WAV バイトデータに変換
        byte[] wavData = ConvertAudioClipToWav(clip);
        stepSw.Stop();
        long wavTime = stepSw.ElapsedMilliseconds;

        if (wavData == null)
        {
            onError?.Invoke("WAVへの変換に失敗しました。");
            yield break;
        }

        // 2. Base64 エンコード
        stepSw.Restart();
        string base64Audio = Convert.ToBase64String(wavData);
        stepSw.Stop();
        long base64Time = stepSw.ElapsedMilliseconds;

        // 3. 4. 5. JSON文字列の構築
        stepSw.Restart();
        StringBuilder combinedInstruction = new StringBuilder(systemInstruction);

        if (EvidenceManager.Instance != null && EvidenceManager.Instance.CollectedEvidences.Count > 0)
        {
            combinedInstruction.AppendLine("\n\n【現在把握している証拠一覧】");
            foreach (var evidence in EvidenceManager.Instance.CollectedEvidences)
            {
                if (evidence != null && !string.IsNullOrEmpty(evidence.Description))
                {
                    combinedInstruction.AppendLine($"- {evidence.Description}");
                }
            }
        }

        string escapedPrompt = prompt.Replace("\"", "\\\"").Replace("\n", "\\n");
        string escapedInstruction = combinedInstruction.ToString().Replace("\"", "\\\"").Replace("\n", "\\n");

        // maxOutputTokens を 512 に拡大して自然な返答長を確保
        string jsonRequestBody = "{" +
            "\"system_instruction\": {" +
                "\"parts\": [{\"text\": \"" + escapedInstruction + "\"}]" +
            "}," +
            "\"contents\": [{" +
                "\"parts\": [" +
                    "{\"text\": \"" + escapedPrompt + "\"}," +
                    "{\"inline_data\": {" +
                        "\"mime_type\": \"audio/wav\"," +
                        "\"data\": \"" + base64Audio + "\"" +
                    "}}" +
                "]" +
            "}]," +
            "\"generationConfig\": {" +
                "\"maxOutputTokens\": 512" +
            "}" +
        "}";
        stepSw.Stop();
        long jsonTime = stepSw.ElapsedMilliseconds;

        // 6. UnityWebRequest の作成と送信
        string requestUrl = $"{ApiUrl}?key={apiKey}";
        using (UnityWebRequest request = new UnityWebRequest(requestUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonRequestBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            Debug.Log($"[GeminiPerf] 送信準備完了 (JSONサイズ: {bodyRaw.Length / 1024f:F2} KB)。API呼び出し中...");

            stepSw.Restart();
            yield return request.SendWebRequest();
            stepSw.Stop();
            long networkTime = stepSw.ElapsedMilliseconds;

            totalSw.Stop();

            // 時間計測結果をコンソールに出力
            Debug.Log($"[GeminiPerf] --- 処理時間レポート ---\n" +
                      $"1. WAV変換     : {wavTime} ms\n" +
                      $"2. Base64変換  : {base64Time} ms\n" +
                      $"3. JSON構築    : {jsonTime} ms\n" +
                      $"4. 通信&推論   : {networkTime} ms\n" +
                      $"-----------------------------\n" +
                      $"合計所要時間   : {totalSw.ElapsedMilliseconds} ms");

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Gemini から応答を受信しました:");
                Debug.Log(request.downloadHandler.text);
                onSuccess?.Invoke(request.downloadHandler.text);
            }
            else
            {
                string errorMsg = $"エラー発生 [{request.responseCode}]: {request.error}\n{request.downloadHandler.text}";
                Debug.LogError(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    #region AudioClip to WAV Conversion Helper (Optimized)

    private byte[] ConvertAudioClipToWav(AudioClip clip)
    {
        try
        {
            float[] samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(Mathf.Clamp(samples[i], -1.0f, 1.0f) * 32767);
            }

            byte[] bytesData = new byte[intData.Length * 2];
            Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    ushort channels = (ushort)clip.channels;
                    uint sampleRate = (uint)clip.frequency;
                    ushort bitsPerSample = 16;

                    // RIFF ヘッダー
                    writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                    writer.Write((uint)(36 + bytesData.Length));
                    writer.Write(Encoding.ASCII.GetBytes("WAVE"));

                    // fmt チャンク
                    writer.Write(Encoding.ASCII.GetBytes("fmt "));
                    writer.Write((uint)16);
                    writer.Write((ushort)1);
                    writer.Write(channels);
                    writer.Write(sampleRate);
                    writer.Write((uint)(sampleRate * channels * (bitsPerSample / 8)));
                    writer.Write((ushort)(channels * (bitsPerSample / 8)));
                    writer.Write(bitsPerSample);

                    // data チャンク
                    writer.Write(Encoding.ASCII.GetBytes("data"));
                    writer.Write((uint)bytesData.Length);
                    writer.Write(bytesData);
                }
                return stream.ToArray();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"WAVE変換エラー: {ex.Message}");
            return null;
        }
    }

    #endregion
}