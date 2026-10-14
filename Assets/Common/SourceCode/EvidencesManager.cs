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

    [Header("ボタンハイライト設定")]
    [Tooltip("1つ目に選択中のボタン全体の色（グレー）")]
    [SerializeField] private Color selectedBgColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [Tooltip("組み合わせ可能なボタンの縁（Outline）の色")]
    [SerializeField] private Color combinableOutlineColor = new Color(0.2f, 1f, 0.2f, 1f);
    [Tooltip("通常時のボタン背景色")]
    [SerializeField] private Color defaultBgColor = Color.white;

    [Header("詳細表示UI設定")]
    [SerializeField] private GameObject detailModalPrefab;
    [SerializeField] private Transform detailModalParent;

    [Header("フォント設定")]
    [SerializeField] private TMP_FontAsset notoSansFont;

    [Header("カメラ設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private float distanceFromCamera = 1.5f;

    [Header("入力コマンド参照")]
    [SerializeField] private InputCommand inputCommand;

    [Header("壁回避設定")]
    [Tooltip("壁に埋まらないように手前に移動させるオフセット（離す距離）")]
    [SerializeField] private float wallOffset = 0.1f;
    [Tooltip("壁判定を行うレイヤー（未設定時はすべてのレイヤー）")]
    [SerializeField] private LayerMask obstacleLayer = ~0;

    private GameObject currentDetailModalInstance;

    // 組み合わせ選択中の証拠品を保持する変数
    private EvidenceObject selectedCombineEvidence = null;

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
                if (evidence == selectedCombineEvidence)
                {
                    buttonText.text = $"{evidence.name} [選択中]";
                }
                else if (selectedCombineEvidence != null && CanCombineWith(selectedCombineEvidence, evidence))
                {
                    buttonText.text = $"{evidence.name} ✨";
                }
                else
                {
                    buttonText.text = evidence.name;
                }

                if (notoSansFont != null)
                {
                    buttonText.font = notoSansFont;
                }
            }

            // ★ 1. ボタン背景（Image）の背景色設定
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null)
            {
                if (evidence == selectedCombineEvidence)
                {
                    buttonImage.color = selectedBgColor; // 選択中は全体をグレーに
                }
                else
                {
                    buttonImage.color = defaultBgColor;  // 通常色
                }
            }

            // ★ 2. ボタンの縁（Outline）の設定
            Outline outline = buttonObj.GetComponent<Outline>();
            if (outline == null)
            {
                // コンポーネントがなければ追加する
                outline = buttonObj.AddComponent<Outline>();
                outline.effectDistance = new Vector2(3f, -3f); // 縁の太さ
            }

            if (selectedCombineEvidence != null && CanCombineWith(selectedCombineEvidence, evidence))
            {
                outline.enabled = true;
                outline.effectColor = combinableOutlineColor; // 組み合わせ可能なアイテムのみ縁を光らせる
            }
            else
            {
                outline.enabled = false; // 通常時・選択中は縁を非表示
            }

            Button buttonComponent = buttonObj.GetComponent<Button>();
            if (buttonComponent != null)
            {
                EvidenceObject targetEvidence = evidence;
                buttonComponent.onClick.AddListener(() => OnEvidenceButtonClicked(targetEvidence));
            }
        }
    }

    private bool CanCombineWith(EvidenceObject first, EvidenceObject second)
    {
        if (first == null || second == null) return false;

        bool isPatternA = (first.CombineIngredient == second && first.CombineResult != null);
        bool isPatternB = (second.CombineIngredient == first && second.CombineResult != null);

        return isPatternA || isPatternB;
    }

    private void OnEvidenceButtonClicked(EvidenceObject evidence)
    {
        Debug.Log($"証拠品が選択されました: {evidence.name}");
        OpenDetailModal(evidence);
    }

    private void OpenDetailModal(EvidenceObject evidence)
    {
        if (detailModalPrefab == null)
        {
            Debug.LogWarning("EvidenceManager: detailModalPrefab が設定されていません");
            return;
        }

        if (currentDetailModalInstance != null)
        {
            Destroy(currentDetailModalInstance);
        }

        Transform targetParent = detailModalParent != null ? detailModalParent : evidenceListCanvas.transform;
        currentDetailModalInstance = Instantiate(detailModalPrefab, targetParent);

        if (currentDetailModalInstance.TryGetComponent<EvidenceDetail>(out var detailScript))
        {
            detailScript.SetEvidence(evidence);
        }
        else
        {
            detailScript = currentDetailModalInstance.GetComponentInChildren<EvidenceDetail>();
            if (detailScript != null)
            {
                detailScript.SetEvidence(evidence);
            }
        }

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
                iconImage.enabled = false;
            }
        }

        TMP_Text titleText = currentDetailModalInstance.transform.Find("TitleText")?.GetComponent<TMP_Text>();
        if (titleText != null)
        {
            titleText.text = evidence.name;
            if (notoSansFont != null) titleText.font = notoSansFont;
        }

        TMP_Text descriptionText = currentDetailModalInstance.transform.Find("DescriptionText")?.GetComponent<TMP_Text>();
        if (descriptionText != null)
        {
            descriptionText.text = evidence.UIDescription;
            if (notoSansFont != null) descriptionText.font = notoSansFont;
        }

        Button closeButton = currentDetailModalInstance.transform.Find("CloseButton")?.GetComponent<Button>();
        if (closeButton == null)
        {
            closeButton = currentDetailModalInstance.GetComponentInChildren<Button>();
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(CloseDetailModal);
        }

        if (currentDetailModalInstance.TryGetComponent<NyonUI>(out var nyonUI))
        {
            nyonUI.Show();
        }
    }

    public void OnUseButtonClicked(EvidenceObject evidence)
    {
        if (evidence == null) return;

        Debug.Log($"証拠品が使用されました: {evidence.name}");

        EvidenceObject[] allEvidences = FindObjectsByType<EvidenceObject>(FindObjectsSortMode.None);
        foreach (var targetObj in allEvidences)
        {
            if (targetObj != null && targetObj.IsPlayerClose())
            {
                targetObj.OnEvidenceUsed(evidence);
            }
        }

        CloseDetailModal();
    }

    public void OnCombineButtonClicked(EvidenceObject evidence)
    {
        if (evidence == null) return;

        Debug.Log($"証拠品を組み合わせるが押されました: {evidence.name}");

        if (selectedCombineEvidence == null)
        {
            selectedCombineEvidence = evidence;
            Debug.Log($"合成の1つ目として [{selectedCombineEvidence.name}] を選択しました。");

            CloseDetailModal();
            UpdateEvidenceButtons();
            return;
        }

        if (selectedCombineEvidence == evidence)
        {
            Debug.Log("同じ証拠品が選択されたため、組み合わせをキャンセルしました。");
            selectedCombineEvidence = null;
            CloseDetailModal();
            UpdateEvidenceButtons();
            return;
        }

        TryCombineEvidences(selectedCombineEvidence, evidence);
    }

    private void TryCombineEvidences(EvidenceObject first, EvidenceObject second)
    {
        bool isSuccess = false;

        if (first.CombineIngredient == second && first.CombineResult != null)
        {
            ExecuteCombine(first, second, first.CombineResult);
            isSuccess = true;
        }
        else if (second.CombineIngredient == first && second.CombineResult != null)
        {
            ExecuteCombine(first, second, second.CombineResult);
            isSuccess = true;
        }

        if (!isSuccess)
        {
            Debug.Log($"[{first.name}] と [{second.name}] は組み合わせられません。");
            if (SEManager.Instance != null)
            {
                SEManager.Instance.PlayErrorSound();
            }
        }

        selectedCombineEvidence = null;
        CloseDetailModal();
        UpdateEvidenceButtons();
    }

    private void ExecuteCombine(EvidenceObject first, EvidenceObject second, EvidenceObject result)
    {
        Debug.Log($"合成成功！: {first.name} + {second.name} -> {result.name}");

        collectedEvidences.Remove(first);
        collectedEvidences.Remove(second);

        if (!collectedEvidences.Contains(result))
        {
            collectedEvidences.Add(result);
        }

        if (result != null)
        {
            result.ShowCombineResultCanvas(playerCamera, distanceFromCamera);
        }

        if (SEManager.Instance != null)
        {
            SEManager.Instance.PlayInteractSound();
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
            selectedCombineEvidence = null;
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

        Vector3 origin = playerCamera.position;
        Vector3 direction = playerCamera.forward;
        float targetDistance = distanceFromCamera;

        // カメラ正面に向かって Raycast を飛ばし、壁があるかチェック
        if (Physics.Raycast(origin, direction, out RaycastHit hit, distanceFromCamera, obstacleLayer))
        {
            // 壁に当たった場合、衝突点より少し手前（wallOffset 分）に配置距離を縮める
            targetDistance = Mathf.Max(0.2f, hit.distance - wallOffset);
        }

        // 決定した距離に Canvas を配置
        evidenceListCanvas.transform.position = origin + (direction * targetDistance);

        // Canvas をカメラの正面に向かせる（視線に対して平行にする場合）
        evidenceListCanvas.transform.rotation = Quaternion.LookRotation(direction);
    }

    public bool HasEvidence(EvidenceObject evidence)
    {
        return evidence != null && collectedEvidences.Contains(evidence);
    }

    public bool HasAllEvidences(List<EvidenceObject> requiredEvidences)
    {
        if (requiredEvidences == null || requiredEvidences.Count == 0) return true;

        foreach (var req in requiredEvidences)
        {
            if (req != null && !collectedEvidences.Contains(req))
            {
                return false;
            }
        }
        return true;
    }

    public void EraseEvidences(List<EvidenceObject> evidencesToErase)
    {
        if (evidencesToErase == null || evidencesToErase.Count == 0) return;

        bool hasRemovedAny = false;

        foreach (var evidence in evidencesToErase)
        {
            if (evidence != null && collectedEvidences.Contains(evidence))
            {
                collectedEvidences.Remove(evidence);
                hasRemovedAny = true;
                Debug.Log($"証拠品を削除しました: {evidence.name}");
            }
        }

        if (hasRemovedAny)
        {
            UpdateEvidenceButtons();
            CloseDetailModal();
        }
    }
}