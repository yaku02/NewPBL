using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro を使用する場合（通常の UI Text の場合は UnityEngine.UI.Text に変更可）
using UnityEngine.InputSystem;

public class TalkChar : MonoBehaviour
{
    // --- 選択肢データの構造体 ---
    [System.Serializable]
    public class ChoiceData
    {
        [Tooltip("選択肢に表示するテキスト")]
        public string choiceText = "選択肢";

        [Tooltip("この選択肢を表示可能にするフラグ")]
        public bool isAvailable = true;

        [Tooltip("この選択肢を選択した際に表示する固有のキャンバス")]
        public GameObject resultCanvasForThisChoice;

        [Tooltip("フラグが false の（ダミー状態の）時に表示するテキスト（空欄の場合は choiceText をそのまま表示）")]
        public string dummyText = "？？？？";
    }

    [Header("参照設定")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private InputCommand inputCommand;

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
    [SerializeField] private float interactDistance = 3.0f;
    [SerializeField] private float rotateSpeed = 5.0f;

    private float sqrInteractDistance;
    private bool isTalking = false;
    private GameObject currentResultCanvas = null; // 現在表示中の結果キャンバスを保持
    private List<GameObject> generatedButtons = new List<GameObject>(); // 生成されたボタン（通常・ダミー問わず）を保持

    private void Start()
    {
        sqrInteractDistance = interactDistance * interactDistance;

        CloseAllCanvas();

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
    }

    private void Update()
    {
        if (playerCamera == null) return;

        float sqrDistance = (playerCamera.position - transform.position).sqrMagnitude;
        bool isCloseEnough = sqrDistance <= sqrInteractDistance;

        // --- 1. 範囲外に出た場合の処理 ---
        if (!isCloseEnough)
        {
            if (isTalking || (talkStartCanvas != null && talkStartCanvas.activeSelf))
            {
                CloseAllCanvas();
            }
            return;
        }

        // --- 2. インタラクト入力判定 ---
        bool isInteractPressed = inputCommand != null
            && inputCommand.Interact != null
            && inputCommand.Interact.WasPressedThisFrame();

        // --- 3. 会話中の処理 ---
        if (isTalking)
        {
            // 会話中にもう一度キーが押されたら解除
            if (isInteractPressed)
            {
                CloseAllCanvas();
                return;
            }

            LookAtPlayer();
            return;
        }

        // --- 4. 範囲内にいる（会話前）の UI 表示 ---
        if (talkStartCanvas != null && !talkStartCanvas.activeSelf)
        {
            talkStartCanvas.SetActive(true);
        }

        // --- 5. 会話開始の入力判定 ---
        if (isInteractPressed)
        {
            Interact();
        }
    }

    private void LookAtPlayer()
    {
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

        if (SEManager.Instance != null)
        {
            SEManager.Instance.PlayInteractSound();
        }

        if (talkStartCanvas != null) talkStartCanvas.SetActive(false);

        // 選択肢ボタンの動的生成
        GenerateChoiceButtons();

        if (choiceCanvas != null) choiceCanvas.SetActive(true);
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
            int index = i; // ローカル変数にキャプチャ（ラムダ式クロージャ対策）

            if (data.isAvailable)
            {
                // --- 1. isAvailable == true: 通常ボタンを生成 ---
                GameObject btnObj = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                generatedButtons.Add(btnObj);

                SetButtonText(btnObj, data.choiceText);

                // ボタンのクリックイベント登録
                Button button = btnObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() => OnChoiceSelected(index));
                }
            }
            else
            {
                // --- 2. isAvailable == false: ダミーボタンを生成（指定されている場合） ---
                if (dummyButtonPrefab != null)
                {
                    GameObject dummyObj = Instantiate(dummyButtonPrefab, choiceButtonContainer);
                    generatedButtons.Add(dummyObj);

                    // ダミー用に表示するテキストを選択（設定があれば dummyText、無ければ choiceText）
                    string displayText = !string.IsNullOrEmpty(data.dummyText) ? data.dummyText : data.choiceText;
                    SetButtonText(dummyObj, displayText);

                    
                    Button dummyButton = dummyObj.GetComponent<Button>();
                }
            }
        }
    }

    /// <summary>
    /// ボタンオブジェクトのテキストを設定する共通処理
    /// </summary>
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
            SEManager.Instance.PlayInteractSound();
        }

        // 選択肢画面を閉じる
        if (choiceCanvas != null) choiceCanvas.SetActive(false);

        // この選択肢に対応する Result Canvas が設定されている場合は表示
        if (selectedChoice.resultCanvasForThisChoice != null)
        {
            selectedChoice.resultCanvasForThisChoice.SetActive(true);
            currentResultCanvas = selectedChoice.resultCanvasForThisChoice;
        }
    }

    /// <summary>
    /// 外部の UI ボタンなどから特定の選択肢のフラグ（isAvailable）を変更するメソッド
    /// </summary>
    /// <param name="index">インデックス（0始まり）</param>
    /// <param name="isAvailable">有効化するかどうか</param>
    public void SetChoiceAvailable(int index, bool isAvailable)
    {
        if (index >= 0 && index < choices.Count)
        {
            choices[index].isAvailable = isAvailable;
        }
    }

    /// <summary>
    /// 生成されたボタンをクリア
    /// </summary>
    private void ClearChoiceButtons()
    {
        foreach (GameObject btn in generatedButtons)
        {
            if (btn != null) Destroy(btn);
        }
        generatedButtons.Clear();
    }

    public void CloseAllCanvas()
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