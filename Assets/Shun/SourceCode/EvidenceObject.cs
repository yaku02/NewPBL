using UnityEngine;
using UnityEngine.InputSystem;

public class EvidenceObject : MonoBehaviour
{
    [SerializeField] private string description = "";
    public string Description => description;

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
            // 範囲内にいる場合のみ interactCanvas を表示
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
            // 範囲内にいる場合のみ resultCanvas を表示（離れたら SetActive(false) になる）
            if (resultCanvas != null && resultCanvas.activeSelf != isCloseEnough)
            {
                resultCanvas.SetActive(isCloseEnough);
            }
        }
    }

    private void Interact()
    {
        hasInteracted = true; // フラグを立てる

        if(targetParticle != null) targetParticle.gameObject.SetActive(false);
        // インタラクト用UIは消す
        if (interactCanvas != null) interactCanvas.SetActive(false);
        // 結果UIを表示する
        if (resultCanvas != null)
        {
            resultCanvas.SetActive(true); // オブジェクト自体をアクティブにする

            // NyonUIコンポーネントを取得してアニメーション実行
            if (resultCanvas.TryGetComponent<NyonUI>(out var nyonUI))
            {
                nyonUI.Show();
            }
            else
            {
                // もしresultCanvasの「子オブジェクト」にNyonUIがついている場合はこちら
                nyonUI = resultCanvas.GetComponentInChildren<NyonUI>();
                if (nyonUI != null) nyonUI.Show();
            }
        }
        if (EvidenceManager.Instance == null)
        {
            Debug.Log("EvidenceManagerがnullです");
        }
        
        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.AddEvidence(this);
        }
    }
}