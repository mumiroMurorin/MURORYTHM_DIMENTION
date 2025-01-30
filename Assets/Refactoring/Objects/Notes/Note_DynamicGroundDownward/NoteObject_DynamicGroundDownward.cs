using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Refactoring
{
    /// <summary>
    /// �_�C�i�~�b�N�O���E���h�m�[�c�ɃA�^�b�`�����N���X
    /// </summary>
    public class NoteObject_DynamicGroundDownward : NoteObject<NoteData_DynamicGroundDownward>
    {
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_DynamicGroundDownward noteData;
        
        bool isJudged;
        IReadOnlyReactiveDictionary<float, Vector3> timeToPositionRight;
        IReadOnlyReactiveDictionary<float, Vector3> timeToPositionLeft;
        Vector2ReactiveProperty maxDifference = new Vector2ReactiveProperty(Vector2.zero);

        /// <summary>
        /// ������
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_DynamicGroundDownward data)
        {
            noteData = data;

            Bind();
        }

        private void Bind()
        {
            if (noteData == null) { return; }
            if (noteData.SpaceInput == null) { return; }

            timeToPositionRight = noteData.SpaceInput?.GetSpaceInputReactiveDictionary(SpaceTrackingTag.RightHand);
            timeToPositionLeft = noteData.SpaceInput?.GetSpaceInputReactiveDictionary(SpaceTrackingTag.LeftHand);

            // �E��
            timeToPositionRight.ObserveAdd()
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => isJudged)
                .Subscribe(_ => UpdateMaxDifference(timeToPositionRight))
                .AddTo(this);

            // ����
            timeToPositionLeft.ObserveAdd()
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => isJudged)
                .Subscribe(_ => UpdateMaxDifference(timeToPositionLeft))
                .AddTo(this);


        }

        /// <summary>
        /// �ő卷���Z�o
        /// </summary>
        /// <param name="timeToPosition"></param>
        private void UpdateMaxDifference(IReadOnlyReactiveDictionary<float, Vector3> timeToPosition)
        {
            if (timeToPosition == null || timeToPosition.Count < 2)
            {
                maxDifference.Value = Vector2.zero;
                return;
            }

            var xValues = timeToPosition.Where(v => v.Key > ).Select(v => v.Value.x).ToList();
            var yValues = timeToPosition.Select(v => v.Value.y).ToList();
            float maxDiffX = xValues.Max() - xValues.Min();
            float maxDiffY = yValues.Max() - yValues.Min();

            maxDifference.Value = new Vector2(maxDiffX, maxDiffY);
        }

        /// <summary>
        /// ����
        /// </summary>
        private void Judge()
        {
            // ����𓾂�
            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);

            NoteJudgementData judgementData = new NoteJudgementData
            {
                Judgement = judgement,
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
    public class NoteData_DynamicGroundDownward : INoteData
    {
        public NoteType NoteType => NoteType.DynamicGroundDownward;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISpaceInputGetter SpaceInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
