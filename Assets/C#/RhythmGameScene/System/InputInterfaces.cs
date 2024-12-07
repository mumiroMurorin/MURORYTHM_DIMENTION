using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// スライダーからの入力を受け取る
    /// </summary>
    public interface ISliderInputGetter
    {
        IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index);
    }

    public enum SpaceTrackingTag
    {
        RightHand,
        LeftHand,
    }

    /// <summary>
    /// スペースの入力を受け取る
    /// </summary>
    public interface ISpaceInputGetter
    {
        IReadOnlyReactiveProperty<Vector3> GetSpaceInputReactiveProperty(SpaceTrackingTag spaceTrackingTag);

        IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get; }
    }
}
