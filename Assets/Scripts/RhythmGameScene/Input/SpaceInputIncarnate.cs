using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class SpaceInputIncarnate : MonoBehaviour
{
    [SerializeField] HandCaptureObject rightHandCaptureObject;
    [SerializeField] HandCaptureObject leftHandCaptureObject;

    [SerializeField] Vector3 judgeFieldCenter;
    [SerializeField] Vector3 judgeFieldSize;

    ISpaceInputGetter spaceInputGetter;
    INoteSpawnDataOptionHolder spawnOptionGetter;

    [Inject]
    public void Constructor(ISpaceInputGetter spaceInputGetter, INoteSpawnDataOptionHolder spawnOptionGetter)
    {
        this.spaceInputGetter = spaceInputGetter;
        this.spawnOptionGetter = spawnOptionGetter;
    }

    void Start()
    {
        Bind();
    }

    private void Bind()
    {
        // 右手
        spaceInputGetter?.GetSpaceInput(SpaceTrackingTag.RightHand)
            .ObserveAdd()
            .Subscribe(value => MoveCaptureObject(rightHandCaptureObject, value.Value.Pos))
            .AddTo(this.gameObject);

        // 左手
        spaceInputGetter?.GetSpaceInput(SpaceTrackingTag.LeftHand)
            .ObserveAdd()
            .Subscribe(value => MoveCaptureObject(leftHandCaptureObject, value.Value.Pos))
            .AddTo(this.gameObject);

        spawnOptionGetter?.IsAutoModeRP
            .Subscribe(SetActiveCaputureObject)
            .AddTo(this.gameObject);
    }

    /// <summary>
    /// ハンドオブジェクトの位置を動かす
    /// </summary>
    /// <param name="handObject"></param>
    /// <param name="position"></param>
    private void MoveCaptureObject(HandCaptureObject handObject, Vector3 position)
    {
        position = new Vector3(
            judgeFieldCenter.x + position.x * (judgeFieldSize.x / 2f),
            judgeFieldCenter.y + position.y * (judgeFieldSize.y / 2f),
            0
        );

        handObject.OnMoveHandPosition(position);
    }

    private void SetActiveCaputureObject(bool isAutoMode)
    {
        rightHandCaptureObject.gameObject.SetActive(!isAutoMode);
        leftHandCaptureObject.gameObject.SetActive(!isAutoMode);
    }
}
