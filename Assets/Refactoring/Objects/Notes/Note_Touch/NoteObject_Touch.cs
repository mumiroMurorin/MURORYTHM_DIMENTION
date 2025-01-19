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
                Debug.LogWarning("【Note_Touch】NoteDataが割り当てられていません");
                return;
            }

            /*
            foreach (int index in noteData.Range)
            {
                // 判定
                sliderInput?.GetSliderInputReactiveProperty(index)
                    // タッチされたとき且つ
                    .Where(isTouch => isTouch)
                    // 未判定のとき且つ
                    .Where(_ => !isJudged)
                    // Good判定時間に含まれているとき判定
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
        /// 判定
        /// </summary>
        private void Judge()
        {
            // 判定を得る
            /*
            Judgement judgement = judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime);
            JudgementRecorder?.RecordJudgement(judgement);
            */
            isJudged = true;
        }

        /// <summary>
        /// ノーツを機能停止する
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
        /// ミス判定
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
