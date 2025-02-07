using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �_�C�i�~�b�N�O���E���h�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_DynamicGroundLeftward : NoteObject<NoteData_DynamicGroundLeftward>
    {
        Vector3 JudgeVector => Vector3.left;

        [SerializeField] float judgeMagnitude;
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_DynamicGroundLeftward noteData;
        DynamicJudgement dynamicJudgement;
        Judgement bestJudgement = Judgement.Miss;
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_DynamicGroundLeftward data)
        {
            noteData = data;

            dynamicJudgement = new DynamicJudgement(noteData.Range, JudgeVector, judgeMagnitude);

            Bind();
        }

        private void Bind()
        {
            if (noteData == null) { return; }
            if (noteData.SpaceInput == null) { return; }

            // �E��
            noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.RightHand)
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !isJudged)
                .Subscribe(Judge)
                .AddTo(this.gameObject);

            // ����
            noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand)
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !isJudged)
                .Subscribe(Judge)
                .AddTo(this.gameObject);
        }

        private void Update()
        {
            if (JudgeMiss())
            {
                RecordJudgement();
                SetDisable();
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Judge(Vector3 velocity)
        {
            //Debug.Log($"�yJudge�zDownward velocity:{velocity}, {dynamicJudgement.Judge(velocity)}, {this.gameObject.name}");

            // 臒l����o�Ă邩����
            if (!dynamicJudgement.Judge(velocity)) { return; }

            // ������X�V
            Judgement currentJudgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if ((int)bestJudgement < (int)currentJudgement)
            {
                bestJudgement = currentJudgement;
            }

            // Perfect�������Ƃ��͖ⓚ���p��Perfect
            if (bestJudgement == Judgement.Perfect) { RecordJudgement(); }

            // Great�ȉ��������Ƃ���Miss����܂ő҂�

            return;
        }

        /// <summary>
        /// �~�X����
        /// </summary>
        /// <returns></returns>
        private bool JudgeMiss()
        {
            if (noteData == null) { return false; }
            if (noteData.Timer == null) { return false; }
            if (judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.Miss) { return false; }
            if (isJudged) { return false; }

            return true;
        }

        /// <summary>
        /// ����̋L�^
        /// </summary>
        private void RecordJudgement()
        {
            NoteJudgementData judgementData = new NoteJudgementData
            {
                Judgement = bestJudgement,
                NoteData = this.noteData,
                TimingError = noteData.Timing - noteData.Timer.Time
            };

            noteData.JudgementRecorder?.RecordJudgement(judgementData);
            isJudged = true;
        }

        /// <summary>
        /// �m�[�c���@�\��~����
        /// </summary>
        private void SetDisable()
        {
            this.gameObject.SetActive(false);
            // Destroy(this.gameObject);
        }

    }

    /// <summary>
    /// (�������ɕK�v�ȕϐ����܂�)�_�C�i�~�b�N�m�[�c(��)�̃f�[�^
    /// </summary>
    public class NoteData_DynamicGroundLeftward : INoteData
    {
        public NoteType NoteType => NoteType.DynamicGroundLeftward;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISpaceInputGetter SpaceInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
