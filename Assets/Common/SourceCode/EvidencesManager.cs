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

    // ★ 組み合わせ選択中の証拠品を保持する変数
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

    // ★ 詳細モーダルを開き、EvidenceObject の情報を反映させる
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

        // ★ 生成したモーダルの EvidenceDetail に選択した証拠品をセットする
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

    /// <summary>
    /// モーダルの「使用する」ボタンから呼び出される処理
    /// </summary>
    public void OnUseButtonClicked(EvidenceObject evidence)
    {
        if (evidence == null) return;

        Debug.Log($"証拠品が使用されました: {evidence.name}");

        // 近くにある（インタラクト範囲内の）EvidenceObject に対して使用通知を送る
        EvidenceObject[] allEvidences = FindObjectsByType<EvidenceObject>(FindObjectsSortMode.None);
        foreach (var targetObj in allEvidences)
        {
            if (targetObj != null && targetObj.IsPlayerClose())
            {
                targetObj.OnEvidenceUsed(evidence);
            }
        }

        // モーダルを閉じる
        CloseDetailModal();
    }

    /// <summary>
    /// モーダルの「組み合わせる」ボタンから呼び出される処理
    /// </summary>
    public void OnCombineButtonClicked(EvidenceObject evidence)
    {
        if (evidence == null) return;

        Debug.Log($"証拠品を組み合わせるが押されました: {evidence.name}");

        // 1回目：まだ誰も選択されていない場合
        if (selectedCombineEvidence == null)
        {
            selectedCombineEvidence = evidence;
            Debug.Log($"合成の1つ目として [{selectedCombineEvidence.name}] を選択しました。");

            CloseDetailModal();
            UpdateEvidenceButtons(); // UI上の「[選択中]」表記などを更新
            return;
        }

        // 同じアイテムを再度選んだ場合はキャンセル処理
        if (selectedCombineEvidence == evidence)
        {
            Debug.Log("同じ証拠品が選択されたため、組み合わせをキャンセルしました。");
            selectedCombineEvidence = null;
            CloseDetailModal();
            UpdateEvidenceButtons();
            return;
        }

        // 2回目：別のアイテムが選ばれたので合成判定を実行
        TryCombineEvidences(selectedCombineEvidence, evidence);
    }

    /// <summary>
    /// 2つの証拠品を合成判定・実行する
    /// </summary>
    private void TryCombineEvidences(EvidenceObject first, EvidenceObject second)
    {
        bool isSuccess = false;

        // パターンA: first が土台で second が素材
        if (first.CombineIngredient == second && first.CombineResult != null)
        {
            ExecuteCombine(first, second, first.CombineResult);
            isSuccess = true;
        }
        // パターンB: second が土台で first が素材（順不同対応）
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

        // 選択状態をリセットしてモーダルを閉じる
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

        // カメラの目の前に ResultCanvas を展開する
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

        evidenceListCanvas.transform.position = playerCamera.position + (playerCamera.forward * distanceFromCamera);
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
                return false; // 1つでも足りなければ false
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

        // 1つ以上削除された場合のみUI更新とモーダル閉じる処理を行う
        if (hasRemovedAny)
        {
            UpdateEvidenceButtons();
            CloseDetailModal();
        }
    }
}