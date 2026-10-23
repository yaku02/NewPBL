using UnityEngine;
using UnityEngine.InputSystem; // ★ InputSystemを使うため追加

public class OptionCanvasManager : MonoBehaviour
{
    [Header("UI設定")]
    [SerializeField] private GameObject optionCanvas;
    [SerializeField] private AudioClip openCanvasSE; // 効果音


    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera; // プレイヤーのカメラ（Main Camera / HMD）
    [SerializeField] private float distanceFromCamera = 1.5f; // カメラからキャンバスまでの距離（メートル）

    [Header("入力コマンド参照")]
    [SerializeField] private InputCommand inputCommand;

    private void Start()
    {
        if (optionCanvas != null)
        {
            optionCanvas.SetActive(false);
        }

        if (inputCommand != null && inputCommand.ToggleOptionCanvas != null)
        {
            inputCommand.ToggleOptionCanvas.performed += OnTogglePerformed;
        }
    }

    private void OnDestroy()
    {
        // イベント解除（メモリリーク防止）
        if (inputCommand != null && inputCommand.ToggleOptionCanvas != null)
        {
            inputCommand.ToggleOptionCanvas.performed -= OnTogglePerformed;
        }
    }

    private void OnTogglePerformed(InputAction.CallbackContext context)
    {
        ToggleCanvas();
    }

    public void ToggleCanvas()
    {
        if (optionCanvas == null) return;

        bool isActive = !optionCanvas.activeSelf;

        if (isActive)
        {
            // ★ 表示前にカメラの前に位置を合わせる関数を呼び出す
            PositionCanvasInFrontOfCamera();

            optionCanvas.SetActive(true);

            if (optionCanvas.TryGetComponent<NyonUI>(out var nyonUI))
            {
                nyonUI.Show();
            }
            else
            {
                nyonUI = optionCanvas.GetComponentInChildren<NyonUI>();
                if (nyonUI != null) nyonUI.Show();
            }

            if (SEManager.Instance != null && openCanvasSE != null)
            {
                SEManager.Instance.PlaySE(openCanvasSE);
            }
        }
        else
        {
            optionCanvas.SetActive(false);
        }
    }

    private void PositionCanvasInFrontOfCamera()
    {
        if (playerCamera == null)
        {
            if (Camera.main != null)
            {
                playerCamera = Camera.main.transform;
            }
            else
            {
                Debug.LogWarning("OptionCanvasManager: playerCamera が設定されていません");
                return;
            }
        }

        optionCanvas.transform.position = playerCamera.position + (playerCamera.forward * distanceFromCamera);
    }
}