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
            // �m�[�c�̔��背�[�����w��
            for(int i = JudgementData.StartIndex; i < JudgementData.EndIndex; i++)
            {
                // ���̂܂�i��������ƂȂ����Q�Ɠn���ɂȂ�
                int index = i;

                SliderInputGetter?.GetSliderInputReactiveProperty(index)
                    // �^�b�`���ꂽ�Ƃ�����
                    .Where(isTouch => isTouch)
                    // ������̂Ƃ�����
                    .Where(_ => !isJudged)
                    // Good���莞�ԂɊ܂܂�Ă���Ƃ�����
                    .Where(_ => judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime) != Judgement.None)
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
            Judgement judgement = judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime);
            JudgementRecorder?.RecordJudgement(judgement);

            isJudged = true;
        }

        /// <summary>
        /// �m�[�c�������Ȃ�����
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
            if (isJudged) { return false; }
            if (judgementWindow.GetJudgement(Timer.Time, JudgementData.JudgeTime) != Judgement.Miss) { return false; }

            return true;
        }
    }

}
