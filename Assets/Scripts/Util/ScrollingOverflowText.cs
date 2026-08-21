using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class ScrollingOverflowText : MonoBehaviour
{
    [SerializeField] RectTransform viewport;
    [SerializeField] float scrollSpeed = 40f;
    [SerializeField] float startDelay = 0.8f;
    [SerializeField] float restartDelay = 0.8f;
    [SerializeField] float resetFadeDuration = 0.25f;
    [SerializeField] float edgePadding = 24f;
    [SerializeField] int loopGapSpaces = 8;
    [SerializeField] bool addViewportMaskIfMissing = true;
    [SerializeField] bool scrollFromRightEdge = false;

    TextMeshProUGUI tmp;
    RectTransform textRect;
    ContentSizeFitter contentSizeFitter;
    Vector2 defaultAnchoredPosition;
    Vector2 defaultAnchorMin;
    Vector2 defaultAnchorMax;
    Vector2 defaultSizeDelta;
    Vector2 defaultPivot;
    TextAlignmentOptions defaultAlignment;
    TextOverflowModes defaultOverflowMode;
    bool defaultEnableAutoSizing;
    bool defaultEnableWordWrapping;
    bool defaultRichText;
    bool defaultContentSizeFitterEnabled;

    string lastText;
    string loopDisplayText;
    float lastViewportWidth;
    float lastPreferredWidth;
    float scrollStartX;
    float loopDistance;
    float loopResetX;
    float delayTimer;
    bool isScrolling;
    bool isInitialized;
    bool isRefreshQueued;
    bool isRefreshing;
    bool isWaitingAfterScroll;
    bool isResetFadingOut;
    bool isResetFadingIn;
    float resetFadeTimer;

    private void Awake()
    {
        Initialize();
    }

    private void OnEnable()
    {
        Initialize();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        QueueRefresh();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void Update()
    {
        if (!isInitialized) { Initialize(); }

        float currentViewportWidth = GetViewportWidth();
        if (!Mathf.Approximately(lastViewportWidth, currentViewportWidth))
        {
            QueueRefresh();
        }

        if (!isScrolling) { return; }

        if (isWaitingAfterScroll)
        {
            UpdateWaitAfterScroll();
            return;
        }

        if (isResetFadingOut)
        {
            UpdateResetFadeOut();
            return;
        }

        if (isResetFadingIn)
        {
            UpdateResetFadeIn();
            return;
        }

        Vector2 pos = textRect.anchoredPosition;
        pos.x = GetScrollPositionX();
        textRect.anchoredPosition = pos;
    }

    private void LateUpdate()
    {
        if (!isRefreshQueued) { return; }

        isRefreshQueued = false;
        Refresh();
    }

    public void Refresh()
    {
        Initialize();

        string currentText = GetSourceText();
        float currentViewportWidth = GetViewportWidth();

        SetTextWithoutRefresh(currentText);
        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = false;
        tmp.richText = true;

        isRefreshing = true;
        tmp.ForceMeshUpdate();
        isRefreshing = false;

        float preferredWidth = tmp.preferredWidth;
        float overflowWidth = preferredWidth - currentViewportWidth;
        bool isSameLayout =
            isScrolling &&
            currentText == lastText &&
            Mathf.Approximately(currentViewportWidth, lastViewportWidth) &&
            Mathf.Approximately(preferredWidth, lastPreferredWidth);

        lastText = currentText;
        lastViewportWidth = currentViewportWidth;
        lastPreferredWidth = preferredWidth;

        if (overflowWidth <= 0f)
        {
            StopScrolling();
            return;
        }

        if (isSameLayout)
        {
            SetTextWithoutRefresh(loopDisplayText);
            textRect.anchoredPosition = new Vector2(GetScrollPositionX(), defaultAnchoredPosition.y);
            return;
        }

        if (addViewportMaskIfMissing)
        {
            EnsureViewportMask();
        }

        loopDisplayText = BuildLoopDisplayText(currentText);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Overflow;
        ApplyScrollingRectLayout();
        SetTextWithoutRefresh(loopDisplayText);
        ForceMeshUpdateWithoutRefresh();

        isScrolling = true;
        isWaitingAfterScroll = false;
        isResetFadingOut = false;
        isResetFadingIn = false;
        scrollStartX = CalculateLeftAlignedStartPositionX();
        loopDistance = CalculateLoopDistance(currentText);
        loopResetX = scrollStartX - loopDistance;
        delayTimer = 0f;

        SetAlpha(1f);
        textRect.anchoredPosition = new Vector2(GetScrollPositionX(), defaultAnchoredPosition.y);
    }

    private void StopScrolling()
    {
        isScrolling = false;
        isWaitingAfterScroll = false;
        isResetFadingOut = false;
        isResetFadingIn = false;
        delayTimer = 0f;
        scrollStartX = defaultAnchoredPosition.x;
        loopDistance = 0f;
        loopResetX = defaultAnchoredPosition.x;

        tmp.alignment = defaultAlignment;
        tmp.overflowMode = defaultOverflowMode;
        tmp.enableAutoSizing = defaultEnableAutoSizing;
        tmp.enableWordWrapping = defaultEnableWordWrapping;
        tmp.richText = defaultRichText;
        SetTextWithoutRefresh(lastText);
        loopDisplayText = null;
        RestoreDefaultRectLayout();
        textRect.anchoredPosition = defaultAnchoredPosition;
        SetAlpha(1f);
    }

    private void Initialize()
    {
        if (isInitialized) { return; }

        tmp = GetComponent<TextMeshProUGUI>();
        textRect = (RectTransform)transform;
        contentSizeFitter = GetComponent<ContentSizeFitter>();
        if (viewport == null && transform.parent != null)
        {
            viewport = transform.parent as RectTransform;
        }

        defaultAnchoredPosition = textRect.anchoredPosition;
        defaultAnchorMin = textRect.anchorMin;
        defaultAnchorMax = textRect.anchorMax;
        defaultSizeDelta = textRect.sizeDelta;
        defaultPivot = textRect.pivot;
        defaultAlignment = tmp.alignment;
        defaultOverflowMode = tmp.overflowMode;
        defaultEnableAutoSizing = tmp.enableAutoSizing;
        defaultEnableWordWrapping = tmp.enableWordWrapping;
        defaultRichText = tmp.richText;
        defaultContentSizeFitterEnabled = contentSizeFitter != null && contentSizeFitter.enabled;
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

    private void OnRectTransformDimensionsChange()
    {
        if (!isActiveAndEnabled || !isInitialized) { return; }
        if (Mathf.Approximately(lastViewportWidth, GetViewportWidth())) { return; }

        QueueRefresh();
    }

    private void OnTextChanged(Object changedObject)
    {
        if (isRefreshing) { return; }
        if (changedObject != tmp) { return; }

        QueueRefresh();
    }

    private void QueueRefresh()
    {
        isRefreshQueued = true;
    }

    private float GetStartPositionX(float currentViewportWidth)
    {
        return scrollStartX;
    }

    private float GetScrollPositionX()
    {
        if (loopDistance <= 0f)
        {
            return scrollStartX;
        }

        float scrollDistance = Mathf.Repeat(Time.time * scrollSpeed, loopDistance);
        return scrollStartX - scrollDistance;
    }

    private void BeginResetFadeOut()
    {
        if (resetFadeDuration <= 0f)
        {
            ResetScrollPosition();
            return;
        }

        resetFadeTimer = 0f;
        isResetFadingOut = true;
    }

    private void BeginWaitAfterScroll()
    {
        delayTimer = restartDelay;
        isWaitingAfterScroll = true;
    }

    private void UpdateWaitAfterScroll()
    {
        delayTimer -= Time.unscaledDeltaTime;
        if (delayTimer > 0f) { return; }

        isWaitingAfterScroll = false;
        BeginResetFadeOut();
    }

    private void UpdateResetFadeOut()
    {
        resetFadeTimer += Time.unscaledDeltaTime;
        SetAlpha(1f - Mathf.Clamp01(resetFadeTimer / resetFadeDuration));

        if (resetFadeTimer < resetFadeDuration) { return; }

        isResetFadingOut = false;
        ResetScrollPosition();
    }

    private void ResetScrollPosition()
    {
        textRect.anchoredPosition = new Vector2(GetStartPositionX(lastViewportWidth), defaultAnchoredPosition.y);
        delayTimer = 0f;

        if (resetFadeDuration <= 0f)
        {
            SetAlpha(1f);
            return;
        }

        resetFadeTimer = 0f;
        isResetFadingIn = true;
    }

    private void UpdateResetFadeIn()
    {
        resetFadeTimer += Time.unscaledDeltaTime;
        SetAlpha(Mathf.Clamp01(resetFadeTimer / resetFadeDuration));

        if (resetFadeTimer < resetFadeDuration) { return; }

        SetAlpha(1f);
        isResetFadingIn = false;
    }

    private void SetAlpha(float alpha)
    {
        Color color = tmp.color;
        color.a = alpha;
        tmp.color = color;
    }

    private string GetSourceText()
    {
        if (!string.IsNullOrEmpty(loopDisplayText) && tmp.text == loopDisplayText)
        {
            return lastText;
        }

        return tmp.text;
    }

    private string BuildLoopDisplayText(string sourceText)
    {
        return $"{sourceText}{BuildLoopGap()}{sourceText}";
    }

    private string BuildLoopGap()
    {
        return new string(' ', Mathf.Max(0, loopGapSpaces));
    }

    private void SetTextWithoutRefresh(string text)
    {
        isRefreshing = true;
        tmp.text = text;
        isRefreshing = false;
    }

    private void ForceMeshUpdateWithoutRefresh()
    {
        isRefreshing = true;
        tmp.ForceMeshUpdate();
        isRefreshing = false;
    }

    private void ApplyScrollingRectLayout()
    {
        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = false;
        }

        if (viewport == null || textRect.parent != viewport)
        {
            textRect.anchoredPosition = defaultAnchoredPosition;
            return;
        }

        textRect.anchorMin = new Vector2(0f, defaultAnchorMin.y);
        textRect.anchorMax = new Vector2(0f, defaultAnchorMax.y);
        textRect.pivot = new Vector2(0f, defaultPivot.y);
        textRect.sizeDelta = new Vector2(GetViewportWidth(), defaultSizeDelta.y);
        textRect.anchoredPosition = new Vector2(0f, defaultAnchoredPosition.y);
    }

    private void RestoreDefaultRectLayout()
    {
        textRect.anchorMin = defaultAnchorMin;
        textRect.anchorMax = defaultAnchorMax;
        textRect.sizeDelta = defaultSizeDelta;
        textRect.pivot = defaultPivot;

        if (contentSizeFitter != null)
        {
            contentSizeFitter.enabled = defaultContentSizeFitterEnabled;
        }
    }

    private float CalculateLeftAlignedStartPositionX()
    {
        if (!TryGetFirstVisibleCharacterLeftInTextParentSpace(out float characterLeftX))
        {
            return defaultAnchoredPosition.x;
        }

        float offsetToViewportLeft = GetViewportLeftInTextParentSpace() - characterLeftX;
        return textRect.anchoredPosition.x + offsetToViewportLeft;
    }

    private float CalculateLoopDistance(string sourceText)
    {
        if (!TryGetFirstVisibleCharacterLeftInTextParentSpace(out float firstCharacterLeftX))
        {
            return lastPreferredWidth;
        }

        int secondTextStartIndex = sourceText.Length + Mathf.Max(0, loopGapSpaces);
        if (!TryGetFirstVisibleCharacterLeftInTextParentSpace(secondTextStartIndex, out float secondCharacterLeftX))
        {
            return lastPreferredWidth;
        }

        return Mathf.Max(1f, secondCharacterLeftX - firstCharacterLeftX);
    }

    private bool TryGetFirstVisibleCharacterLeftInTextParentSpace(out float characterLeftX)
    {
        return TryGetFirstVisibleCharacterLeftInTextParentSpace(0, out characterLeftX);
    }

    private bool TryGetFirstVisibleCharacterLeftInTextParentSpace(int minSourceIndex, out float characterLeftX)
    {
        characterLeftX = 0f;
        if (textRect.parent == null) { return false; }

        TMP_TextInfo textInfo = tmp.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo character = textInfo.characterInfo[i];
            if (character.index < minSourceIndex) { continue; }
            if (!character.isVisible) { continue; }

            Vector3 characterLeftWorld = textRect.TransformPoint(character.bottomLeft);
            characterLeftX = textRect.parent.InverseTransformPoint(characterLeftWorld).x;
            return true;
        }

        return false;
    }

    private float GetViewportLeftInTextParentSpace()
    {
        RectTransform targetViewport = viewport != null ? viewport : textRect;
        if (textRect.parent == null)
        {
            return targetViewport.rect.xMin;
        }

        Vector3[] corners = new Vector3[4];
        targetViewport.GetWorldCorners(corners);
        return textRect.parent.InverseTransformPoint(corners[0]).x;
    }
}
