using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuButtons : MonoBehaviour
{
    [SerializeField] private Button saveButton;
    [SerializeField] private Button resetButton;

    private ISettingsManager settingsManager;

    private void OnEnable()
    {
        settingsManager = ServiceLocator.Get<ISettingsManager>();
        saveButton.onClick.AddListener(OnSaveClicked);
        resetButton.onClick.AddListener(OnResetClicked);
    }

    private void OnDisable()
    {
        saveButton.onClick.RemoveListener(OnSaveClicked);
        resetButton.onClick.RemoveListener(OnResetClicked);
    }

    private void OnSaveClicked()
    {
        settingsManager?.Save();
    }

    private void OnResetClicked()
    {
        settingsManager?.ResetToDefaults();
        settingsManager?.Save();
    }
}
