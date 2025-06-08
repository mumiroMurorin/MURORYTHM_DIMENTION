using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class UpDownAnimation : MonoBehaviour
{
    [SerializeField] float duration;
    [SerializeField] float height;
    [SerializeField] bool isDownFirst;

    private Vector3 initialLocalPosition; // 子オブジェクトのローカル位置

    void Start()
    {
        // 子オブジェクトの初期ローカル位置を取得
        initialLocalPosition = transform.localPosition;

        // DoTweenを使用して上下運動を永続的に繰り返す
        transform.DOLocalMoveY(initialLocalPosition.y + height * (isDownFirst ? -1 : 1), duration)
            .SetRelative(false) // ローカル位置に基づいて動作
            .SetEase(Ease.InOutSine) // 滑らかな動き
            .SetLoops(-1, LoopType.Yoyo); // 永久に繰り返し（上下）
    }
}
