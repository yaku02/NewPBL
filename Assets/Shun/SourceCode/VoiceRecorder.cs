using UnityEngine;

public class VoiceRecorder : MonoBehaviour
{
    [Header("録音設定")]
    [Tooltip("録音する最大時間（秒）")]
    [SerializeField] private int maxRecordingTime = 60;

    [Tooltip("サンプリングレート（標準は 44100Hz）")]
    [SerializeField] private int sampleRate = 44100;

    // 録音状態の保持
    private AudioClip recordedClip;
    private string deviceName;
    private bool isRecording = false;

    // 外部から録音データを取り出すためのプロパティ
    public AudioClip RecordedClip => recordedClip;
    public bool IsRecording => isRecording;

    void Start()
    {
        // 利用可能なマイクがあるか確認
        if (Microphone.devices.Length > 0)
        {
            // デフォルトのマイク（最初のデバイス）を使用
            deviceName = Microphone.devices[0];
        }
        else
        {
            Debug.LogError("使用可能なマイクが見つかりません。");
        }
    }

    /// <summary>
    /// UIボタンの OnClick から呼び出す：録音を開始する
    /// </summary>
    public void StartRecording()
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            Debug.LogWarning("マイクが認識されていません。");
            return;
        }

        if (isRecording)
        {
            Debug.LogWarning("すでに録音中です。");
            return;
        }

        // 録音を開始 (デバイス名, ループするか, 長さ(秒), サンプリングレート)
        recordedClip = Microphone.Start(deviceName, false, maxRecordingTime, sampleRate);
        isRecording = true;
        Debug.Log("録音を開始しました...");
    }

    /// <summary>
    /// UIボタンの OnClick から呼び出す：録音を停止する
    /// </summary>
    public void StopRecording()
    {
        if (!isRecording)
        {
            Debug.LogWarning("録音中ではありません。");
            return;
        }

        // 現在の録音位置（サンプリング数）を取得
        int lastPosition = Microphone.GetPosition(deviceName);

        // マイク停止
        Microphone.End(deviceName);
        isRecording = false;

        if (lastPosition <= 0)
        {
            Debug.LogWarning("音声が録音されませんでした。");
            return;
        }

        // 実際に録音した長さに AudioClip を切り詰める処理
        AudioClip trimmedClip = AudioClip.Create("RecordedVoice", lastPosition, recordedClip.channels, sampleRate, false);
        float[] soundData = new float[lastPosition * recordedClip.channels];
        recordedClip.GetData(soundData, 0);
        trimmedClip.SetData(soundData, 0);

        // トリムした AudioClip を保存
        recordedClip = trimmedClip;

        Debug.Log($"録音を停止しました。録音時間: {(float)lastPosition / sampleRate}秒");
    }

    /// <summary>
    /// UIボタンの OnClick から呼び出す：録音した音声を再生する
    /// </summary>
    public void PlayRecording(AudioSource audioSource)
    {
        if (recordedClip == null)
        {
            Debug.LogWarning("再生する録音データがありません。");
            return;
        }

        if (audioSource != null)
        {
            audioSource.clip = recordedClip;
            audioSource.Play();
            Debug.Log("録音データを再生します。");
        }
    }
}