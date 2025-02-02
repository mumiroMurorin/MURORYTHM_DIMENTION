using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �X���C�_�[����̓��͂��󂯎��
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
    /// �X�y�[�X�̓��͂��󂯎��
    /// </summary>
    public interface ISpaceInputGetter
    {
        IReadOnlyReactiveDictionary<float, Vector3> GetSpaceInputReactiveDictionary(SpaceTrackingTag spaceTrackingTag);

        IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get; }

        Vector3 GetMaxDifference(float timeRange);
    }
}
