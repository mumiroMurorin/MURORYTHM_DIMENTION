using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

[System.Serializable]
public class DoScalePulseBuilder
{
    [Tooltip("大きさ倍率")]
    [SerializeField] float scaleMultiplier = 1.5f;
    [Tooltip("継続時間")]
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

        // すでにTweenが動いていたら中断してリセット
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
