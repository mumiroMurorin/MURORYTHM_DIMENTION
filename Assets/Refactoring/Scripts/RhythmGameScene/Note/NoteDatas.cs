using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NoteJudgement;
using System.Linq;
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
    /// ����ɉ�����SE�̍Đ���Z�߂��N���X
    /// </summary>
    [System.Serializable]
    public class JudgementSoundEffects
    {
        [System.Serializable]
        public class JudgementToSE
        {
            [SerializeField] Judgement Judgement;
            [SerializeField] AudioClip audioClip;

            /// <summary>
            /// �����ƏƂ炵���킹�ATrue�ł����SE�̍Đ����s��
            /// </summary>
            /// <param name="judgement"></param>
            public bool CheckConditionAndPlaySE(Judgement judgement)
            {
                if (this.Judgement != judgement) { return false; }
                SoundManager.Instance.PlaySE(audioClip);
                return true;
            }
        }

        [SerializeField] List<JudgementToSE> sounds;

        public void PlaySE(Judgement judgement)
        {
            foreach(JudgementToSE judgementToSE in sounds)
            {
                if (judgementToSE.CheckConditionAndPlaySE(judgement)) { return; }
            }
        }
    }

    public class DynamicJudgement
    {
        List<Vector3> judgeVectors;

        public DynamicJudgement(int[] range, Vector3 rotationVector, float magnitude)
        {
            judgeVectors = new List<Vector3>();

            // ���ꂼ��̔���x�N�g���𒲂ׂ�
            for (int i = 0; i < range.Length; i++)
            {
                Vector3 vector = NoteJudgement.DynamicNote.CalcJudgementThresHold(10, range[i], rotationVector);

                // �e�v�f��0�łȂ������ׂ�magnitude����
                if (vector.x > 0) { vector.x = magnitude; }
                else if (vector.x < 0) { vector.x = -magnitude; }

                if (vector.y > 0) { vector.y = magnitude; }
                else if (vector.y < 0) { vector.y = -magnitude; }

                if (vector.z > 0) { vector.z = magnitude; }
                else if (vector.z < 0) { vector.z = -magnitude; }

                judgeVectors.Add(vector);
            }

            judgeVectors = judgeVectors.Distinct().ToList();
            Debug.Log($"List: {string.Join(", ", judgeVectors)}");
        }

        public bool Judge(Vector3 diff)
        {
            foreach(Vector3 threshold in judgeVectors)
            {
                if (NoteJudgement.DynamicNote.JudgeThreshold(threshold, diff)) { return true; }
            }

            return false;
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
        Perfect = 1000,
        Great = 100,
        Good = 10,
        Miss = 1,
        None = 0,
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