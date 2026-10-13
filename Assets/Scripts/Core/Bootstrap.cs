using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-99999)]
public class GameBootstrapper : MonoBehaviour
{
    private static GameBootstrapper instance;

    [Header("Service Locator")]
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private SettingsManager settingsManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private InputState inputState;
    [Space]
    [SerializeField] private string startScene = "Game Scene";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator Start()
    {
        RegisterServices();

        yield return StartCoroutine(InitializeAllServices());

        SceneManager.LoadScene(startScene);
    }

    private void RegisterServices()
    {
        ServiceLocator.Register<ISettingsManager>(settingsManager);
        ServiceLocator.Register<IAudioManager>(audioManager);
        ServiceLocator.Register<IInputManager>(inputManager);
        ServiceLocator.Register<IInputState>(inputState);
    }

    private IEnumerator InitializeAllServices()
    {
        IAsyncInitializable[] services = new IAsyncInitializable[]
        {
            settingsManager,
            audioManager,
            inputManager,
            inputState
        };

        foreach (var service in services)
        {
            if (service != null)
                yield return StartCoroutine(service.InitializeAsync());
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            ServiceLocator.Clear();
            instance = null;
        }
    }
}