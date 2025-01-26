using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// Factoryの初期化に必要なデータ
    /// ノーツの初期化に必要な共通のデータはここに入れる
    /// </summary>
    public class NoteFactoryInitializingData
    {
        public INoteSpawnDataOptionHolder OptionHolder { get; set; }

        public ISliderInputGetter SliderInputGetter { get; set; }

        public ISpaceInputGetter SpaceInputGetter { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; }

        public GameObject GroundObject { get; set; }

        public Deformer GroundDeformer { get; set; }
    }

    /// <summary>
    /// Perfect～Goodまでの判定許容範囲をまとめたクラス
    /// </summary>
    [System.Serializable]
    public class JudgementWindow
    {
        [Header("それぞれの判定(±n秒)")]
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
    /// Perfect～Goodまでのエフェクトをまとめたクラス
    /// </summary>
    [System.Serializable]
    public class JudgementEffects
    {
        [Header("エフェクト(非アクティブインスタンス済オブジェクト)")]
        [SerializeField] GameObject perfectEffect;
        [SerializeField] GameObject greatEffect;
        [SerializeField] GameObject goodEffect;
        [SerializeField] GameObject missEffect;

        public void SetActive(Judgement judgement)
        {
            switch (judgement)
            {
                case Judgement.Perfect:
                    perfectEffect.SetActive(true);
                    break;
                case Judgement.Great:
                    greatEffect.SetActive(true);
                    break;
                case Judgement.Good:
                    goodEffect.SetActive(true);
                    break;
                case Judgement.Miss:
                    missEffect.SetActive(true);
                    break;
                default:
                    Debug.LogWarning($"【note】設定されていないプロパティです: {judgement}");
                    break;
            }
        }
    }

    /// <summary>
    /// 各種ノーツデータのListをまとめたもの
    /// </summary>
    public class ChartData
    {
        public int MaxCombo { get; set; }

        public List<NoteData_Touch> noteData_Touches { get; set; }

        public List<NoteData_HoldStart> noteData_HoldStarts { get; set; }

        public List<NoteData_HoldRelay> noteData_HoldRelays { get; set; }

        public List<NoteData_HoldEnd> noteData_HoldEnds { get; set; }

        public List<NoteData_HoldMesh> noteData_HoldMeshes { get; set; }

        public List<NoteData_DynamicGroundUpward> noteData_DynamicGroundUpwards { get; set; }

        public List<NoteData_DynamicGroundRightward> noteData_DynamicGroundRightwards { get; set; }

        public List<NoteData_DynamicGroundLeftward> noteData_DynamicGroundLeftwards { get; set; }

        public List<NoteData_DynamicGroundDownward> noteData_DynamicGroundDownwards { get; set; }

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
        DynamicGroundUpward,
        DynamicGroundDownward,
        DynamicGroundRightward,
        DynamicGroundLeftward,
        DynamicSpace
    }
}