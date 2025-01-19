using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// スライダーの入力状況に依存した判定を行う
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetSliderInput(ISliderInputGetter input);
    }

    /// <summary>
    /// 空間の入力状況に依存した判定を行う
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceInput(ISpaceInputGetter input);
    }

    /// <summary>
    /// 各種ノーツデータの基となるインターフェース
    /// </summary>
    public interface INoteData
    {
        //public float Timing { get; set; }

        //public ITimeGetter Timer { get; set; }

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
            if (judgeTime - goodWindow > currentTime) { return Judgement.None; }
            // Good判定後
            if (judgeTime + goodWindow < currentTime) { return Judgement.Miss; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return Judgement.Perfect; }
            else if (timingDiff <= greatWindow) { return Judgement.Great; }
            else if (timingDiff <= goodWindow) { return Judgement.Good; }

            return Judgement.None;
        }
    }

    public enum Judgement
    {
        None,
        Perfect,
        Great,
        Good,
        Miss
    }
}
