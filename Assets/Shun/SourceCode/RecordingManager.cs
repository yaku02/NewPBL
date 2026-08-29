using UnityEngine;

public class RecordingManager : MonoBehaviour
{
    [SerializeField] private VoiceRecorder recorder;
    [SerializeField] private GeminiConnecter geminiConnecter;
    [SerializeField] private VoicevoxConnecter voicevoxConnecter;

    // 録音停止ボタンや処理から呼ぶ例
    public void OnStopAndSendToGemini()
    {
        // 1. 録音を停止
        recorder.StopRecording();

        // 2. 録音した AudioClip を取得
        AudioClip voiceClip = recorder.RecordedClip;

        if (voiceClip != null)
        {
            // 3. Gemini へ送信
            string prompt = "この音声を聞いて日本語で返答してください。";

            geminiConnecter.SendAudioToGemini(
                voiceClip,
                prompt,
                onSuccess: (jsonResponse) =>
                {
                    Debug.Log("成功！ レスポンス: " + jsonResponse);

                    // --- 4. JSON をパースしてテキストを取り出す ---
                    try
                    {
                        GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(jsonResponse);
                        string replyText = responseData.candidates[0].content.parts[0].text;

                        Debug.Log("Geminiの返答テキスト: " + replyText);

                        // --- 5. VOICEVOX で読み上げを実行 ---
                        if (voicevoxConnecter != null)
                        {
                            voicevoxConnecter.PlayVoice(replyText);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError("JSONのパースまたは読み上げ呼び出しに失敗しました: " + ex.Message);
                    }
                },
                onError: (error) =>
                {
                    Debug.LogError("失敗: " + error);
                }
            );
        }
    }
}

// ==========================================
// JsonUtility でパースするためのデータ構造クラス
// (RecordingManager の外側、または別ファイルに定義)
// ==========================================
[System.Serializable]
public class GeminiResponse
{
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate
{
    public Content content;
}

[System.Serializable]
public class Content
{
    public Part[] parts;
}

[System.Serializable]
public class Part
{
    public string text;
}