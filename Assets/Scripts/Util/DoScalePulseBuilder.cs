using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class DoScalePulseBuilder
{
    [Tooltip("‘å‚«‚³”{—¦")]
    [SerializeField] float scaleMultiplier = 1.5f;
    [Tooltip("Œp‘±ŽžŠÔ")]
    [SerializeField] float duration = 0.3f; 

    Vector3 originalScale;
    bool isSetOriginalScale;
    Tween currentTween;

    public void ApplyScalePulse(Transform transform)
    {
        if (!isSetOriginalScale) 
        {
            originalScale = transform.localScale;
            isSetOriginalScale = true;
        }

        // ‚·‚Å‚ÉTween‚ª“®‚¢‚Ä‚¢‚½‚ç’†’f‚µ‚ÄƒŠƒZƒbƒg
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
            transform.localScale = originalScale;
        }

        currentTween = DOTween.Sequence()
            .Append(transform.DOScale(originalScale * scaleMultiplier, duration / 2f).SetEase(Ease.OutQuad))
            .Append(transform.DOScale(originalScale, duration / 2f).SetEase(Ease.InQuad))
            .OnComplete(() => currentTween = null);
    }
}
