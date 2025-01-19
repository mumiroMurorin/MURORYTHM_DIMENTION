using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// Factoryの初期化に必要なデータ
    /// </summary>
    public class NoteFactoryInitializingData
    {
        public INoteSpawnDataOptionHolder optionHolder { get; set; }

        public GameObject groundObject { get; set; }

        public Deformer groundDeformer { get; set; }
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

    /// <summary>
    /// 各種ノーツデータのListをまとめたもの
    /// </summary>
    public class ChartData
    {
        public List<NoteData_Touch> noteData_Touches { get; set; }

        //public List<NoteData_HoldStart> { get; set; }

        //public List<NoteData_HoldRelay>
        //{ get; set; }

        //public List<NoteData_HoldEnd>
        //{ get; set; }

        //public List<NoteData_HoldMesh>
        //{ get; set; }

        //public List<NoteData_DynamicGround>
        //{ get; set; }

        //public List<NoteData_DynamicSpace>
        //{ get; set; }
    }

    /// <summary>
    /// 判定一覧
    /// </summary>
    public enum Judgement
    {
        None,
        Perfect,
        Great,
        Good,
        Miss
    }

    /// <summary>
    /// ノーツタイプ
    /// </summary>
    public enum NoteType
    {
        Touch,
        HoldStart,
        HoldRelay,
        HoldEnd,
        HoldMesh,
        DynamicGround,
        DynamicSpace
    }
}