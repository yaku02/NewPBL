using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [Header("BGM設定")]
    [SerializeField] private AudioSource bgmAudioSource;
    [SerializeField] private AudioClip defaultBGM;
    [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.5f;

    // 現在の音量（外部参照用）
    public float BGMVolume => bgmVolume;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (bgmAudioSource == null)
        {
            bgmAudioSource = gameObject.AddComponent<AudioSource>();
        }

        bgmAudioSource.loop = true;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.volume = bgmVolume;
    }

    private void Start()
    {
        if (defaultBGM != null)
        {
            PlayBGM(defaultBGM);
        }
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmAudioSource == null) return;
        if (bgmAudioSource.clip == clip && bgmAudioSource.isPlaying) return;

        bgmAudioSource.clip = clip;
        bgmAudioSource.Play();
    }

    public void StopBGM()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
        }
    }

    /// <summary>
    /// BGMの音量を変更します（UIスライダーから呼ばれる）
    /// </summary>
    public void SetVolume(float volume)
    {
        bgmVolume = Mathf.Clamp01(volume);
        if (bgmAudioSource != null)
        {
            bgmAudioSource.volume = bgmVolume;
        }
    }
}