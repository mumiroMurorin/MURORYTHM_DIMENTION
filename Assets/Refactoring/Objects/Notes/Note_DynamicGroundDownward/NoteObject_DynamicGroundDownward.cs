using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Refactoring
{
    /// <summary>
    /// ダイナミックグラウンドノーツにアタッチされるクラス
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
        /// 初期化
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

            // 右手
            timeToPositionRight.ObserveAdd()
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => isJudged)
                .Subscribe(_ => UpdateMaxDifference(timeToPositionRight))
                .AddTo(this);

            // 左手
            timeToPositionLeft.ObserveAdd()
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => isJudged)
                .Subscribe(_ => UpdateMaxDifference(timeToPositionLeft))
                .AddTo(this);


        }

        /// <summary>
        /// 最大差を算出
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
        /// 判定
        /// </summary>
        private void Judge()
        {
            // 判定を得る
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
        /// ノーツを機能停止する
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
        /// ミス判定
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
    /// (初期化に必要な変数も含む)ダイナミックノーツ(アップ)のデータ
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
