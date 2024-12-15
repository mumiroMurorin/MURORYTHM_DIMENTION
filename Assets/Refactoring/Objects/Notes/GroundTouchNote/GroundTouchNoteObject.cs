using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class GroundTouchNoteObject : TouchNoteObject
    {
        bool isJudged;

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // ノーツの判定レーン分購読
            for(int i = JudgementData.StartIndex; i < JudgementData.EndIndex; i++)
            {
                // そのままiを代入するとなぜか参照渡しになる
                int index = i;

                SliderInputGetter?.GetSliderInputReactiveProperty(index)
                    // タッチされたとき且つ
                    .Where(isTouch => isTouch)
                    // 未判定のとき且つ
                    .Where(_ => !isJudged)
                    // Good判定時間に含まれているとき判定
                    .Where(_ => judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime) != Judgement.None)
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
            Judgement judgement = judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime);
            JudgementRecorder?.RecordJudgement(judgement);

            isJudged = true;
        }

        /// <summary>
        /// ノーツを見えなくする
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
            if (isJudged) { return false; }
            if (judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime) != Judgement.Miss) { return false; }

            return true;
        }
    }

}
