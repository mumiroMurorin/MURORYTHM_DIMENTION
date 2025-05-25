using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using NoteJudgement;
using System.Linq;

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

    // 空間入力(左手)
    ReactiveCollection<TimeToPos> leftHandInput = new ReactiveCollection<TimeToPos>();
    // 左手の動きのベクトル
    ReactiveProperty<Vector3> leftHandVelocity = new ReactiveProperty<Vector3>();

    // 空間入力中？
    ReactiveProperty<bool> canGetSpaceInput = new ReactiveProperty<bool>();
    public IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get { return canGetSpaceInput; } }

    public void Initialize(GameObject disposable)
    {
        sliderInput = new ReactiveProperty<bool>[SLIDER_MAX_COUNT];
        for (int i = 0; i < sliderInput.Length; i++)
        {
            sliderInput[i] = new ReactiveProperty<bool>();
        }

        // 両手の動きをVector化する
        rightHandInput.ObserveAdd()
            .Pairwise()
            .Subscribe(pair => {
                SetHandVector(pair.Previous.Value, pair.Current.Value, rightHandVelocity);
                if(rightHandInput.Count > MAX_RECORD_SPACE_INDEX) { rightHandInput.RemoveAt(0); }
            })
            .AddTo(disposable);

        leftHandInput.ObserveAdd()
            .Pairwise()
            .Subscribe(pair => {
                SetHandVector(pair.Previous.Value, pair.Current.Value, leftHandVelocity);
                if (leftHandInput.Count > MAX_RECORD_SPACE_INDEX) { leftHandInput.RemoveAt(0); }
            })
            .AddTo(disposable);
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
                rightHandInput.Add(new TimeToPos(time, pos));
                break;
            case SpaceTrackingTag.LeftHand:
                leftHandInput.Add(new TimeToPos(time, pos));
                break;
            default:
                Debug.LogWarning($"【Input】設定されていないタグです: {tag}");
                return;
        }
    }

    /// <summary>
    /// 体のトラッキングが出来ているかセット
    /// </summary>
    /// <param name="isGet"></param>
    public void SetCanGetSpaceInput(bool isGet)
    {
        if (canGetSpaceInput.Value == isGet) { return; }

        canGetSpaceInput.Value = isGet;
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

        recorder.Value = NoteJudgement.DynamicNote.CalculateVelocity(previous, current);
    }
}
