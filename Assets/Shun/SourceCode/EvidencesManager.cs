using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance { get; private set; }

    [SerializeField] private List<EvidenceObject> collectedEvidences = new List<EvidenceObject>();
    public IReadOnlyList<EvidenceObject> CollectedEvidences => collectedEvidences;

    [Header("UI設定")]
    [SerializeField] private GameObject evidenceListCanvas;

    // ★ テキストの代わりにボタンの親コンテナとボタンプレハブを指定
    [SerializeField] private Transform buttonContainer; // ボタンを並べる親要素（Vertical Layout Group等を推奨）
    [SerializeField] private GameObject evidenceButtonPrefab; // 作成したボタンのプレハブ

    [Header("フォント設定")]
    [SerializeField] private TMP_FontAsset notoSansFont;

    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distanceFromCamera = 1.5f;

    [Header("入力コマンド参照")]
    [SerializeField] private InputCommand inputCommand;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (evidenceListCanvas != null)
        {
            evidenceListCanvas.SetActive(false);
        }

        if (inputCommand != null && inputCommand.ToggleEvidenceList != null)
        {
            inputCommand.ToggleEvidenceList.performed += OnToggleEvidenceListPerformed;
        }
    }

    private void OnDestroy()
    {
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
            UpdateEvidenceButtons(); // ★ ボタン一覧を更新
        }
    }

    // ★ 証拠品ボタンを生成・更新する処理
    private void UpdateEvidenceButtons()
    {
        if (buttonContainer == null || evidenceButtonPrefab == null) return;

        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        foreach (var evidence in collectedEvidences)
        {
            GameObject buttonObj = Instantiate(evidenceButtonPrefab, buttonContainer);

            TMP_Text buttonText = buttonObj.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = evidence.name;

                // ★ フォントアセットが設定されていれば適用する
                if (notoSansFont != null)
                {
                    buttonText.font = notoSansFont;
                }
            }

            Button buttonComponent = buttonObj.GetComponent<Button>();
            if (buttonComponent != null)
            {
                EvidenceObject targetEvidence = evidence;
                buttonComponent.onClick.AddListener(() => OnEvidenceButtonClicked(targetEvidence));
            }
        }
    }

    // ★ 証拠品ボタンが押された時の処理（詳細画面表示など）
    private void OnEvidenceButtonClicked(EvidenceObject evidence)
    {
        Debug.Log($"証拠品が選択されました: {evidence.name}");
        // ここに詳細モーダルを開く処理などを記述できます
    }

    public void ToggleEvidenceUI()
    {
        if (evidenceListCanvas == null) return;

        bool isActive = !evidenceListCanvas.activeSelf;

        if (isActive)
        {
            UpdateEvidenceButtons(); // ★ 表示時に最新のボタン一覧を生成
            PositionCanvasInFrontOfCamera();

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

        evidenceListCanvas.transform.position = playerCamera.position + (playerCamera.forward * distanceFromCamera);
    }
}