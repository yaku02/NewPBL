using UnityEngine;

public class SEManager : MonoBehaviour
{
    // ★ シングルトン用のインスタンス（どこからでも SEManager.Instance でアクセス可能）
    public static SEManager Instance { get; private set; }

    [Header("サウンド設定")]
    [SerializeField] private AudioClip getItemSound; // アイテム入手時の音
    [SerializeField] private AudioClip talkBuddySound;
    [SerializeField] private AudioClip errorSound; // エラー時の音

    [Header("参照")]
    [SerializeField] private AudioSource audioSource; // 再生用コンポーネント

    private void Awake()
    {
        // ★ シングルトンの初期化と重複チェック
        if (Instance == null)
        {
            Instance = this;
            // シーンを跨いでSEManagerを維持したい場合は下の行のコメントを解除してください
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            // すでにシーン内に存在する場合は重複防止のため自身を破棄
            Destroy(gameObject);
            return;
        }

        // AudioSourceが未設定の場合、自身からコンポーネントを取得
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            // それでも無ければ自動追加する
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }

    /// <summary>
    /// インタラクト用サウンドを再生する
    /// </summary>
    public void PlayGetItemSound()
    {
        PlaySE(getItemSound);
    }

    public void PlayTalkBuddySound()
    {
        PlaySE(talkBuddySound);
    }

    public void PlayErrorSound()
    {
        PlaySE(errorSound);
    }

    /// <summary>
    /// 汎用的なSE再生メソッド
    /// </summary>
    public void PlaySE(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("再生しようとした AudioClip が null です！", this);
            return;
        }

        if (audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
}