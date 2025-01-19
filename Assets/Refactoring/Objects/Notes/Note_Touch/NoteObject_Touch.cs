using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class NoteObject_Touch : NoteObject<NoteData_Touch> , ISliderJudgable
    {
        [SerializeField] JudgementWindow judgementWindow;

        ISliderInputGetter sliderInput;
        NoteData_Touch noteData;

        bool isJudged;

        public override void Initialize(NoteData_Touch noteData)
        {
            this.noteData = noteData;
        }

        public void SetSliderInput(ISliderInputGetter input)
        {
            sliderInput = input;

            // Bind
            JudgeBind();
        }

        private void JudgeBind()
        {
            if (noteData == null)
            {
                Debug.LogWarning("�yNote_Touch�zNoteData�����蓖�Ă��Ă��܂���");
                return;
            }

            /*
            foreach (int index in noteData.Range)
            {
                // ����
                sliderInput?.GetSliderInputReactiveProperty(index)
                    // �^�b�`���ꂽ�Ƃ�����
                    .Where(isTouch => isTouch)
                    // ������̂Ƃ�����
                    .Where(_ => !isJudged)
                    // Good���莞�ԂɊ܂܂�Ă���Ƃ�����
                    .Where(_ => judgementWindow.GetJudgement(Timer.Time, noteData.Timing) != Judgement.None)
                    .Subscribe(_ => {
                        Judge();
                        SetDisable();
                    })
                    .AddTo(this.gameObject);
            }
            */
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Judge()
        {
            // ����𓾂�
            /*
            Judgement judgement = judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime);
            JudgementRecorder?.RecordJudgement(judgement);
            */
            isJudged = true;
        }

        /// <summary>
        /// �m�[�c���@�\��~����
        /// </summary>
        private void SetDisable()
        {
            this.gameObject.SetActive(false);
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
            /*
            if (isJudged) { return false; }
            if (judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime) != Judgement.Miss) { return false; }

            */
            return true;
        }
    }

    public class NoteData_Touch : INoteData
    {
        public float Timing { get; set; }

        public int[] Range { get; set; }

        //public ITimeGetter Timer { get; set; }

        //public IJudgementHolder judgementHolder { get; set; } 
    }

}
