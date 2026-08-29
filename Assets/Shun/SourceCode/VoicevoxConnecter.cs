using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class VoicevoxConnecter : MonoBehaviour
{
    [Header("VOICEVOX 設定")]
    [SerializeField] private string voicevoxUrl = "http://localhost:50021";
    [SerializeField] private int speakerId = 3; // スピーカーID（例: 3 = ずんだもん ノーマル）
    [SerializeField] private AudioSource audioSource;

    /// <summary>
    /// Geminiが返したテキストをVOICEVOXで読み上げる
    /// </summary>
    public void PlayVoice(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        StartCoroutine(SpeakCoroutine(text));
    }

    private IEnumerator SpeakCoroutine(string text)
    {
        // --- 1. Audio Query の生成 ---
        string encodedText = UnityWebRequest.EscapeURL(text);
        string queryUrl = $"{voicevoxUrl}/audio_query?text={encodedText}&speaker={speakerId}";

        using (UnityWebRequest queryRequest = UnityWebRequest.PostWwwForm(queryUrl, ""))
        {
            yield return queryRequest.SendWebRequest();

            if (queryRequest.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"AudioQuery エラー: {queryRequest.error}");
                yield break;
            }

            string queryJson = queryRequest.downloadHandler.text;

            // --- 2. 音声合成 (Synthesis) ---
            string synthesisUrl = $"{voicevoxUrl}/synthesis?speaker={speakerId}";
            using (UnityWebRequest synthRequest = new UnityWebRequest(synthesisUrl, "POST"))
            {
                byte[] jsonBytes = Encoding.UTF8.GetBytes(queryJson);
                synthRequest.uploadHandler = new UploadHandlerRaw(jsonBytes);
                synthRequest.downloadHandler = new DownloadHandlerAudioClip(synthesisUrl, AudioType.WAV);
                synthRequest.SetRequestHeader("Content-Type", "application/json");

                yield return synthRequest.SendWebRequest();

                if (synthRequest.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogError($"Synthesis エラー: {synthRequest.error}");
                    yield break;
                }

                // --- 3. 音声の再生 ---
                AudioClip clip = DownloadHandlerAudioClip.GetContent(synthRequest);
                if (audioSource != null && clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                }
            }
        }
    }
}