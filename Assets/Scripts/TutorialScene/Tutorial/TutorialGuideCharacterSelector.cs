using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

public class TutorialGuideCharacterSelector : MonoBehaviour
{
    [SerializeField] GameObject rootObject;
    [SerializeField] TextMeshProUGUI selectedCharacterText;
    [SerializeField] TutorialGuideCharacterType defaultCharacterType = TutorialGuideCharacterType.Shikiboo;
    [SerializeField] OptionDataSetter optionDataSetter;

    IOptionGetter optionGetter;
    IOptionSetter optionSetter;
    Action onConfirmed;
    TutorialGuideCharacterType selectedCharacterType;

    [Inject]
    public void Construct(IOptionGetter optionGetter, IOptionSetter optionSetter)
    {
        this.optionGetter = optionGetter;
        this.optionSetter = optionSetter;
    }

    void Awake()
    {
        SetVisible(false);
    }

    public void BeginSelect(Action onConfirmed)
    {
        this.onConfirmed = onConfirmed;
        selectedCharacterType = optionGetter != null
            ? optionGetter.CurrentTutorialGuideCharacterType.Value
            : defaultCharacterType;

        SetVisible(true);
        ApplySelection();
    }

    public void SelectCreation()
    {
        Select(TutorialGuideCharacterType.Creation);
    }

    public void SelectDestruction()
    {
        Select(TutorialGuideCharacterType.Destruction);
    }

    public void SelectShikiboo()
    {
        Select(TutorialGuideCharacterType.Shikiboo);
    }

    public void Select(TutorialGuideCharacterType characterType)
    {
        selectedCharacterType = characterType;
        ApplySelection();
    }

    public void Confirm()
    {
        SetOption(selectedCharacterType);
        SetVisible(false);
        onConfirmed?.Invoke();
        onConfirmed = null;
    }

    public void Cancel()
    {
        selectedCharacterType = defaultCharacterType;
        Confirm();
    }

    void ApplySelection()
    {
        SetOption(selectedCharacterType);

        if (selectedCharacterText != null)
        {
            selectedCharacterText.text = selectedCharacterType.ToString();
        }
    }

    void SetVisible(bool visible)
    {
        GameObject target = rootObject != null ? rootObject : gameObject;
        target.SetActive(visible);
    }

    void SetOption(TutorialGuideCharacterType characterType)
    {
        if (optionSetter != null)
        {
            optionSetter.SetCurrentTutorialGuideCharacterType(characterType);
            return;
        }

        if (optionDataSetter == null)
        {
            optionDataSetter = FindObjectOfType<OptionDataSetter>();
        }

        optionDataSetter?.SetCurrentTutorialGuideCharacterType(characterType);
    }

    public static TutorialGuideCharacterSelector CreateDefault()
    {
        EnsureEventSystem();

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TutorialGuideCharacterCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
        }

        GameObject panel = new GameObject("TutorialGuideCharacterSelector", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.6f);

        TutorialGuideCharacterSelector selector = panel.AddComponent<TutorialGuideCharacterSelector>();
        selector.rootObject = panel;
        selector.selectedCharacterText = CreateText(panel.transform, "SelectedCharacterText", "Shikiboo", new Vector2(0f, 170f), 54);

        CreateButton(panel.transform, "CreationButton", "Creation", new Vector2(-360f, -40f), selector.SelectCreation);
        CreateButton(panel.transform, "ShikibooButton", "Shikiboo", new Vector2(0f, -40f), selector.SelectShikiboo);
        CreateButton(panel.transform, "DestructionButton", "Destruction", new Vector2(360f, -40f), selector.SelectDestruction);
        CreateButton(panel.transform, "ConfirmButton", "OK", new Vector2(0f, -250f), selector.Confirm);

        panel.SetActive(false);
        return selector;
    }

    static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null) { return; }

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    static TextMeshProUGUI CreateText(Transform parent, string objectName, string text, Vector2 anchoredPosition, float fontSize)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);

        RectTransform rectTransform = textObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(720f, 120f);
        rectTransform.anchoredPosition = anchoredPosition;

        TextMeshProUGUI tmp = textObject.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return tmp;
    }

    static void CreateButton(Transform parent, string objectName, string text, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(280f, 96f);
        rectTransform.anchoredPosition = anchoredPosition;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.9f);

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(onClick);

        TextMeshProUGUI label = CreateText(buttonObject.transform, "Label", text, Vector2.zero, 32);
        label.color = Color.black;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
