using UnityEngine;
using DG.Tweening;

public class SlideUI : MonoBehaviour
{
    public enum SlideDirection
    {
        Left,
        Right,
        Top,
        Bottom
    }

    [Header("Components")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform rectTransform;

    [Header("Slide Settings")]
    [SerializeField] private SlideDirection direction = SlideDirection.Left;
    [SerializeField] private float slideDistance = 200f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Ease easeType = Ease.OutCubic;

    private Vector2 defaultAnchoredPosition;
    private bool isInitialized = false;

    private void Awake()
    {
        Initialize();
    }

    // Åö SetActive(true) Ç…Ç»Ç¡ÇΩÇ∆Ç´Ç…é©ìÆé¿çsÇ≥ÇÍÇÈ
    private void OnEnable()
    {
        Show();
    }

    private void Initialize()
    {
        if (isInitialized) return;

        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
        {
            if (!TryGetComponent<CanvasGroup>(out canvasGroup))
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (rectTransform != null)
        {
            defaultAnchoredPosition = rectTransform.anchoredPosition;
        }

        isInitialized = true;
    }

    public void Show()
    {
        Initialize();

        if (rectTransform == null || canvasGroup == null) return;

        rectTransform.DOKill();
        canvasGroup.DOKill();

        Vector2 startPosition = defaultAnchoredPosition + GetOffsetVector(direction, slideDistance);

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        rectTransform.anchoredPosition = startPosition;

        canvasGroup.DOFade(1f, duration * 0.6f);
        rectTransform.DOAnchorPos(defaultAnchoredPosition, duration)
            .SetEase(easeType)
            .OnComplete(() => {
                if (canvasGroup != null)
                {
                    canvasGroup.interactable = true;
                    canvasGroup.blocksRaycasts = true;
                }
            });
    }

    private Vector2 GetOffsetVector(SlideDirection dir, float distance)
    {
        return dir switch
        {
            SlideDirection.Left => new Vector2(-distance, 0f),
            SlideDirection.Right => new Vector2(distance, 0f),
            SlideDirection.Top => new Vector2(0f, distance),
            SlideDirection.Bottom => new Vector2(0f, -distance),
            _ => Vector2.zero
        };
    }
}