using UnityEngine;

public class DummyButton : MonoBehaviour
{
    // クリックされた時に実行したい処理
    public void OnDummyClick()
    {
        Debug.Log("この選択肢はまだ選択できません！");

        // 例: 専用の SE を鳴らす
        if (SEManager.Instance != null)
        {
            SEManager.Instance.PlayErrorSound();
        }
    }
}