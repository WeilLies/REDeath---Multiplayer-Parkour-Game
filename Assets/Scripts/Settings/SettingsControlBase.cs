using UnityEngine;

public abstract class SettingsControlBase : MonoBehaviour
{
    [SerializeField] protected SettingsKeys.AllSettingsName ParameterName;
    protected string parameterName;
    protected ISettingsManager settingsManager;

    protected virtual void OnEnable()
    {
        parameterName = ParameterName.ToString();
        settingsManager = ServiceLocator.Get<ISettingsManager>();
        if (settingsManager == null) return;

        BindParameter();
        Refresh();
    }

    protected virtual void OnDisable()
    {
        UnbindParameter();
    }

    protected abstract void BindParameter();
    protected abstract void UnbindParameter();
    protected abstract void Refresh();
}
