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
        public void SetSliderInputMonitor(ISliderInputMonitor inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);
    }

    /// <summary>
    /// スペースの入力状況に依存した判定を行う
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceInputMonitor(ISpaceInputMonitor inputData);

        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder);
    }

    /// <summary>
    /// 全ノーツの基底クラス
    /// </summary>
    public abstract class NoteObject : MonoBehaviour, INoteStarter
    {
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
        }

        /// <summary>
        /// 移動
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
    /// グラウンドノーツオブジェクトを制御する基底クラス
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
    /// ダイナミックノーツの基底クラス
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