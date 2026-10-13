using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ArrowSettingControl : SettingsControlBase
{
    [SerializeField] private Button previousButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text valueLabel;

    private IndexedParameter parameter;

    private void Awake()
    {
        previousButton.onClick.AddListener(() => parameter?.Step(-1));
        nextButton.onClick.AddListener(() => parameter?.Step(1));
    }

    protected override void BindParameter()
    {
        parameter = settingsManager.GetParameter<IndexedParameter>(parameterName);
        if (parameter != null) parameter.OnChanged += Refresh;
    }

    protected override void UnbindParameter()
    {
        if (parameter != null) parameter.OnChanged -= Refresh;
    }

    protected override void Refresh()
    {
        if (parameter == null) return;
        valueLabel.text = parameter.Current.ToUpperInvariant();
    }
}
