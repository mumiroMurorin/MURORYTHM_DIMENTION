using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �^�b�`�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_Touch : NoteObject<NoteData_Touch>
    {
        [SerializeField] JudgementWindow judgementWindow;
        [SerializeField] JudgementSoundEffects judgementSoundEffects;

        NoteData_Touch noteData;
        
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_Touch data)
        {
            noteData = data;

            Bind();
        }

        private void Bind()
        {
            if(noteData == null) { return; }

            // ��������
            foreach (int index in noteData.Range)
            {
                if(noteData.SliderInput == null) { break; }
                if(noteData.Timer == null) { break; }

                noteData.SliderInput?.GetSliderInputReactiveProperty(index)
                    // �^�b�`���ꂽ�Ƃ�����
                    .Where(isTouch => isTouch)
                    // ������̂Ƃ�����
                    .Where(_ => !isJudged)
                    // Good���莞�ԂɊ܂܂�Ă���Ƃ�����
                    .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                    .Subscribe(_ => {
                        Judge();
                        SetDisable();
                    })
                    .AddTo(this.gameObject);
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Judge()
        {
            // ����𓾂�
            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);

            float radian = (11.25f * ((noteData.Range[noteData.Range.Length - 1] - noteData.Range[0]) / 2f + 0.5f) - 180f) * Mathf.Deg2Rad;

            NoteJudgementData judgementData = new NoteJudgementData
            {
                Judgement = judgement,
                NoteData = this.noteData,
                TimingError = noteData.Timing - noteData.Timer.Time,
                PositionJudged = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0)
            };

            noteData.JudgementRecorder?.RecordJudgement(judgementData);
            judgementSoundEffects.PlaySE(judgement);
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

        private void Update()
        {
            if (JudgeMiss())
            {
                Judge();
                SetDisable();
            }

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
    }

    /// <summary>
    /// (�������ɕK�v�ȕϐ����܂�)�^�b�`�m�[�c�̃f�[�^
    /// </summary>
    public class NoteData_Touch : INoteData
    {
        public NoteType NoteType => NoteType.Touch;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
