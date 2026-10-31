using System.Collections.Generic; // ★ これを追加
using UnityEngine;
using UnityEngine.InputSystem;

public class EvidenceObject : MonoBehaviour
{
    [Header("証拠品情報")]
    [SerializeField] private string description = "";
    public string Description => description;

    [SerializeField] private string UIdescription = "";
    public string UIDescription => UIdescription;

    // ★ UI表示用の画像を持たせる変数（PNGなどの画像アセット）
    [SerializeField] private Sprite evidenceIcon;
    public Sprite EvidenceIcon => evidenceIcon;

    [Header("入手失敗時")]
    [SerializeField] private GameObject failedCanvas;
    [SerializeField] private AudioClip failedSound;


    [Header("参照・設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private GameObject interactCanvas;
    [SerializeField] private GameObject resultCanvas;
    [SerializeField] private ParticleSystem targetParticle;

    [Header("合成設定")]
    [Tooltip("合成で手に入るアイテムの場合は true（距離判定を行わず、合成時にResultCanvasを表示）")]
    [SerializeField] private bool isCombineResult = false;

    [Tooltip("この証拠品の上にドラッグ＆ドロップできる対象（合成元）")]
    [SerializeField] private EvidenceObject combineIngredient;
    public EvidenceObject CombineIngredient => combineIngredient;

    [Tooltip("合成が成功した時に入手する新しい証拠品")]
    [SerializeField] private EvidenceObject combineResult;
    public EvidenceObject CombineResult => combineResult;

    [Header("入手条件")]
    [Tooltip("このアイテムを入手するために必要な証拠品（複数設定可能）")]
    [SerializeField] private List<EvidenceObject> requiredEvidences = new List<EvidenceObject>();

    [Header("入手成功時設定")]
    [Tooltip("条件を満たして入手した際に再生する専用サウンド（未設定の場合は即時Interactを実行します）")]
    [SerializeField] private AudioClip successSound;

    private HashSet<EvidenceObject> usedEvidences = new HashSet<EvidenceObject>();

    private bool hasInteracted = false; // インタラクト完了フラグ
    private bool hasFailed = false;

    [Header("Distance Settings")]
    [SerializeField] private float interactDistance = 3.0f;

    private float sqrInteractDistance;

    private void Awake()
    {
        // 自身または子要素に Collider が存在するかチェック
        Collider col = GetComponentInChildren<Collider>();
        if (col == null)
        {
            // なければ自身に BoxCollider を追加して isTrigger を true にする
            BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;
        }
    }

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

        if (failedCanvas != null)
        {
            failedCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (InteractDetector.Instance == null) return;

        // --- 1. 合成アイテム（isCombineResult = true）の距離制御 ---
        if (isCombineResult)
        {
            if (resultCanvas != null && resultCanvas.activeSelf)
            {
                // ★ 本体ではなく resultCanvas の位置とプレイヤーカメラ間の距離で判定する
                bool isCloseEnough = InteractDetector.Instance.IsCloseEnough(resultCanvas.transform, interactDistance);
                if (!isCloseEnough)
                {
                    resultCanvas.SetActive(false);
                }
            }
            return;
        }

        // --- 2. 通常の証拠品（フィールド設置型）の処理 ---
        bool isTargeted = InteractDetector.Instance.CanInteract(transform, true, interactDistance);

        if (!hasInteracted)
        {
            if (interactCanvas != null && interactCanvas.activeSelf != isTargeted)
            {
                interactCanvas.SetActive(isTargeted);
            }

            if (!isTargeted && failedCanvas != null && failedCanvas.activeSelf)
            {
                failedCanvas.SetActive(false);
            }

            if (InteractDetector.Instance.WasInteracted(transform, true, interactDistance))
            {
                if (CanInteract())
                {
                    Interact();
                }
                else
                {
                    FailedInteract();
                }
            }

            if (failedCanvas != null && failedCanvas.activeSelf != isTargeted && hasFailed)
            {
                failedCanvas.SetActive(isTargeted);
            }
        }
        else
        {
            bool isCloseEnough = InteractDetector.Instance.IsCloseEnough(transform, interactDistance);

            if (resultCanvas != null && resultCanvas.activeSelf != isCloseEnough)
            {
                resultCanvas.SetActive(isCloseEnough);
            }
        }
    }

    /// <summary>
    /// ★ 合成成功時に、カメラの目の前にResultCanvasを表示する
    /// </summary>
    public void ShowCombineResultCanvas(Transform targetCamera, float distanceFromCamera = 1.5f)
    {
        if (resultCanvas == null) return;

        if (targetCamera != null)
        {
            // ★ 本体（transform）の位置も一緒に移動させておくとより安全です
            transform.position = targetCamera.position + (targetCamera.forward * distanceFromCamera);

            // キャンバスの位置・回転を設定
            resultCanvas.transform.position = transform.position;
            resultCanvas.transform.rotation = Quaternion.LookRotation(targetCamera.forward);
        }

        resultCanvas.SetActive(true);
    }

    /// <summary>
    /// プレイヤーが範囲内にいるかを判定します
    /// </summary>
    public bool IsPlayerClose()
    {
        if (isCombineResult) return false;
        if (playerCamera == null) return false;
        return (playerCamera.position - transform.position).sqrMagnitude <= sqrInteractDistance;
    }

    /// <summary>
    /// 証拠品がモーダルから「使用する」された時の通知を受け取ります
    /// </summary>
    public void OnEvidenceUsed(EvidenceObject evidence)
    {
        if (hasInteracted) return;

        if (requiredEvidences.Contains(evidence))
        {
            usedEvidences.Add(evidence);
            Debug.Log($"{name}: 必要証拠品 [{evidence.name}] が使用されました。");

            // 条件をすべて満たしたら自動でインタラクトを実行
            if (CanInteract())
            {
                Interact();
            }
        }
    }

    public bool CanInteract()
    {
        // 必要条件が設定されていない場合は誰でも入手可能
        if (requiredEvidences == null || requiredEvidences.Count == 0) return true;

        // EvidenceManager が存在しない場合はインタラクト不可
        if (EvidenceManager.Instance == null) return false;

        // 1. 設定された全ての証拠品を持っているかチェック
        if (!EvidenceManager.Instance.HasAllEvidences(requiredEvidences)) return false;

        // 2. 設定された全ての証拠品が「使用」されたかチェック
        foreach (var req in requiredEvidences)
        {
            if (req != null && !usedEvidences.Contains(req))
            {
                return false;
            }
        }

        return true;
    }

    private void Interact()
    {
        hasInteracted = true;
        hasFailed = false;

        if (targetParticle != null) targetParticle.gameObject.SetActive(false);
        if (interactCanvas != null) interactCanvas.SetActive(false);
        if (failedCanvas != null) failedCanvas.SetActive(false);

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

            // 入手に必要な全ての証拠をevidenceManagerから消す
            EvidenceManager.Instance.EraseEvidences(requiredEvidences);
        }

        if (SEManager.Instance != null)
        {
            if(successSound != null)
            {
                SEManager.Instance.PlaySE(successSound);
            }
            else
            {
                SEManager.Instance.PlayGetItemSound();
            }
        }
    }

    private void FailedInteract()
    {
        hasFailed = true;

        if (failedCanvas != null)
        {
            failedCanvas.SetActive(true);

            if (failedCanvas.TryGetComponent<NyonUI>(out var nyonUI))
            {
                nyonUI.Show();
            }
            else
            {
                nyonUI = failedCanvas.GetComponentInChildren<NyonUI>();
                if (nyonUI != null) nyonUI.Show();
            }
        }

        if (SEManager.Instance != null)
        {
            if (failedSound == null)
            {
                SEManager.Instance.PlayErrorSound();
            }
            else
            {
                SEManager.Instance.PlaySE(failedSound);
            }
        }
    }
}