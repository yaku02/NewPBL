using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class BGMSlider : MonoBehaviour
{
    private Slider slider;

    private void Awake()
    {
        slider = GetComponent<Slider>();
    }

    private void Start()
    {
        // スライダーの最小値・最大値を設定
        slider.minValue = 0f;
        slider.maxValue = 1f;

        // BGMManager の現在の音量を初期値に反映
        if (BGMManager.Instance != null)
        {
            slider.value = BGMManager.Instance.BGMVolume;
        }

        // スライダー変更時のイベントをコード側から登録
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnDestroy()
    {
        // メモリリーク防止のためイベント登録を解除
        slider.onValueChanged.RemoveListener(OnSliderValueChanged);
    }

    private void OnSliderValueChanged(float value)
    {
        if (BGMManager.Instance != null)
        {
            BGMManager.Instance.SetVolume(value);
        }
    }
}