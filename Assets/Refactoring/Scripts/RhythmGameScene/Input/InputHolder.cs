using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using NoteJudgement;
using System.Linq;

namespace Refactoring
{
    public class InputHolder : ISliderInputSetter, ISpaceInputSetter, ISliderInputGetter, ISpaceInputGetter
    {
        const int SLIDER_MAX_COUNT = 16;
        const int MAX_RECORD_SPACE_INDEX = 60;
        //const float VECTOR_MESUREMENT_TIME = 0.1f;

        // �X���C�_�[����̓���
        ReactiveProperty<bool>[] sliderInput;

        // ��ԓ���(�E��)
        LimitedReactiveDictionary<float, Vector3> rightHandInput = new LimitedReactiveDictionary<float, Vector3>(MAX_RECORD_SPACE_INDEX);
        // �E��̓����̃x�N�g��
        ReactiveProperty<Vector3> rightHandVelocity = new ReactiveProperty<Vector3>();

        // ��ԓ���(����)
        LimitedReactiveDictionary<float, Vector3> leftHandInput = new LimitedReactiveDictionary<float, Vector3>(MAX_RECORD_SPACE_INDEX);
        // ����̓����̃x�N�g��
        ReactiveProperty<Vector3> leftHandVelocity = new ReactiveProperty<Vector3>();

        // ��ԓ��͒��H
        ReactiveProperty<bool> canGetSpaceInput = new ReactiveProperty<bool>();
        public IReadOnlyReactiveProperty<bool> CanGetSpaceInputReactiveProperty { get { return canGetSpaceInput; } }

        private CompositeDisposable disposables = new CompositeDisposable();

        public InputHolder()
        {
            sliderInput = new ReactiveProperty<bool>[SLIDER_MAX_COUNT];
            for (int i = 0; i < sliderInput.Length; i++)
            {
                sliderInput[i] = new ReactiveProperty<bool>();
            }

            // ����̓�����Vector������
            rightHandInput.Dictionary.ObserveAdd()
                .Subscribe(_ => SetHandVector(rightHandInput.Dictionary, rightHandVelocity))
                .AddTo(disposables);

            leftHandInput.Dictionary.ObserveAdd()
                .Subscribe(_ => SetHandVector(leftHandInput.Dictionary, leftHandVelocity))
                .AddTo(disposables);
        }

        /// <summary>
        /// (index)�Ԃ̃X���C�_�[��(isEnable)��Ԃɂ���
        /// </summary>
        /// <param name="index"></param>
        public void SetSliderInput(int index, bool isEnable)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"�yInput�zOut of range: {index}"); return; }
            if (sliderInput[index].Value == isEnable) { return; }

            sliderInput[index].Value = isEnable;
        }

        /// <summary>
        /// (tag)�^�O�̃|�W�V������pos�ɃZ�b�g
        /// </summary>
        /// <param name="tag"></param>
        /// <param name="pos"></param>
        public void SetSpaceInput(SpaceTrackingTag tag, Vector3 pos, float time)
        {
            switch (tag)
            {
                case SpaceTrackingTag.RightHand:
                    if (rightHandInput.Dictionary.Count > 0 && 
                        (rightHandInput.Dictionary.Last().Value == pos || rightHandInput.Dictionary.Last().Key == time)) { break; }
                    rightHandInput.Add(time, pos);
                    break;
                case SpaceTrackingTag.LeftHand:
                    if (leftHandInput.Dictionary.Count > 0 &&
                        (leftHandInput.Dictionary.Last().Value == pos || leftHandInput.Dictionary.Last().Key == time)) { break; }
                    leftHandInput.Add(time, pos);
                    break;
                default:
                    Debug.LogWarning($"�yInput�z�ݒ肳��Ă��Ȃ��^�O�ł�: {tag}");
                    return;
            }
        }

        /// <summary>
        /// �̂̃g���b�L���O���o���Ă��邩�Z�b�g
        /// </summary>
        /// <param name="isGet"></param>
        public void SetCanGetSpaceInput(bool isGet)
        {
            if (canGetSpaceInput.Value == isGet) { return; }
            canGetSpaceInput.Value = isGet;
        }

        /// <summary>
        /// �X���C�_�[����(ReactiveProperty)��Ԃ�
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IReadOnlyReactiveProperty<bool> GetSliderInputReactiveProperty(int index)
        {
            if (index >= SLIDER_MAX_COUNT) { Debug.LogWarning($"�yInput�zOut of range: {index}"); return null; }

            return sliderInput[index];
        }

        /// <summary>
        /// ��ԓ���(ReactiveProperty)��Ԃ�
        /// </summary>
        /// <param name="spaceTrackingTag"></param>
        /// <returns></returns>
        public IReadOnlyReactiveDictionary<float,Vector3> GetSpaceInputReactiveDictionary(SpaceTrackingTag spaceTrackingTag)
        {
            switch (spaceTrackingTag)
            {
                case SpaceTrackingTag.RightHand:
                    return rightHandInput.Dictionary;
                case SpaceTrackingTag.LeftHand:
                    return leftHandInput.Dictionary;
                default:
                    Debug.LogWarning($"�yInput�z�ݒ肳��Ă��Ȃ��^�O�ł�: {spaceTrackingTag}");
                    return null;
            }
        }

        /// <summary>
        /// ��ԓ���(ReactiveProperty)��Ԃ�
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
                    Debug.LogWarning($"�yInput�z�ݒ肳��Ă��Ȃ��^�O�ł�: {spaceTrackingTag}");
                    return null;
            }
        }

        /// <summary>
        /// ��̍��W���瓮�����x�N�g��������
        /// </summary>
        /// <param name="handInput"></param>
        private void SetHandVector(IReadOnlyReactiveDictionary<float, Vector3> handInput, ReactiveProperty<Vector3> recorder)
        {
            if(handInput == null || handInput.Count < 2) { return; }

            var previous = handInput.ElementAt(handInput.Count - 2);
            var current = handInput.Last();
            recorder.Value = NoteJudgement.DynamicNote.CalculateVelocity((previous.Key, previous.Value), (current.Key, current.Value));
        }

        public void Dispose()
        {
            disposables.Dispose();
        }
    }
}
