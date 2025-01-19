using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    /// <summary>
    /// �X���C�_�[�̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISliderJudgable
    {
        public void SetSliderInput(ISliderInputGetter input);
    }

    /// <summary>
    /// ��Ԃ̓��͏󋵂Ɉˑ�����������s��
    /// </summary>
    public interface ISpaceJudgable
    {
        public void SetSpaceInput(ISpaceInputGetter input);
    }

    /// <summary>
    /// �e��m�[�c�f�[�^�̊�ƂȂ�C���^�[�t�F�[�X
    /// </summary>
    public interface INoteData
    {
        //public float Timing { get; set; }

        //public ITimeGetter Timer { get; set; }

    }

    /// <summary>
    /// Perfect�`Good�܂ł̔��苖�e�͈͂��܂Ƃ߂��N���X
    /// </summary>
    [System.Serializable]
    public class JudgementWindow
    {
        [SerializeField] float perfectWindow;
        [SerializeField] float greatWindow;
        [SerializeField] float goodWindow;

        public float PerfectWindow { get { return perfectWindow; } }
        public float GreatWindow { get { return greatWindow; } }
        public float GoodWindow { get { return goodWindow; } }

        public Judgement GetJudgement(float currentTime, float judgeTime)
        {
            // Good����O
            if (judgeTime - goodWindow > currentTime) { return Judgement.None; }
            // Good�����
            if (judgeTime + goodWindow < currentTime) { return Judgement.Miss; }

            float timingDiff = Mathf.Abs(judgeTime - currentTime);

            if (timingDiff <= perfectWindow) { return Judgement.Perfect; }
            else if (timingDiff <= greatWindow) { return Judgement.Great; }
            else if (timingDiff <= goodWindow) { return Judgement.Good; }

            return Judgement.None;
        }
    }

    public enum Judgement
    {
        None,
        Perfect,
        Great,
        Good,
        Miss
    }
}
