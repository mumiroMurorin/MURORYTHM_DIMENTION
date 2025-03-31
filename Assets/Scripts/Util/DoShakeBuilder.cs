using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


[System.Serializable]
public class DoShakeBuilder
{
    [Header("’Êíİ’è")]
    [Tooltip("U“®‚ÌüŠú")]
    [SerializeField] float duration = 1f;
    [Tooltip("U“®‚Ì‹­‚³(U•)")]
    [SerializeField] float strength = 1f;
    [Tooltip("U“®‚Ìü”g”")]
    [SerializeField] int vibrato = 10;
    [Tooltip("U“®‚Ìƒ‰ƒ“ƒ_ƒ€«(•Ï‚¦‚È‚­‚Ä‚¢‚¢)")]
    [SerializeField] float randomness = 90f;
    [Tooltip("U“®‚ğ‚¾‚ñ‚¾‚ñã‚­‚·‚é")]
    [SerializeField] bool isFadeOut = true;
    [Header("ƒ‹[ƒvİ’è")]
    [Tooltip("U“®‰ñ”(-1‚Å–³ŒÀƒ‹[ƒv)")]
    [SerializeField] int shakeTimes = 1;
    [Tooltip("ƒ‹[ƒvƒ^ƒCƒv")]
    [SerializeField] LoopType loopType = LoopType.Restart;

    private Tweener shakeTween;

    /// <summary>
    /// U“®‚ÌÀs
    /// </summary>
    /// <param name="transform"></param>
    public void ApplyShake(Transform transform)
    {
        shakeTween = transform.DOShakePosition(duration, strength, vibrato, randomness, false, isFadeOut)
            .SetLoops(shakeTimes, loopType);
    }

    public void Kill()
    {
        shakeTween?.Kill();
    }
}
