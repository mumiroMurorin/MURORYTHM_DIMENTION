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

        IReadOnlyReactiveProperty<Vector3> GetSpaceInputVelocity(SpaceTrackingTag spaceTrackingTag);

        IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get; }
    }

    public interface ISliderInputSetter
    {
        public void SetSliderInput(int index, bool isEnable);

        public void Dispose();
    }

    public interface ISpaceInputSetter
    {
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time);

        public void SetCanGetSpaceInput(bool isGet);

        public void Dispose();
    }
}
