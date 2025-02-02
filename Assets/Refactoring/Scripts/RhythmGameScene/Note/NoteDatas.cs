using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    /// <summary>
    /// Factory�̏������ɕK�v�ȃf�[�^
    /// �m�[�c�̏������ɕK�v�ȋ��ʂ̃f�[�^�͂����ɓ����
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
    /// Perfect�`Good�܂ł̔��苖�e�͈͂��܂Ƃ߂��N���X
    /// </summary>
    [System.Serializable]
    public class JudgementWindow
    {
        [Header("���ꂼ��̔���(�}n�b)")]
        [SerializeField] float perfectWindow;
        [SerializeField] float greatWindow;
        [SerializeField] float goodWindow;

        public float PerfectWindow { get { return perfectWindow; } }
        public float GreatWindow { get { return greatWindow; } }
        public float GoodWindow { get { return goodWindow; } }

        public Judgement GetJudgement(float currentTime, float judgeTime)
        {
            return GetJudgementAndError(currentTime, judgeTime).Judgement;
        }

        public JudgementAndErrorTime GetJudgementAndError(float currentTime, float judgeTime)
        {
            float error = judgeTime - currentTime;

            // Good����O
            if (judgeTime - goodWindow > currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.None,Error = error }; }
            // Good�����
            if (judgeTime + goodWindow < currentTime) { return new JudgementAndErrorTime { Judgement = Judgement.Miss, Error = error }; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Perfect, Error = error }; }
            else if (timingDiff <= greatWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Great, Error = error }; }
            else if (timingDiff <= goodWindow) { return new JudgementAndErrorTime { Judgement = Judgement.Good, Error = error }; }

            return new JudgementAndErrorTime { Judgement = Judgement.None };
        }
    }

    /// <summary>
    /// Perfect�`Good�܂ł̃G�t�F�N�g���܂Ƃ߂��N���X
    /// </summary>
    [System.Serializable]
    public class JudgementEffects
    {
        [Header("�G�t�F�N�g(��A�N�e�B�u�C���X�^���X�σI�u�W�F�N�g)")]
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
                    Debug.LogWarning($"�ynote�z�ݒ肳��Ă��Ȃ��v���p�e�B�ł�: {judgement}");
                    break;
            }
        }
    }

    public struct JudgementAndErrorTime
    {
        public Judgement Judgement { get; set; }

        public float Error { get; set; }
    }

    /// <summary>
    /// �m�[�c�̔������Z�߂��f�[�^
    /// </summary>
    public class NoteJudgementData
    {
        public INoteData NoteData { get; set; }

        public Judgement Judgement { get; set; }

        public float TimingError { get; set; }

        public Vector3 PositionJudged { get; set; }
    }

    /// <summary>
    /// �e��m�[�c�f�[�^��List���܂Ƃ߂�����
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
        DynamicGroundUpward,
        DynamicGroundDownward,
        DynamicGroundRightward,
        DynamicGroundLeftward,
        DynamicSpace
    }
}