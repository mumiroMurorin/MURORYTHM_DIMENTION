using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// �_�C�i�~�b�N�O���E���h�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_DynamicGroundRightward : NoteObject<NoteData_DynamicGroundRightward>
    {
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_DynamicGroundRightward noteData;
        
        bool isJudged;

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_DynamicGroundRightward data)
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
                if(noteData.SpaceInput == null) { break; }
                if(noteData.Timer == null) { break; }

                //noteData.SpaceInput?.GetSpaceInputReactiveProperty(SpaceTrackingTag.RightHand)
                //    // �^�b�`���ꂽ�Ƃ�����
                //    .Where(isTouch => isTouch)
                //    // ������̂Ƃ�����
                //    .Where(_ => !isJudged)
                //    // Good���莞�ԂɊ܂܂�Ă���Ƃ�����
                //    .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                //    .Subscribe(_ => {
                //        Judge();
                //        SetDisable();
                //    })
                //    .AddTo(this.gameObject);
            }
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Judge()
        {
            // ����𓾂�
            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            noteData.JudgementRecorder?.RecordJudgement(judgement);
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
    /// (�������ɕK�v�ȕϐ����܂�)�_�C�i�~�b�N�m�[�c(�A�b�v)�̃f�[�^
    /// </summary>
    public class NoteData_DynamicGroundRightward : INoteData
    {
        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISpaceInputGetter SpaceInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
