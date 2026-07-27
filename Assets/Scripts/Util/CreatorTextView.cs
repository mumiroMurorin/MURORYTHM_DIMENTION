using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class CreatorTextView : MonoBehaviour
{
    enum ViewState
    {
        Hidden,
        FadeIn,
        StartDelay,
        Scroll,
        WaitAfterScroll,
        ResetFadeOut,
        ResetFadeIn,
        FadeOut,
        SingleText
    }

    [SerializeField] RectTransform viewport;
    [SerializeField] float scrollSpeed = 40f;
    [SerializeField] float startDelay = 0.8f;
    [SerializeField] float afterScrollDelay = 1.2f;
    [SerializeField] float fadeDuration = 0.25f;
    [SerializeField] float edgePadding = 24f;
    [SerializeField] bool addViewportMaskIfMissing = true;

    readonly List<string> texts = new List<string>();

    TextMeshProUGUI tmp;
    RectTransform textRect;
    Vector2 defaultAnchoredPosition;
    TextAlignmentOptions defaultAlignment;
    TextOverflowModes defaultOverflowMode;
    bool defaultEnableAutoSizing;
    bool defaultEnableWordWrapping;

    ViewState state = ViewState.Hidden;
    int currentIndex;
    float timer;
    float scrollDistance;
    float scrollStartX;
    float scrollEndX;
    float viewportWidth;
    bool isInitialized;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        DisableSiblingScrollingOverflowText();
        ApplyCurrentText();
    }

    private void Update()
    {
        if (!isInitialized) { Initialize(); }

        switch (state)
        {
            case ViewState.FadeIn:
                UpdateFadeIn();
                break;
            case ViewState.StartDelay:
                UpdateStartDelay();
                break;
            case ViewState.Scroll:
                UpdateScroll();
                break;
            case ViewState.WaitAfterScroll:
                UpdateWaitAfterScroll();
                break;
            case ViewState.ResetFadeOut:
                UpdateResetFadeOut();
                break;
            case ViewState.ResetFadeIn:
                UpdateResetFadeIn();
                break;
            case ViewState.FadeOut:
                UpdateFadeOut();
                break;
        }
    }

    public void SetTexts(IEnumerable<string> newTexts)
    {
        Initialize();

        texts.Clear();
        if (newTexts != null)
        {
            foreach (string text in newTexts)
            {
                if (string.IsNullOrWhiteSpace(text)) { continue; }
                texts.Add(text);
            }
        }

        currentIndex = 0;
        ApplyCurrentText();
    }

    public void SetTexts(params string[] newTexts)
    {
        SetTexts((IEnumerable<string>)newTexts);
    }

    public void SetCreators(string composerName, IEnumerable<string> otherCreators)
    {
        List<string> creatorTexts = new List<string>();
        if (!string.IsNullOrWhiteSpace(composerName))
        {
            creatorTexts.Add(composerName);
        }

        if (otherCreators != null)
        {
            foreach (string creator in otherCreators)
            {
                if (string.IsNullOrWhiteSpace(creator)) { continue; }
                creatorTexts.Add(creator);
            }
        }

        SetTexts(creatorTexts);
    }

    private void ApplyCurrentText()
    {
        if (!isActiveAndEnabled || texts.Count == 0)
        {
            SetAlpha(0f);
            state = ViewState.Hidden;
            return;
        }

        tmp.text = texts[currentIndex];
        PrepareTextLayout();
        textRect.anchoredPosition = scrollDistance > 0f
            ? new Vector2(scrollStartX, defaultAnchoredPosition.y)
            : defaultAnchoredPosition;

        if (texts.Count == 1)
        {
            SetAlpha(1f);
            if (scrollDistance > 0f)
            {
                BeginStartDelay();
                return;
            }

            state = ViewState.SingleText;
            return;
        }

        SetAlpha(0f);
        timer = 0f;
        state = ViewState.FadeIn;
    }

    private void PrepareTextLayout()
    {
        viewportWidth = GetViewportWidth();

        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = false;
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.ForceMeshUpdate();

        scrollDistance = Mathf.Max(0f, tmp.preferredWidth - viewportWidth);
        scrollStartX = GetLeftEdgeAlignedPositionX(tmp.preferredWidth, viewportWidth);
        scrollEndX = GetRightEdgeAlignedPositionX(tmp.preferredWidth, viewportWidth);
        if (addViewportMaskIfMissing)
        {
            EnsureViewportMask();
        }
    }

    private void UpdateFadeIn()
    {
        if (fadeDuration <= 0f)
        {
            SetAlpha(1f);
            BeginStartDelay();
            return;
        }

        timer += Time.unscaledDeltaTime;
        SetAlpha(Mathf.Clamp01(timer / fadeDuration));

        if (timer >= fadeDuration)
        {
            BeginStartDelay();
        }
    }

    private void BeginStartDelay()
    {
        SetAlpha(1f);
        timer = startDelay;
        state = ViewState.StartDelay;
    }

    private void UpdateStartDelay()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) { return; }

        if (scrollDistance <= 0f)
        {
            BeginWaitAfterScroll();
            return;
        }

        state = ViewState.Scroll;
    }

    private void UpdateScroll()
    {
        Vector2 pos = textRect.anchoredPosition;
        pos.x -= scrollSpeed * Time.unscaledDeltaTime;

        if (pos.x <= scrollEndX)
        {
            pos.x = scrollEndX;
            textRect.anchoredPosition = pos;
            BeginWaitAfterScroll();
            return;
        }

        textRect.anchoredPosition = pos;
    }

    private void BeginWaitAfterScroll()
    {
        timer = afterScrollDelay;
        state = ViewState.WaitAfterScroll;
    }

    private void UpdateWaitAfterScroll()
    {
        timer -= Time.unscaledDeltaTime;
        if (timer > 0f) { return; }

        if (texts.Count <= 1)
        {
            BeginResetFadeOut();
            return;
        }

        timer = 0f;
        state = ViewState.FadeOut;
    }

    private void UpdateFadeOut()
    {
        if (fadeDuration <= 0f)
        {
            ShowNextText();
            return;
        }

        timer += Time.unscaledDeltaTime;
        SetAlpha(1f - Mathf.Clamp01(timer / fadeDuration));

        if (timer >= fadeDuration)
        {
            ShowNextText();
        }
    }

    private void ShowNextText()
    {
        currentIndex = (currentIndex + 1) % texts.Count;
        ApplyCurrentText();
    }

    private void BeginResetFadeOut()
    {
        if (fadeDuration <= 0f)
        {
            ResetScrollPosition();
            return;
        }

        timer = 0f;
        state = ViewState.ResetFadeOut;
    }

    private void UpdateResetFadeOut()
    {
        timer += Time.unscaledDeltaTime;
        SetAlpha(1f - Mathf.Clamp01(timer / fadeDuration));

        if (timer < fadeDuration) { return; }

        ResetScrollPosition();
    }

    private void ResetScrollPosition()
    {
        textRect.anchoredPosition = new Vector2(scrollStartX, defaultAnchoredPosition.y);

        if (fadeDuration <= 0f)
        {
            SetAlpha(1f);
            BeginStartDelay();
            return;
        }

        timer = 0f;
        state = ViewState.ResetFadeIn;
    }

    private void UpdateResetFadeIn()
    {
        timer += Time.unscaledDeltaTime;
        SetAlpha(Mathf.Clamp01(timer / fadeDuration));

        if (timer < fadeDuration) { return; }

        SetAlpha(1f);
        BeginStartDelay();
    }

    private void SetAlpha(float alpha)
    {
        Color color = tmp.color;
        color.a = alpha;
        tmp.color = color;
    }

    private void Initialize()
    {
        if (isInitialized) { return; }

        tmp = GetComponent<TextMeshProUGUI>();
        textRect = (RectTransform)transform;
        if (viewport == null && transform.parent != null)
        {
            viewport = transform.parent as RectTransform;
        }

        defaultAnchoredPosition = textRect.anchoredPosition;
        defaultAlignment = tmp.alignment;
        defaultOverflowMode = tmp.overflowMode;
        defaultEnableAutoSizing = tmp.enableAutoSizing;
        defaultEnableWordWrapping = tmp.enableWordWrapping;
        isInitialized = true;
    }

    private float GetViewportWidth()
    {
        RectTransform targetViewport = viewport != null ? viewport : textRect;
        return Mathf.Max(0f, targetViewport.rect.width);
    }

    private void EnsureViewportMask()
    {
        if (viewport == null) { return; }
        if (viewport.GetComponent<RectMask2D>() != null) { return; }
        if (viewport.GetComponent<Mask>() != null) { return; }

        viewport.gameObject.AddComponent<RectMask2D>();
    }

    private void OnDisable()
    {
        RestoreDefaults();
    }

    private float GetLeftEdgeAlignedPositionX(float preferredWidth, float currentViewportWidth)
    {
        return defaultAnchoredPosition.x + (preferredWidth - currentViewportWidth) * textRect.pivot.x;
    }

    private float GetRightEdgeAlignedPositionX(float preferredWidth, float currentViewportWidth)
    {
        return defaultAnchoredPosition.x - (preferredWidth - currentViewportWidth) * (1f - textRect.pivot.x);
    }

    private void RestoreDefaults()
    {
        if (!isInitialized) { return; }

        tmp.alignment = defaultAlignment;
        tmp.overflowMode = defaultOverflowMode;
        tmp.enableAutoSizing = defaultEnableAutoSizing;
        tmp.enableWordWrapping = defaultEnableWordWrapping;
        textRect.anchoredPosition = defaultAnchoredPosition;
        SetAlpha(1f);
    }

    private void DisableSiblingScrollingOverflowText()
    {
        ScrollingOverflowText scrollingOverflowText = GetComponent<ScrollingOverflowText>();
        if (scrollingOverflowText == null) { return; }
        if (!scrollingOverflowText.enabled) { return; }

        scrollingOverflowText.enabled = false;
    }
}
