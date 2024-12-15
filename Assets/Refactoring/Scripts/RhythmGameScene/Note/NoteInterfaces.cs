using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// ノーツをスタートさせる
    /// </summary>
    public interface INoteStarter
    {
        void SetPosition(Vector3 pos);
     
        void SetActive(bool isActive);

        void StartMovement(Vector3 movingVector);
    }

    /// <summary>
    /// ISliderJudgableで渡す用のパッケージ
    /// </summary>
    public class SliderJudgableDataSet
    {
        public ITimeGetter Timer { get; set; }
        public ISliderInputGetter SliderInput { get; set; }
        public IJudgementRecorder JudgementRecorder { get; set; }
        public INoteSliderJudgementData JudgementData { get; set; }
    }

    /// <summary>
    /// スライダーの入力状況に依存した判定を行う
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetSliderJudgableDatas(SliderJudgableDataSet datas);
    }

    /// <summary>
    /// ISpaceJudgableで渡す用のパッケージ
    /// </summary>
    public class SpaceJudgableDataSet
    {
        public ITimeGetter Timer { get; set; }
        public ISpaceInputGetter SpaceInput { get; set; }
        public IJudgementRecorder JudgementRecorder { get; set; }
        public INoteSpaceJudgementData JudgementData { get; set; }
    }

    /// <summary>
    /// スペースの入力状況に依存した判定を行う
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceJudgableDatas(SpaceJudgableDataSet datas);
    }

    /// <summary>
    /// Perfect～Goodまでの判定許容範囲をまとめたクラス
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
            // Good判定前
            if(judgeTime - goodWindow > currentTime) { return Judgement.None; }
            // Good判定後
            if(judgeTime + goodWindow < currentTime) { return Judgement.Miss; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return Judgement.Perfect; }
            else if(timingDiff <= greatWindow) { return Judgement.Great; }
            else if(timingDiff <= goodWindow) { return Judgement.Good; }

            return Judgement.None;
        }
    }

    /// <summary>
    /// 全ノーツの基底クラス
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
        /// 移動開始
        /// </summary>
        /// <param name="movingVector"></param>
        public virtual void StartMovement(Vector3 movingVector)
        {
            isStartMovement = true;
            this.movingVector = movingVector;
        }

        /// <summary>
        /// 移動
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
    /// タッチノーツ(Slider判定依存)を制御する基底クラス
    /// </summary>
    public abstract class TouchNoteObject : NoteObject, ISliderJudgable
    {
        protected ITimeGetter Timer;
        protected IJudgementRecorder JudgementRecorder;
        protected ISliderInputGetter SliderInputGetter;
        protected INoteSliderJudgementData JudgementData;

        /// <summary>
        /// データセット
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
    /// ダイナミックノーツ(Space判定依存)の基底クラス
    /// </summary>
    public abstract class DynamicNoteObject : NoteObject, ISpaceJudgable
    {
        protected ITimeGetter Timer;
        protected IJudgementRecorder JudgementRecorder;
        protected ISpaceInputGetter SpaceInputGetter;
        protected INoteSpaceJudgementData JudgementData;

        /// <summary>
        /// データセット
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