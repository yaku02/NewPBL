using UnityEngine;

// 1. UdonSharpBehaviour ではなく MonoBehaviour に変更
public class TalkCharacter : MonoBehaviour
{
    public GameObject choiceCanvas;
    public GameObject[] textCanvases; // インスペクターで TextCanvas, (1), (2) を登録

    [HideInInspector] public int resultIndex = 0;

    [Header("操作設定")]
    [SerializeField] private KeyCode interactKey = KeyCode.E; // 話しかけるキー設定

    void Update()
    {
        // プレイヤーが近くで E キーを押した時に会話を開始／終了する処理（必要に応じて使用）
        if (Input.GetKeyDown(interactKey))
        {
            // ※必要であればここに「プレイヤーとキャラクターの距離判定」を追加することも可能です
            // Interact();
        }
    }

    // VRChatのInteractの代わり（UIボタンや他のスクリプトから呼び出せます）
    public void Interact()
    {
        if (IsTalking())
        {
            CloseAllCanvases();
        }
        else
        {
            OpenChoice();
        }
    }

    // 選択肢を開く処理
    public void OpenChoice()
    {
        CloseAllCanvases();
        if (choiceCanvas != null)
            choiceCanvas.SetActive(true);
    }

    // 各選択肢ボタンから結果画面へ切り替える処理
    public void ShowResult()
    {
        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);

        // 指定された番号のテキストだけを表示
        if (textCanvases != null && resultIndex >= 0 && resultIndex < textCanvases.Length)
        {
            if (textCanvases[resultIndex] != null)
                textCanvases[resultIndex].SetActive(true);
        }
    }

    public void ShowResult0() { resultIndex = 0; ShowResult(); }
    public void ShowResult1() { resultIndex = 1; ShowResult(); }
    public void ShowResult2() { resultIndex = 2; ShowResult(); }
    public void ShowResult3() { resultIndex = 3; ShowResult(); }

    public void CloseAllCanvases()
    {
        if (choiceCanvas != null)
            choiceCanvas.SetActive(false);

        if (textCanvases != null)
        {
            foreach (GameObject canvas in textCanvases)
            {
                if (canvas != null)
                    canvas.SetActive(false);
            }
        }
    }

    // 現在、会話UIが開いているかどうかを判定する関数
    public bool IsTalking()
    {
        if (choiceCanvas != null && choiceCanvas.activeSelf)
            return true;

        if (textCanvases != null)
        {
            foreach (GameObject canvas in textCanvases)
            {
                if (canvas != null && canvas.activeSelf)
                    return true;
            }
        }

        return false;
    }
}