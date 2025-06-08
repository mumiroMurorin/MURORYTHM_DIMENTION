using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class RotationAnimation : MonoBehaviour
{
    [SerializeField] private float duration = 1f;                 
    [SerializeField] private Vector3 rotationAxis = Vector3.up;  
    [SerializeField] private bool isClockwise = true;            

    private void Start()
    {
        float direction = isClockwise ? 1f : -1f;
        Vector3 rotationPerLoop = rotationAxis.normalized * 360f * direction;

        // 現在のローカル回転から相対的に回転し続ける
        transform.DOLocalRotate(rotationPerLoop, duration, RotateMode.Fast)
            .SetRelative(true)                // 相対回転で積み重ねる
            .SetEase(Ease.Linear)            // 等速回転
            .SetLoops(-1, LoopType.Restart); // 無限ループ
    }
}