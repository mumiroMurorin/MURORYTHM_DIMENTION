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
        public void SetTimeCounter(ITimeGetter timeer);

        public void SetSliderInputGetter(ISliderInputGetter inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);

        public void SetJudgementData(INoteSliderJudgementData judgeData);
    }

    /// <summary>
    /// �X�y�[�X�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetTimeCounter(ITimeGetter timer);

        public void SetSpaceInputGetter(ISpaceInputGetter inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);

        public void SetJudgementData(INoteSpaceJudgementData judgeData);
    }
    
    /// <summary>
    /// Perfect�`Good�܂ł̔��苖�e�͈͂��܂Ƃ߂��N���X
    /// </summary>
    [System.Serializable]
    public class JudgementWindow
    {
        [SerializeField] float perfectWindow;
        [SerializeField] float greatWindow;
        [SerializeField] float goodWindow;

        public float PerfectWindow { get { return perfectWindow; } }
        public float GreatWindow { get { return greatWindow; } }
        public float GoodWindow { get { return goodWindow; } }

        public Judgement GetJudgement(float timingDiff)
        {
            if(timingDiff < perfectWindow) { return Judgement.Perfect; }
            else if(timingDiff < greatWindow) { return Judgement.Great; }
            else if(timingDiff < goodWindow) { return Judgement.Good; }

            return Judgement.Miss;
        }
    }

    /// <summary>
    /// �S�m�[�c�̊��N���X
    /// </summary>
    public abstract class NoteObject : MonoBehaviour, INoteStarter
    {
        [SerializeField] protected JudgementWindow judgementWindow;

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
            this.gameObject.SetActive(true);
        }

        /// <summary>
        /// �ړ�
        /// </summary>
        protected virtual void Move()
        {
            this.gameObject.transform.position += movingVector * Time.fixedDeltaTime;
        }

        private void Awake()
        {
            this.gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            Move();
        }
    }

    /// <summary>
    /// �O���E���h�m�[�c�I�u�W�F�N�g�𐧌䂷����N���X
    /// </summary>
    public abstract class GroundNoteObject : NoteObject
    {
        
    }

    /// <summary>
    /// �_�C�i�~�b�N�m�[�c�̊��N���X
    /// </summary>
    public abstract class SpaceNoteObject : NoteObject
    {
        
    }
}