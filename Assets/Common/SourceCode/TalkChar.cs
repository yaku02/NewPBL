using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro を使用する場合
using UnityEngine.InputSystem;

public class TalkChar : MonoBehaviour
{
    // --- 選択肢データの構造体 ---
    [System.Serializable]
    public class ChoiceData
    {
        [Tooltip("選択肢に表示するテキスト")]
        public string choiceText = "選択肢";

        [Tooltip("手動、または証拠品によってこの選択肢を表示可能にするフラグ")]
        public bool isAvailable = true;

        [Header("証拠品による解放条件")]
        [Tooltip("ここに設定したすべての証拠品を所持している場合、自動的に isAvailable が true になります（空の場合は手動設定に従います）")]
        public List<EvidenceObject> requiredEvidences;

        [Tooltip("この選択肢を選択した際に表示する固有のキャンバス")]
        public GameObject resultCanvasForThisChoice;

        [Tooltip("フラグが false の（ダミー状態の）時に表示するテキスト（空欄の場合は choiceText をそのまま表示）")]
        public string dummyText = "？？？？";
    }

    [Header("参照設定")]
    [SerializeField] private Transform playerCamera;

    [Header("UI Canvas")]
    [SerializeField] private GameObject talkStartCanvas;
    [SerializeField] private GameObject choiceCanvas;

    [Header("動的ボタン生成設定")]
    [SerializeField] private Transform choiceButtonContainer; // ボタンを配置する親オブジェクト（Vertical Layout Group など）
    [SerializeField] private GameObject choiceButtonPrefab;  // 通常の選択肢ボタンのプレハブ
    [SerializeField] private GameObject dummyButtonPrefab;   // フラグ false 時に生成するダミーボタンのプレハブ

    [Header("選択肢設定")]
    [SerializeField] private List<ChoiceData> choices = new List<ChoiceData>(); // インスペクターで要素数を調整可能

    [Header("インタラクト設定")]
    [SerializeField] private float rotateSpeed = 5.0f;
    [SerializeField] private AudioClip talkSE;

    private bool isTalking = false;
    private GameObject currentResultCanvas = null; // 現在表示中の結果キャンバスを保持
    private List<GameObject> generatedButtons = new List<GameObject>(); // 生成されたボタン（通常・ダミー問わず）を保持

    private void Start()
    {
        StopTalking();

        if (playerCamera == null)
        {
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                playerCamera = mainCamera.transform;
            }
            else
            {
                Debug.LogWarning("TalkChar: Main Camera が見つかりません。playerCamera を手動で設定してください。");
            }
        }

        if (talkSE == null)
        {
            talkSE = Resources.Load<AudioClip>("talkSE");
        }
    }

    private void Update()
    {
        if (InteractDetector.Instance == null) return;

        // --- 1. 会話中（選択肢表示中 または 結果キャンバス表示中）の処理 ---
        if (isTalking || (currentResultCanvas != null && currentResultCanvas.activeSelf))
        {
            // ★ Raycast（視線）は無視し、距離（3.0m）のみで判定
            bool isClose = InteractDetector.Instance.IsCloseEnough(transform, 3.0f);

            if (!isClose)
            {
                // 3.0m 以上離れたら会話状態を解除してキャンバスを閉じる
                StopTalking();
                return;
            }

            // 会話中（または結果表示中）にもう一度インタラクトキーが押されたら解除
            if (InteractDetector.Instance.IsInteractPressed())
            {
                StopTalking();
                return;
            }

            // プレイヤーの方を向く回転処理を継続
            LookAtPlayer();
            return;
        }

        // --- 2. 会話前の判定（距離 3.0m ＋ Raycast判定） ---
        bool isCloseAndLooking = InteractDetector.Instance.CanInteract(transform, true, 3.0f);

        // 会話前のUI制御（視線外または 3.0m 外なら非表示）
        if (!isCloseAndLooking)
        {
            if (talkStartCanvas != null && talkStartCanvas.activeSelf)
            {
                talkStartCanvas.SetActive(false);
            }
            return;
        }

        // 3.0m 範囲内かつ視線上に捉えている時だけ会話開始UIを表示
        if (talkStartCanvas != null && !talkStartCanvas.activeSelf)
        {
            talkStartCanvas.SetActive(true);
        }

        // --- 3. 会話開始の入力判定（距離 3.0m ＆ 視線OK ＋ キー入力） ---
        if (InteractDetector.Instance.WasInteracted(transform, true, 3.0f))
        {
            Interact();
        }
    }

    private void LookAtPlayer()
    {
        if (playerCamera == null) return;

        Vector3 targetDirection = playerCamera.position - transform.position;
        targetDirection.y = 0f;

        if (targetDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotateSpeed);
        }
    }

    public void Interact()
    {
        isTalking = true;

        if (SEManager.Instance != null && talkSE != null)
        {
            SEManager.Instance.PlaySE(talkSE);
        }

        if (talkStartCanvas != null) talkStartCanvas.SetActive(false);

        // 会話開始時に、証拠品の所持状況を元に選択肢の解放状態を自動更新する
        UpdateChoiceAvailabilities();

        // 選択肢ボタンの動的生成
        GenerateChoiceButtons();

        if (choiceCanvas != null) choiceCanvas.SetActive(true);
    }

    /// <summary>
    /// 設定された requiredEvidences をすべて持っているか確認し、isAvailable を自動更新する
    /// </summary>
    private void UpdateChoiceAvailabilities()
    {
        foreach (var choice in choices)
        {
            if (choice.requiredEvidences != null && choice.requiredEvidences.Count > 0)
            {
                if (EvidenceManager.Instance != null)
                {
                    choice.isAvailable = EvidenceManager.Instance.HasAllEvidences(choice.requiredEvidences);
                }
            }
        }
    }

    /// <summary>
    /// isAvailable の状態に応じて通常ボタンまたはダミーボタンを動的生成
    /// </summary>
    private void GenerateChoiceButtons()
    {
        ClearChoiceButtons();

        if (choiceButtonPrefab == null || choiceButtonContainer == null)
        {
            Debug.LogWarning("TalkChar: choiceButtonPrefab または choiceButtonContainer が設定されていません。");
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            ChoiceData data = choices[i];
            int index = i; // ローカル変数にキャプチャ

            if (data.isAvailable)
            {
                GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                generatedButtons.Add(btnObj);

                SetButtonText(btnObj, data.choiceText);

                Button button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnChoiceSelected(index));
                }
            }
            else
            {
                if (dummyButtonPrefab != null)
                {
                    GameObject dummyObj = Instantiate(dummyButtonPrefab, choiceButtonContainer);
                    generatedButtons.Add(dummyObj);

                    string displayText = !string.IsNullOrEmpty(data.dummyText) ? data.dummyText : data.choiceText;
                    SetButtonText(dummyObj, displayText);
                }
            }
        }
    }

    private void SetButtonText(GameObject obj, string text)
    {
        TMP_Text tmpText = obj.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = text;
        }
        else
        {
            Text legacyText = obj.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = text;
        }
    }

    /// <summary>
    /// 選択肢がクリックされた時の処理
    /// </summary>
    private void OnChoiceSelected(int index)
    {
        if (index < 0 || index >= choices.Count) return;

        ChoiceData selectedChoice = choices[index];

        if (SEManager.Instance != null)
        {
            SEManager.Instance.PlayGetItemSound();
        }

        // 選択肢画面を閉じる
        if (choiceCanvas != null) choiceCanvas.SetActive(false);

        // ★ この選択肢に対応する Result Canvas が設定されている場合は表示し、判定除外用の参照として保持
        if (selectedChoice.resultCanvasForThisChoice != null)
        {
            selectedChoice.resultCanvasForThisChoice.SetActive(true);
            currentResultCanvas = selectedChoice.resultCanvasForThisChoice;
        }
    }

    public void SetChoiceAvailable(int index, bool isAvailable)
    {
        if (index >= 0 && index < choices.Count)
        {
            choices[index].isAvailable = isAvailable;
        }
    }

    private void ClearChoiceButtons()
    {
        foreach (GameObject btn in generatedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        generatedButtons.Clear();
    }

    public void StopTalking()
    {
        isTalking = false;

        ClearChoiceButtons();

        if (talkStartCanvas != null) talkStartCanvas.SetActive(false);
        if (choiceCanvas != null) choiceCanvas.SetActive(false);

        // すべての選択肢に設定された Result Canvas を非アクティブにする
        foreach (var choice in choices)
        {
            if (choice.resultCanvasForThisChoice != null)
            {
                choice.resultCanvasForThisChoice.SetActive(false);
            }
        }

        currentResultCanvas = null;
    }
}