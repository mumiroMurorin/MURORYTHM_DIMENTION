using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �^�b�`�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_HoldRelay : NoteObject<NoteData_HoldRelay>
    {
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_HoldRelay noteData;

        bool isJudged;
        Judgement bestJudgement = Judgement.Miss;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldRelay data)
        {
            noteData = data;
        }

        private void Update()
        {
            // ���莞�ԓ����X���C�_�[��������Ă���Ƃ�
            if (IsInJudgementRange() && IsTouchingSlider())
            {
                // �L�^���������肢�����肾�����Ƃ�����̍X�V
                Judgement currentJudgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
                if ((int)bestJudgement < (int)currentJudgement)
                {
                    bestJudgement = currentJudgement;
                }

                // �ō�����̂Ƃ��m��
                if (bestJudgement == Judgement.Perfect)
                {
                    SendJudgementData();
                }
            }
            // ���莞�Ԃ��߂����Ƃ�
            else if (IsPassJudgementRange())
            {
                SendJudgementData();
                SetDisable();
            }
        }

        /// <summary>
        /// ����f�[�^�𑗐M
        /// </summary>
        private void SendJudgementData()
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

        /// <summary>
        /// ����͈͓������ׂ�
        /// </summary>
        /// <returns></returns>
        private bool IsInJudgementRange()
        {
            if (noteData == null) { return false; }
            if (noteData.Timer == null) { return false; }
            if (isJudged) { return false; }

            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if (judgement == Judgement.Miss || judgement == Judgement.None) { return false; }

            return true;
        }

        /// <summary>
        /// �m�[�c�͈͓��̃X���C�_�[���^�b�`����Ă��邩����
        /// </summary>
        /// <returns></returns>
        private bool IsTouchingSlider()
        {
            if (noteData.SliderInput == null) { return false; }
            if (noteData.Timer == null) { return false; }

            foreach (int index in noteData.Range)
            {
                if (noteData.SliderInput.GetSliderInputReactiveProperty(index).Value) { return true; }
            }

            return false;
        }

        /// <summary>
        /// �m�[�c����͈͊O�H
        /// </summary>
        /// <returns></returns>
        private bool IsPassJudgementRange()
        {
            if (noteData == null) { return false; }
            if (noteData.Timer == null) { return false; }
            if (judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.Miss) { return false; }
            if (isJudged) { return false; }

            return true;
        }
    }

    /// <summary>
    /// (�������ɕK�v�ȕϐ����܂�)�z�[���h���_�m�[�c�̃f�[�^
    /// </summary>
    public class NoteData_HoldRelay : INoteData
    {
        public NoteType NoteType => NoteType.HoldRelay;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
