using UnityEngine;

public class PlayerFootstep : MonoBehaviour
{
    [Header("足音の設定")]
    [Tooltip("ペタペタと連続で鳴る5秒ほどのAudioClip")]
    [SerializeField] private AudioClip footstepLoopSound;

    [Tooltip("移動とみなす最小速度")]
    [SerializeField] private float moveThreshold = 0.1f;

    [Header("参照設定")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private AudioSource audioSource;

    private Vector3 lastPosition;

    private void Start()
    {
        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // --- ループ再生用の設定 ---
        audioSource.clip = footstepLoopSound;
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        audioSource.pitch = 2.0f;

        // ★ 音声ピッチが Time.timeScale（コマ送り等）の影響を受けないようにする設定
        audioSource.ignoreListenerPause = false;

        lastPosition = transform.position;
    }

    private void Update()
    {
        if (audioSource == null || audioSource.clip == null) return;

        // 実際の移動速度を計算（水平移動のみ）
        Vector3 currentPosition = transform.position;
        Vector3 horizontalMove = new Vector3(currentPosition.x - lastPosition.x, 0, currentPosition.z - lastPosition.z);
        float currentSpeed = horizontalMove.magnitude / Time.deltaTime;

        lastPosition = currentPosition;

        // 接地判定
        bool isGrounded = characterController == null || characterController.isGrounded;

        // 接地していて、一定速度以上で歩いている場合
        if (isGrounded && currentSpeed > moveThreshold)
        {
            // 音が鳴っていなければ再生を開始
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // 立ち止まったら再生を停止（Pauseにしておくと歩き出しの繋がりが自然になります）
            if (audioSource.isPlaying)
            {
                audioSource.Pause();
            }
        }
    }
}