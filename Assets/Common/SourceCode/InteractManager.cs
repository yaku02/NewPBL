using UnityEngine;
using UnityEngine.InputSystem;

public class InteractManager : MonoBehaviour
{
    [Header("参照設定")]
    [SerializeField] private Transform playerCamera; // Main Camera

    [Header("探索設定")]
    [SerializeField] private float searchRadius = 5.0f; // 付近のインタラクト対象を探す最大半径

    private IInteractable currentInteractable = null;
    private bool isInteracting = false;

    public bool IsInteracting => isInteracting;

    private void Update()
    {
        if (playerCamera == null) return;

        // キー入力判定（Fキー または QuestのAボタン）
        bool isFKeyPressed = Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
        bool isAButtonPressed = Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;

        // インタラクト中でない場合は、一番近い対象を探す
        if (!isInteracting)
        {
            currentInteractable = FindNearestInteractable();
        }

        // 入力があった場合の処理
        if (isFKeyPressed || isAButtonPressed)
        {
            if (isInteracting)
            {
                // インタラクト解除
                EndInteraction();
            }
            else if (currentInteractable != null)
            {
                // インタラクト開始
                StartInteraction();
            }
        }

        // インタラクト中、対象から離れすぎたら自動解除
        if (isInteracting && currentInteractable != null)
        {
            float dist = Vector3.Distance(playerCamera.position, currentInteractable.transform.position);
            if (dist > currentInteractable.InteractDistance + 1.0f)
            {
                EndInteraction();
            }
        }
    }

    private void StartInteraction()
    {
        isInteracting = true;
        currentInteractable.OnInteract(true);
    }

    private void EndInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.OnInteract(false);
        }
        isInteracting = false;
        currentInteractable = null;
    }

    // プレイヤーの近くにある一番近い IInteractable を探す
    private IInteractable FindNearestInteractable()
    {
        Collider[] hitColliders = Physics.OverlapSphere(playerCamera.position, searchRadius);
        IInteractable nearest = null;
        float minDistance = float.MaxValue;

        foreach (var col in hitColliders)
        {
            // IInteractable コンポーネントを持っているか確認
            if (col.TryGetComponent<IInteractable>(out var interactable))
            {
                float dist = Vector3.Distance(playerCamera.position, interactable.transform.position);

                // 規定のインタラクト可能距離内かつ、一番近いものを保持
                if (dist <= interactable.InteractDistance && dist < minDistance)
                {
                    minDistance = dist;
                    nearest = interactable;
                }
            }
        }

        return nearest;
    }

    // エディタ上で探索範囲を視覚化（Gizmos）
    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(playerCamera.position, searchRadius);
        }
    }
}