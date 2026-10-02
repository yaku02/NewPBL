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
    [SerializeField] private Transform buttonContainer;
    [SerializeField] private GameObject evidenceButtonPrefab;

    [Header("詳細表示UI設定")]
    [SerializeField] private GameObject detailModalPrefab; // ★ 詳細表示用プレハブ
    [SerializeField] private Transform detailModalParent;  // ★ モーダルの親要素（未設定時は evidenceListCanvas 内）

    [Header("フォント設定")]
    [SerializeField] private TMP_FontAsset notoSansFont;

    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distanceFromCamera = 1.5f;

    [Header("入力コマンド参照")]
    [SerializeField] private InputCommand inputCommand;

    private GameObject currentDetailModalInstance;

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
            UpdateEvidenceButtons();
        }
    }

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

    private void OnEvidenceButtonClicked(EvidenceObject evidence)
    {
        Debug.Log($"証拠品が選択されました: {evidence.name}");
        OpenDetailModal(evidence);
    }

    // ★ 詳細モーダルを開き、EvidenceObject の情報を反省させる
    private void OpenDetailModal(EvidenceObject evidence)
    {
        if (detailModalPrefab == null)
        {
            Debug.LogWarning("EvidenceManager: detailModalPrefab が設定されていません");
            return;
        }

        // 既存のモーダルがあれば削除
        if (currentDetailModalInstance != null)
        {
            Destroy(currentDetailModalInstance);
        }

        // 生成先を決定
        Transform targetParent = detailModalParent != null ? detailModalParent : evidenceListCanvas.transform;

        // モーダル生成
        currentDetailModalInstance = Instantiate(detailModalPrefab, targetParent);

        // -------------------------------------------------------------
        // 1. 画像（evidenceIcon）のセット
        // -------------------------------------------------------------
        Image iconImage = currentDetailModalInstance.transform.Find("EvidenceIcon")?.GetComponent<Image>();
        if (iconImage == null)
        {
            iconImage = currentDetailModalInstance.GetComponentInChildren<Image>();
        }

        if (iconImage != null)
        {
            if (evidence.EvidenceIcon != null)
            {
                iconImage.sprite = evidence.EvidenceIcon;
                iconImage.enabled = true;
            }
            else
            {
                // 画像が設定されていない場合は非表示（またはデフォルト画像にする）
                iconImage.enabled = false;
            }
        }

        // -------------------------------------------------------------
        // 2. 証拠品名（TitleText）のセット
        // -------------------------------------------------------------
        TMP_Text titleText = currentDetailModalInstance.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        if (titleText != null)
        {
            titleText.text = evidence.name;
            if (notoSansFont != null) titleText.font = notoSansFont;
        }

        // -------------------------------------------------------------
        // 3. 説明文（DescriptionText）のセット
        // -------------------------------------------------------------
        TMP_Text descriptionText = currentDetailModalInstance.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
        if (descriptionText != null)
        {
            descriptionText.text = evidence.UIDescription;
            if (notoSansFont != null) descriptionText.font = notoSansFont;
        }

        // -------------------------------------------------------------
        // 4. 閉じるボタン（CloseButton）の設定
        // -------------------------------------------------------------
        Button closeButton = currentDetailModalInstance.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
        {
            closeButton = currentDetailModalInstance.GetComponentInChildren<Button>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetailModal);
        }

        // NyonUI などのアニメーション対応
        if (currentDetailModalInstance.TryGetComponent<NyonUI>(out var nyonUI))
        {
            nyonUI.Show();
        }
    }

    public void CloseDetailModal()
    {
        if (currentDetailModalInstance != null)
        {
            Destroy(currentDetailModalInstance);
            currentDetailModalInstance = null;
        }
    }

    public void ToggleEvidenceUI()
    {
        if (evidenceListCanvas == null) return;

        bool isActive = !evidenceListCanvas.activeSelf;

        if (isActive)
        {
            UpdateEvidenceButtons();
            PositionCanvasInFrontOfCamera();

            evidenceListCanvas.SetActive(true);

            if (SEManager.Instance != null)
            {
                SEManager.Instance.PlayInteractSound();
            }

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
            CloseDetailModal();
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