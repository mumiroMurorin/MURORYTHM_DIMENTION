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
        void SetPosition(Vector3 pos);
     
        void SetActive(bool isActive);

        void StartMovement(Vector3 movingVector);
    }

    /// <summary>
    /// ISliderJudgable�œn���p�̃p�b�P�[�W
    /// </summary>
    public class SliderJudgableDataSet
    {
        public ITimeGetter Timer { get; set; }
        public ISliderInputGetter SliderInput { get; set; }
        public IJudgementRecorder JudgementRecorder { get; set; }
        public INoteSliderJudgementData JudgementData { get; set; }
    }

    /// <summary>
    /// �X���C�_�[�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetSliderJudgableDatas(SliderJudgableDataSet datas);
    }

    /// <summary>
    /// ISpaceJudgable�œn���p�̃p�b�P�[�W
    /// </summary>
    public class SpaceJudgableDataSet
    {
        public ITimeGetter Timer { get; set; }
        public ISpaceInputGetter SpaceInput { get; set; }
        public IJudgementRecorder JudgementRecorder { get; set; }
        public INoteSpaceJudgementData JudgementData { get; set; }
    }

    /// <summary>
    /// �X�y�[�X�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceJudgableDatas(SpaceJudgableDataSet datas);
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

        public Judgement GetJudgement(float currentTime, float judgeTime)
        {
            // Good����O
            if(judgeTime - goodWindow > currentTime) { return Judgement.None; }
            // Good�����
            if(judgeTime + goodWindow < currentTime) { return Judgement.Miss; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return Judgement.Perfect; }
            else if(timingDiff <= greatWindow) { return Judgement.Great; }
            else if(timingDiff <= goodWindow) { return Judgement.Good; }

            return Judgement.None;
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

        public void SetPosition(Vector3 pos)
        {
            this.gameObject.transform.position = pos;
        }

        public void SetActive(bool isActive)
        {
            this.gameObject.SetActive(isActive);
        }

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
            if (!isStartMovement) { return; }
            this.gameObject.transform.position += movingVector * Time.fixedDeltaTime;
        }

        private void FixedUpdate()
        {
            Move();
        }

        private void Awake()
        {
            this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// �^�b�`�m�[�c(Slider����ˑ�)�𐧌䂷����N���X
    /// </summary>
    public abstract class TouchNoteObject : NoteObject, ISliderJudgable
    {
        protected ITimeGetter Timer;
        protected IJudgementRecorder JudgementRecorder;
        protected ISliderInputGetter SliderInputGetter;
        protected INoteSliderJudgementData JudgementData;

        /// <summary>
        /// �f�[�^�Z�b�g
        /// </summary>
        /// <param name="datas"></param>
        public void SetSliderJudgableDatas(SliderJudgableDataSet datas)
        {
            Timer = datas.Timer;
            JudgementRecorder = datas.JudgementRecorder;
            SliderInputGetter = datas.SliderInput;
            JudgementData = datas.JudgementData;
        }

    }

    /// <summary>
    /// �_�C�i�~�b�N�m�[�c(Space����ˑ�)�̊��N���X
    /// </summary>
    public abstract class DynamicNoteObject : NoteObject, ISpaceJudgable
    {
        protected ITimeGetter Timer;
        protected IJudgementRecorder JudgementRecorder;
        protected ISpaceInputGetter SpaceInputGetter;
        protected INoteSpaceJudgementData JudgementData;

        /// <summary>
        /// �f�[�^�Z�b�g
        /// </summary>
        /// <param name="datas"></param>
        public void SetSpaceJudgableDatas(SpaceJudgableDataSet datas)
        {
            Timer = datas.Timer;
            JudgementRecorder = datas.JudgementRecorder;
            SpaceInputGetter = datas.SpaceInput;
            JudgementData = datas.JudgementData;
        }

    }
}