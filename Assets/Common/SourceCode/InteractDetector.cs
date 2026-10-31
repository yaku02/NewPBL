using UnityEngine;

public class InteractDetector : MonoBehaviour
{
    public static InteractDetector Instance { get; private set; }

    [Header("レイキャスト・距離設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float interactDistance = 2.0f;
    [SerializeField] private LayerMask targetLayer = ~0;

    private float sqrInteractDistance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        UpdateSqrDistance();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        UpdateSqrDistance();
    }
#endif

    private void UpdateSqrDistance()
    {
        sqrInteractDistance = interactDistance * interactDistance;
    }

    /// <summary>
    /// 対象の Transform がプレイヤーカメラから十分近いかを判定します
    /// </summary>
    /// <param name="targetTransform">対象のオブジェクト</param>
    /// <param name="customDistance">指定した場合、その距離（メートル）で判定します（-1 の場合は標準の interactDistance を使用）</param>
    public bool IsCloseEnough(Transform targetTransform, float customDistance = -1f)
    {
        if (playerCamera == null || targetTransform == null) return false;

        float sqrDistance = (playerCamera.position - targetTransform.position).sqrMagnitude;

        // ★ カスタム距離が指定されている場合はその二乗で比較、無ければデフォルトを使用
        float targetSqrDistance = (customDistance > 0f) ? (customDistance * customDistance) : sqrInteractDistance;

        return sqrDistance <= targetSqrDistance;
    }

    /// <summary>
    /// カメラからのレイが対象の Transform（またはその子要素）に届いているかを判定します
    /// </summary>
    /// <param name="targetTransform">対象のオブジェクト</param>
    /// <param name="customDistance">指定した場合、レイの届く距離を変更します（-1 の場合は標準距離）</param>
    public bool IsLookingAt(Transform targetTransform, float customDistance = -1f)
    {
        if (playerCamera == null || targetTransform == null) return false;

        float distance = (customDistance > 0f) ? customDistance : interactDistance;

        Ray ray = new Ray(playerCamera.position, playerCamera.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, distance, targetLayer, QueryTriggerInteraction.Collide))
        {
            return hit.transform == targetTransform || hit.transform.IsChildOf(targetTransform);
        }

        return false;
    }

    /// <summary>
    /// 距離条件・視線条件・カスタム条件を満たしているかを判定します
    /// </summary>
    public bool CanInteract(Transform targetTransform, bool customInteract = true, float customDistance = -1f)
    {
        if (!customInteract) return false;

        return IsCloseEnough(targetTransform, customDistance) && IsLookingAt(targetTransform, customDistance);
    }

    /// <summary>
    /// インタラクトボタンがこのフレームで押されたかを判定します
    /// </summary>
    public bool IsInteractPressed()
    {
        if (InputCommandManager.Instance == null || InputCommandManager.Instance.Interact == null)
        {
            return false;
        }

        return InputCommandManager.Instance.Interact.WasPressedThisFrame();
    }

    /// <summary>
    /// インタラクト可能状態（距離・視線）かつボタンが押された瞬間かを判定します
    /// </summary>
    public bool WasInteracted(Transform targetTransform, bool customInteract = true, float customDistance = -1f)
    {
        return CanInteract(targetTransform, customInteract, customDistance) && IsInteractPressed();
    }
}