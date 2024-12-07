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
        void StartMovement(Vector3 movingVector);
    }

    /// <summary>
    /// スライダーの入力状況に依存した判定を行う
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetTimeCounter(ITimeGetter timeer);

        public void SetSliderInputGetter(ISliderInputGetter inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);

        public void SetJudgementData(INoteSliderJudgementData judgeData);
    }

    /// <summary>
    /// スペースの入力状況に依存した判定を行う
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetTimeCounter(ITimeGetter timer);

        public void SetSpaceInputGetter(ISpaceInputGetter inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);

        public void SetJudgementData(INoteSpaceJudgementData judgeData);
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

        public Judgement GetJudgement(float timingDiff)
        {
            if(timingDiff < perfectWindow) { return Judgement.Perfect; }
            else if(timingDiff < greatWindow) { return Judgement.Great; }
            else if(timingDiff < goodWindow) { return Judgement.Good; }

            return Judgement.Miss;
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

        /// <summary>
        /// 移動開始
        /// </summary>
        /// <param name="movingVector"></param>
        public virtual void StartMovement(Vector3 movingVector)
        {
            isStartMovement = true;
            this.movingVector = movingVector;
            this.gameObject.SetActive(true);
        }

        /// <summary>
        /// 移動
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
    /// グラウンドノーツオブジェクトを制御する基底クラス
    /// </summary>
    public abstract class GroundNoteObject : NoteObject
    {
        
    }

    /// <summary>
    /// ダイナミックノーツの基底クラス
    /// </summary>
    public abstract class SpaceNoteObject : NoteObject
    {
        
    }
}