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
    [SerializeField] bool addViewportMaskIfMissing = true;
    [SerializeField] bool scrollFromRightEdge = false;

    TextMeshProUGUI tmp;
    RectTransform textRect;
    Vector2 defaultAnchoredPosition;
    TextAlignmentOptions defaultAlignment;
    TextOverflowModes defaultOverflowMode;
    bool defaultEnableAutoSizing;
    bool defaultEnableWordWrapping;

    string lastText;
    float lastViewportWidth;
    float lastPreferredWidth;
    float scrollStartX;
    float scrollEndX;
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

        if (delayTimer > 0f)
        {
            delayTimer -= Time.unscaledDeltaTime;
            return;
        }

        Vector2 pos = textRect.anchoredPosition;
        pos.x -= scrollSpeed * Time.unscaledDeltaTime;

        if (pos.x <= GetEndPositionX())
        {
            pos.x = GetEndPositionX();
            textRect.anchoredPosition = pos;
            BeginWaitAfterScroll();
            return;
        }

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

        string currentText = tmp.text;
        float currentViewportWidth = GetViewportWidth();

        tmp.enableAutoSizing = false;
        tmp.enableWordWrapping = false;

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
            return;
        }

        if (addViewportMaskIfMissing)
        {
            EnsureViewportMask();
        }

        isScrolling = true;
        scrollStartX = GetLeftEdgeAlignedPositionX(preferredWidth, currentViewportWidth);
        scrollEndX = GetRightEdgeAlignedPositionX(preferredWidth, currentViewportWidth);
        delayTimer = startDelay;

        tmp.alignment = TextAlignmentOptions.Left;
        tmp.overflowMode = TextOverflowModes.Overflow;
        textRect.anchoredPosition = new Vector2(GetStartPositionX(currentViewportWidth), defaultAnchoredPosition.y);
    }

    private void StopScrolling()
    {
        isScrolling = false;
        isWaitingAfterScroll = false;
        isResetFadingOut = false;
        isResetFadingIn = false;
        delayTimer = 0f;
        scrollStartX = defaultAnchoredPosition.x;
        scrollEndX = defaultAnchoredPosition.x;

        tmp.alignment = defaultAlignment;
        tmp.overflowMode = defaultOverflowMode;
        tmp.enableAutoSizing = defaultEnableAutoSizing;
        tmp.enableWordWrapping = defaultEnableWordWrapping;
        textRect.anchoredPosition = defaultAnchoredPosition;
        SetAlpha(1f);
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
        if (!scrollFromRightEdge) { return scrollStartX; }

        return defaultAnchoredPosition.x + currentViewportWidth + edgePadding;
    }

    private float GetEndPositionX()
    {
        return scrollEndX;
    }

    private float GetLeftEdgeAlignedPositionX(float preferredWidth, float currentViewportWidth)
    {
        return defaultAnchoredPosition.x + (preferredWidth - currentViewportWidth) * textRect.pivot.x;
    }

    private float GetRightEdgeAlignedPositionX(float preferredWidth, float currentViewportWidth)
    {
        return defaultAnchoredPosition.x - (preferredWidth - currentViewportWidth) * (1f - textRect.pivot.x);
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
        delayTimer = startDelay;

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
}
