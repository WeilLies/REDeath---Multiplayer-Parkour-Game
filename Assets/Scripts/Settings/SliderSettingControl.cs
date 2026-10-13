using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Slider))]
public class SliderSettingControl : SettingsControlBase
{
    [SerializeField] private Slider slider;
    [SerializeField] private float minValue = 0f;
    [SerializeField] private float maxValue = 1f;

    private SettingsParameter<float> parameter;

    private void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.onValueChanged.AddListener(OnSliderMoved);
    }

    protected override void BindParameter()
    {
        parameter = settingsManager.GetParameter<SettingsParameter<float>>(parameterName);
        if (parameter != null) parameter.OnChanged += Refresh;
    }

    protected override void UnbindParameter()
    {
        if (parameter != null) parameter.OnChanged -= Refresh;
    }

    protected override void Refresh()
    {
        if (parameter == null) return;
        slider.SetValueWithoutNotify(parameter.Value);
    }

    private void OnSliderMoved(float value)
    {
        parameter?.Set(value);
    }
}
