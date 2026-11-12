using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;


[System.Serializable]
public class DoShakeBuilder
{
    [Header("通常設定")]
    [Tooltip("振動の周期")]
    [SerializeField] float duration = 1f;
    [Tooltip("振動の強さ(振幅)")]
    [SerializeField] float strength = 1f;
    [Tooltip("振動の周波数")]
    [SerializeField] int vibrato = 10;
    [Tooltip("振動のランダム性(変えなくていい)")]
    [SerializeField] float randomness = 90f;
    [Tooltip("振動をだんだん弱くする")]
    [SerializeField] bool isFadeOut = true;
    [Header("ループ設定")]
    [Tooltip("振動回数(-1で無限ループ)")]
    [SerializeField] int shakeTimes = 1;
    [Tooltip("ループタイプ")]
    [SerializeField] LoopType loopType = LoopType.Restart;

    private Tweener shakeTween;

    /// <summary>
    /// 振動の実行
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
