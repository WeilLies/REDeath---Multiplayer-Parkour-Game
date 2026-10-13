using FMODUnity;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonAnimationUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Color textTargetColor = Color.blue;
    [SerializeField] private Sprite targetSprite;
    [SerializeField] private EventReference clickSound;
    [SerializeField] private EventReference highlightSound;

    private TextMeshProUGUI currentText;
    private Image currentImage;
    private Color textStartColor;
    private Sprite startSprite;

    private IAudioManager audioManager;

    private void Awake()
    {
        audioManager = ServiceLocator.Get<IAudioManager>();

        currentText = FindTextObject();

        currentImage = GetComponent<Image>();

        textStartColor = currentText != null ? currentText.color : Color.white;

        startSprite = currentImage.sprite;
    }

    private TextMeshProUGUI FindTextObject()
    {
        return GetComponentInChildren<TextMeshProUGUI>(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioManager != null && !highlightSound.IsNull)
            audioManager.PlayOneShot(highlightSound, transform.position);

        if (currentText == null) return;

        currentText.color = textTargetColor;

        if (currentImage == null) return;

        currentImage.sprite = targetSprite;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (currentText == null) return;

        currentText.color = textStartColor;

        if (currentImage == null) return;

        currentImage.sprite = startSprite;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (audioManager != null && !clickSound.IsNull)
            audioManager.PlayOneShot(clickSound, transform.position);
    }
}