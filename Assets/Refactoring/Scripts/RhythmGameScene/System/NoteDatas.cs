using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// Factory�̏������ɕK�v�ȃf�[�^
    /// </summary>
    public class NoteFactoryInitializingData
    {
        public INoteSpawnDataOptionHolder optionHolder { get; set; }

        public GameObject groundObject { get; set; }

        public Deformer groundDeformer { get; set; }
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
            if (judgeTime - goodWindow > currentTime) { return Judgement.None; }
            // Good�����
            if (judgeTime + goodWindow < currentTime) { return Judgement.Miss; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return Judgement.Perfect; }
            else if (timingDiff <= greatWindow) { return Judgement.Great; }
            else if (timingDiff <= goodWindow) { return Judgement.Good; }

            return Judgement.None;
        }
    }

    /// <summary>
    /// �e��m�[�c�f�[�^��List���܂Ƃ߂�����
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
    /// ����ꗗ
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
    /// �m�[�c�^�C�v
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