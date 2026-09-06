using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    [SerializeField] private List<EvidenceObject> collectedEvidences = new List<EvidenceObject>();
    public IReadOnlyList<EvidenceObject> CollectedEvidences => collectedEvidences;

    [Header("UI設定")]
    [SerializeField] private GameObject evidenceListCanvas;
    [SerializeField] private TextMeshProUGUI evidenceListText;

    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera; // プレイヤーのカメラ（Main Camera / HMD）
    [SerializeField] private float distanceFromCamera = 1.5f; // カメラからキャンバスまでの距離（メートル）

    [Header("入力コマンド参照")]
    [SerializeField] private InputCommand inputCommand;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if(evidenceListCanvas != null)
        {
            evidenceListCanvas.SetActive(false);
        }

        // ToggleEvidenceList アクションにイベントを登録
        if (inputCommand != null && inputCommand.ToggleEvidenceList != null)
        {
            inputCommand.ToggleEvidenceList.performed += OnToggleEvidenceListPerformed;
        }
    }

    private void OnDestroy()
    {
        // イベント解除（メモリリーク防止）
        if (inputCommand != null && inputCommand.ToggleEvidenceList != null)
        {
            inputCommand.ToggleEvidenceList.performed -= OnToggleEvidenceListPerformed;
        }
    }

    private void OnToggleEvidenceListPerformed(InputAction.CallbackContext context)
    {
        ToggleEvidenceUI();
    }

    public void AddEvidence(EvidenceObject evidence)
    {
        if (evidence != null && !collectedEvidences.Contains(evidence))
        {
            collectedEvidences.Add(evidence);
            UpdateEvidenceText();
        }
    }

    private void UpdateEvidenceText()
    {
        if (evidenceListText == null) return;
        evidenceListText.text = "";
        foreach (var evidence in collectedEvidences)
        {
            evidenceListText.text += $"・ {evidence.name}\n";
        }
    }

    public void ToggleEvidenceUI()
    {
        if (evidenceListCanvas == null) return;

        bool isActive = !evidenceListCanvas.activeSelf;

        if (isActive)
        {
            UpdateEvidenceText(); //テキスト更新
            PositionCanvasInFrontOfCamera(); //位置をプレイヤーの目の前に

            evidenceListCanvas.SetActive(true);

            if (evidenceListCanvas.TryGetComponent<NyonUI>(out var nyonUI))
            {
                nyonUI.Show();
            }
            else
            {
                nyonUI = evidenceListCanvas.GetComponentInChildren<NyonUI>();
                if (nyonUI != null) nyonUI.Show();
            }
        }
        else
        {
            evidenceListCanvas.SetActive(false);
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
                Debug.LogWarning("EvidenceManager: playerCamera が設定されていません");
                return;
            }
        }

        // カメラの位置から前方（forward）に distanceFromCamera メートル進んだ座標を適用
        evidenceListCanvas.transform.position = playerCamera.position + (playerCamera.forward * distanceFromCamera);
    }
}