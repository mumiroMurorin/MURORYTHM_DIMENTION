using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class GroundTouchNoteObject : GroundNoteObject , ISliderJudgable
    {
        ITimeGetter timer;
        IJudgementRecorder judgementRecorder;
        ISliderInputGetter sliderInputGetter;
        INoteSliderJudgementData judgeData;

        // データセット
        public void SetJudgementRecorder(IJudgementRecorder judgementRecorder)
        {
            this.judgementRecorder = judgementRecorder;
        }

        public void SetSliderInputGetter(ISliderInputGetter inputData)
        {
            sliderInputGetter = inputData;
        }

        public void SetJudgementData(INoteSliderJudgementData judgeData)
        {
            this.judgeData = judgeData;
        }

        public void SetTimeCounter(ITimeGetter timer)
        {
            this.timer = timer;
        }

        bool isJudged;

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // ノーツの判定レーン分購読
            for(int i = judgeData.StartIndex; i < judgeData.EndIndex; i++)
            {
                // そのままiを代入するとなぜか参照渡しになる
                int index = i;

                sliderInputGetter?.GetSliderInputReactiveProperty(index)
                    // タッチされたとき且つ
                    .Where(isTouch => isTouch)
                    // 未判定のとき且つ
                    .Where(_ => !isJudged)
                    // Good判定時間に含まれているとき判定
                    .Where(_ => Mathf.Abs(judgeData.JudgeTime - timer.Time) <= judgementWindow.GoodWindow)
                    .Subscribe(_ => { 
                        Judge();
                        SetDisable();
                    })
                    .AddTo(this.gameObject);
            }
        }

        /// <summary>
        /// 判定
        /// </summary>
        private void Judge()
        {
            // 判定を得る
            Judgement judgement = judgementWindow.GetJudgement(Mathf.Abs(judgeData.JudgeTime - timer.Time));
            judgementRecorder?.RecordJudgement(judgement);

            isJudged = true;
        }

        /// <summary>
        /// ノーツを見えなくする
        /// </summary>
        private void SetDisable()
        {
            this.gameObject.SetActive(false);
        }
    }

}
