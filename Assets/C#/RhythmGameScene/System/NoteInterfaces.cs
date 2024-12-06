using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// �m�[�c���X�^�[�g������
    /// </summary>
    public interface INoteStarter
    {
        void StartMovement(Vector3 movingVector);
    }

    /// <summary>
    /// �X���C�_�[�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetSliderInputMonitor(ISliderInputMonitor inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);
    }

    /// <summary>
    /// �X�y�[�X�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceInputMonitor(ISpaceInputMonitor inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);
    }

    /// <summary>
    /// �S�m�[�c�̊��N���X
    /// </summary>
    public abstract class NoteObject : MonoBehaviour, INoteStarter
    {
        protected bool isStartMovement;
        protected Vector3 movingVector;

        /// <summary>
        /// �ړ��J�n
        /// </summary>
        /// <param name="movingVector"></param>
        public virtual void StartMovement(Vector3 movingVector)
        {
            isStartMovement = true;
            this.movingVector = movingVector;
        }

        /// <summary>
        /// �ړ�
        /// </summary>
        protected virtual void Move()
        {
            this.gameObject.transform.position += movingVector * Time.fixedDeltaTime;
        }

        private void FixedUpdate()
        {
            Move();
        }
    }

    /// <summary>
    /// �O���E���h�m�[�c�I�u�W�F�N�g�𐧌䂷����N���X
    /// </summary>
    public abstract class GroundNoteObject : NoteObject, ISliderJudgable
    {
        protected IJudgementRecorder judgementRecorder;

        void ISliderJudgable.SetJudgementRecorder(IJudgementRecorder judgementRecorder)
        {
            this.judgementRecorder = judgementRecorder;
        }

        public abstract void SetSliderInputMonitor(ISliderInputMonitor inputData);
    }

    /// <summary>
    /// �_�C�i�~�b�N�m�[�c�̊��N���X
    /// </summary>
    public abstract class DynamicNoteObject : NoteObject , ISpaceJudgable
    {
        protected IJudgementRecorder judgementRecorder;

        void ISpaceJudgable.SetJudgementRecorder(IJudgementRecorder judgementRecorder)
        {
            this.judgementRecorder = judgementRecorder;
        }

        public abstract void SetSpaceInputMonitor(ISpaceInputMonitor inputData);

    }
}