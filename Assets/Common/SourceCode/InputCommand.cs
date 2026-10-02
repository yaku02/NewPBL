using UnityEngine;
using UnityEngine.InputSystem;

public class InputCommand : MonoBehaviour
{
    [Header("UI操作用アクション")]
    [Tooltip("VRコントローラー（例: Aボタン/PrimaryButton）とキーボード（例: Tabキー）の両方を割り当てます")]
    [SerializeField] private InputAction toggleEvidenceList;
    [SerializeField] private InputAction toggleOptionCanvas;
    [SerializeField] private InputAction interact;

    // 他のクラスからアクションを取得するためのプロパティ
    public InputAction ToggleEvidenceList => toggleEvidenceList;
    public InputAction ToggleOptionCanvas => toggleOptionCanvas;
    public InputAction Interact => interact;

    private void OnEnable()
    {
        // スクリプト有効化時にアクションを有効にする
        toggleEvidenceList?.Enable();
        toggleOptionCanvas?.Enable();
        interact?.Enable();
    }

    private void OnDisable()
    {
        // スクリプト無効化時にアクションを無効にする
        toggleEvidenceList?.Disable();
        toggleOptionCanvas?.Disable();
        interact?.Disable();
    }
}