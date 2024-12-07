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

        // �f�[�^�Z�b�g
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
            // �m�[�c�̔��背�[�����w��
            for(int i = judgeData.StartIndex; i < judgeData.EndIndex; i++)
            {
                // ���̂܂�i��������ƂȂ����Q�Ɠn���ɂȂ�
                int index = i;

                sliderInputGetter?.GetSliderInputReactiveProperty(index)
                    // �^�b�`���ꂽ�Ƃ�����
                    .Where(isTouch => isTouch)
                    // ������̂Ƃ�����
                    .Where(_ => !isJudged)
                    // Good���莞�ԂɊ܂܂�Ă���Ƃ�����
                    .Where(_ => Mathf.Abs(judgeData.JudgeTime - timer.Time) <= judgementWindow.GoodWindow)
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
            Judgement judgement = judgementWindow.GetJudgement(Mathf.Abs(judgeData.JudgeTime - timer.Time));
            judgementRecorder?.RecordJudgement(judgement);

            isJudged = true;
        }

        /// <summary>
        /// �m�[�c�������Ȃ�����
        /// </summary>
        private void SetDisable()
        {
            this.gameObject.SetActive(false);
        }
    }

}
