using UnityEngine;
using DG.Tweening;

public class NyonUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;

    private Vector3 defaultScale; // 元のスケールを保持する変数
    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    // 初期状態のスケールを保存する処理
    private void Initialize()
    {
        if (isInitialized) return;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        defaultScale = rectTransform.localScale; // Inspectorで設定した元のスケールを記憶
        isInitialized = true;
    }

    public void Show()
    {
        // まだ初期化されていない場合は呼び出す
        Initialize();

        // 途中でアニメーションが呼ばれてもいいように既存のTweenを停止
        rectTransform.DOKill();
        canvasGroup.DOKill();

        // 元のスケールを基準にした初期値（横幅80%、縦幅0%）
        Vector3 startScale = new Vector3(defaultScale.x * 0.8f, 0f, defaultScale.z);

        // リセット
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.localScale = startScale;

        // アニメーション実行
        canvasGroup.DOFade(1f, 0.2f);
        rectTransform.DOScale(defaultScale, 0.35f) // Vector3.one ではなく defaultScale を指定！
            .SetEase(Ease.OutBack)
            .OnComplete(() => {
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            });
    }
}