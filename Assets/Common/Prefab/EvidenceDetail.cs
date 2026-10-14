using UnityEngine;
using UnityEngine.UI;

public class EvidenceDetail : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] private Button useButton; // 「使用する」ボタン
    [SerializeField] private Button combineButton;   // 「組み合わせる」ボタン

    // 現在モーダルで表示している証拠品
    private EvidenceObject currentEvidence;

    private void Awake()
    {
        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnClickUseButton);
        }

        // ★ ここが正しく設定されているか確認
        if (combineButton != null)
        {
            combineButton.onClick.RemoveAllListeners();
            combineButton.onClick.AddListener(OnClickCombineButton);
        }
    }

    /// <summary>
    /// モーダル表示時に EvidenceManager などから呼び出して証拠品情報をセットするメソッド
    /// </summary>
    public void SetEvidence(EvidenceObject evidence)
    {
        currentEvidence = evidence;

        if (useButton != null)
        {
            useButton.onClick.RemoveAllListeners();
            useButton.onClick.AddListener(OnClickUseButton);
        }

        if (combineButton != null)
        {
            combineButton.onClick.RemoveAllListeners();
            combineButton.onClick.AddListener(OnClickCombineButton);
        }

    }

    /// <summary>
    /// 「使用する」ボタンが押されたときの処理
    /// </summary>
    private void OnClickUseButton()
    {
        if (currentEvidence == null)
        {
            Debug.LogWarning("EvidenceDetail: 対象の証拠品がセットされていません。");
            return;
        }

        if (EvidenceManager.Instance != null)
        {
            // EvidenceManager へ「この証拠品が使用された」と通知
            EvidenceManager.Instance.OnUseButtonClicked(currentEvidence);
        }
        else
        {
            Debug.LogError("EvidenceDetail: EvidenceManager.Instance が見つかりません！");
        }
    }

    /// <summary>
    /// 「組み合わせる」ボタンが押されたときの処理
    /// </summary>
    private void OnClickCombineButton()
    {
        if (currentEvidence == null)
        {
            Debug.LogWarning("EvidenceDetail: 対象の証拠品がセットされていません。");
            return;
        }

        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.OnCombineButtonClicked(currentEvidence);
        }
        else
        {
            Debug.LogError("EvidenceDetail: EvidenceManager.Instance が見つかりません！");
        }
    }

    private void OnDestroy()
    {
        // メモリリーク防止のためイベント解除
        if (useButton != null)
        {
            useButton.onClick.RemoveListener(OnClickUseButton);
        }
    }
}