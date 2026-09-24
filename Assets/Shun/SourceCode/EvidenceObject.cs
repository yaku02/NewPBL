using UnityEngine;
using UnityEngine.InputSystem;

public class EvidenceObject : MonoBehaviour
{
    [Header("証拠品情報")]
    [SerializeField] private string description = "";
    public string Description => description;

    // ★ UI表示用の画像を持たせる変数（PNGなどの画像アセット）
    [SerializeField] private Sprite evidenceIcon;
    public Sprite EvidenceIcon => evidenceIcon;

    [Header("参照・設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject interactCanvas;
    [SerializeField] private GameObject resultCanvas;
    [SerializeField] private Key interactKey = Key.G;
    [SerializeField] private ParticleSystem targetParticle;
    private bool hasInteracted = false; // インタラクト完了フラグ

    [Header("Distance Settings")]
    [SerializeField] private float interactDistance = 1.5f;

    private float sqrInteractDistance;

    private void Start()
    {
        sqrInteractDistance = interactDistance * interactDistance;

        if (interactCanvas != null)
        {
            interactCanvas.SetActive(false);
        }
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (playerCamera == null) return;

        // 二乗距離計算
        float sqrDistance = (playerCamera.position - transform.position).sqrMagnitude;
        bool isCloseEnough = sqrDistance <= sqrInteractDistance;

        if (!hasInteracted)
        {
            // --- まだインタラクトしていない時 ---
            if (interactCanvas != null && interactCanvas.activeSelf != isCloseEnough)
            {
                interactCanvas.SetActive(isCloseEnough);
            }

            // Gキー入力判定
            if (isCloseEnough && Keyboard.current != null && Keyboard.current[interactKey].wasPressedThisFrame)
            {
                Interact();
            }
        }
        else
        {
            // --- インタラクト済みの時 ---
            if (resultCanvas != null && resultCanvas.activeSelf != isCloseEnough)
            {
                resultCanvas.SetActive(isCloseEnough);
            }
        }
    }

    private void Interact()
    {
        hasInteracted = true; // フラグを立てる

        if (targetParticle != null) targetParticle.gameObject.SetActive(false);
        if (interactCanvas != null) interactCanvas.SetActive(false);

        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true);

            if (resultCanvas.TryGetComponent<NyonUI>(out var nyonUI))
            {
                nyonUI.Show();
            }
            else
            {
                nyonUI = resultCanvas.GetComponentInChildren<NyonUI>();
                if (nyonUI != null) nyonUI.Show();
            }
        }

        if (EvidenceManager.Instance == null)
        {
            Debug.Log("EvidenceManagerがnullです");
        }
        else
        {
            EvidenceManager.Instance.AddEvidence(this);
        }
    }
}