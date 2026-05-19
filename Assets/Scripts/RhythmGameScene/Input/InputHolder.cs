using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using JudgementUtil.Dynamic;
using System.Linq;
using static JudgementUtil.SpacaHold.SpaceHoldJudgement;

public class InputHolder : ISliderInputSetter, ISpaceInputSetter, ISliderInputGetter, ISpaceInputGetter
{
    const int SLIDER_MAX_COUNT = 16;
    const int MAX_RECORD_SPACE_INDEX = 60;

    // スライダーからの入力
    ReactiveProperty<bool>[] sliderInput;

    // 空間入力(右手)
    ReactiveCollection<TimeToPos> rightHandInput = new ReactiveCollection<TimeToPos>();
    // 右手の動きのベクトル
    ReactiveProperty<Vector3> rightHandVelocity = new ReactiveProperty<Vector3>();
    // 右手を取得できているか
    ReactiveProperty<bool> canGetRightHand = new ReactiveProperty<bool>();

    // 空間入力(左手)
    ReactiveCollection<TimeToPos> leftHandInput = new ReactiveCollection<TimeToPos>();
    // 左手の動きのベクトル
    ReactiveProperty<Vector3> leftHandVelocity = new ReactiveProperty<Vector3>();
    // 左手を取得できているか
    ReactiveProperty<bool> canGetLeftHand = new ReactiveProperty<bool>();

    public void Initialize()
    {
        sliderInput = new ReactiveProperty<bool>[SLIDER_MAX_COUNT];
        for (int i = 0; i < sliderInput.Length; i++)
        {
            sliderInput[i] = new ReactiveProperty<bool>();
        }
    }

    /// <summary>
    /// (index)番のスライダーを(isEnable)状態にする
    /// </summary>
    /// <param name="index"></param>
    public void SetSliderInput(int index, bool isEnable)
    {
        if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"【Input】Out of range: {index}"); return; }
        if (sliderInput[index].Value == isEnable) { return; }

        sliderInput[index].Value = isEnable;
    }

    /// <summary>
    /// (tag)タグのポジションをposにセット
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="pos"></param>
    public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time)
    {
        switch (tag)
        {
            case SpaceTrackingTag.RightHand:
                var previous = rightHandInput.LastOrDefault();
                var current = new TimeToPos(time, pos);

                rightHandInput.Add(current);
                if (rightHandInput.Count < 2) { break; }

                SetHandVector(previous, current, rightHandVelocity);
                if (rightHandInput.Count > MAX_RECORD_SPACE_INDEX) { rightHandInput.RemoveAt(0); }
                break;

            case SpaceTrackingTag.LeftHand:
                var previous_ = leftHandInput.LastOrDefault();
                var current_ = new TimeToPos(time, pos);

                leftHandInput.Add(current_);
                if (leftHandInput.Count < 2) { break; }

                SetHandVector(previous_, current_, leftHandVelocity);
                if (leftHandInput.Count > MAX_RECORD_SPACE_INDEX) { leftHandInput.RemoveAt(0); }
                break;
            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {tag}");
                return;
        }
    }

    public void SetCanGetSpaceInput(SpaceTrackingTag tag, bool isGet)
    {
        switch (tag)
        {
            case SpaceTrackingTag.RightHand:
                if (canGetRightHand.Value != isGet)
                {
                    canGetRightHand.Value = isGet;
                }
                break;

            case SpaceTrackingTag.LeftHand:
                if (canGetLeftHand.Value != isGet)
                {
                    canGetLeftHand.Value = isGet;
                }
                break;

            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {tag}");
                return;
        }
    }

    public IReadOnlyReactiveProperty<bool> GetCanGetSpaceInputReactiveProperty(SpaceTrackingTag spaceTrackingTag)
    {
        switch (spaceTrackingTag)
        {
            case SpaceTrackingTag.RightHand:
                return canGetRightHand;
            case SpaceTrackingTag.LeftHand:
                return canGetLeftHand;
            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {spaceTrackingTag}");
                return null;
        }
    }

    /// <summary>
    /// スライダー入力(ReactiveProperty)を返す
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index)
    {
        if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"【Input】Out of range: {index}"); return null; }

        return sliderInput[index];
    }

    public IReadOnlyReactiveCollection<TimeToPos> GetSpaceInput(SpaceTrackingTag spaceTrackingTag)
    {
        switch (spaceTrackingTag)
        {
            case SpaceTrackingTag.RightHand:
                return rightHandInput;
            case SpaceTrackingTag.LeftHand:
                return leftHandInput;
            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {spaceTrackingTag}");
                return null;
        }
    }

    /// <summary>
    /// 空間入力(ReactiveProperty)を返す
    /// </summary>
    /// <param name="spaceTrackingTag"></param>
    /// <returns></returns>
    public IReadOnlyReactiveProperty<Vector3> GetSpaceInputVelocity(SpaceTrackingTag spaceTrackingTag)
    {
        switch (spaceTrackingTag)
        {
            case SpaceTrackingTag.RightHand:
                return rightHandVelocity;
            case SpaceTrackingTag.LeftHand:
                return leftHandVelocity;
            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {spaceTrackingTag}");
                return null;
        }
    }

    /// <summary>
    /// 手の座標から動きをベクトル化する
    /// </summary>
    /// <param name="handInput"></param>
    private void SetHandVector(TimeToPos previous, TimeToPos current, ReactiveProperty<Vector3> recorder)
    {
        if (previous.Time == current.Time)
        {
            recorder.Value = Vector3.zero;
            return;
        }

        recorder.Value = DynamicJudgement.CalculateVelocity(previous, current);
    }

    /// <summary>
    /// 引数の範囲に手が入っているかどうか
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    public bool IsInSpaceRange(Vector2[] vertices, float radius = 0)
    {
        return IsInSpaceRange(vertices, SpaceTrackingTag.LeftHand, radius) || IsInSpaceRange(vertices, SpaceTrackingTag.RightHand, radius);
    }

    public bool IsInSpaceRange(Vector2[] vertices, SpaceTrackingTag spaceTrackingTag, float radius = 0)
    {
        var handVectorList = GetSpaceInput(spaceTrackingTag);
        int count = handVectorList.Count;

        if (count < 2) { return false; }

        var pos1 = handVectorList[count - 1].Pos;
        var pos2 = handVectorList[count - 2].Pos;

        return IsSegmentIntersectingOrInsidePolygon(pos1, pos2, vertices)
            || IsCircleIntersectingPolygon(vertices, pos1, radius);
    }
}
