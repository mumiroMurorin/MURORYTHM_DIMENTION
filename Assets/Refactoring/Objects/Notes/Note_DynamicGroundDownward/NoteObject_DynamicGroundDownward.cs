using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using NoteJudgement;
using System.Linq;

namespace Refactoring
{
    /// <summary>
    /// ダイナミックグラウンドノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_DynamicGroundDownward : NoteObject<NoteData_DynamicGroundDownward>
    {
        Vector3 JudgeVector => Vector3.down;

        [SerializeField] float judgeTimeRange = 1f;
        [SerializeField] JudgementWindow judgementWindow;
        [SerializeField] Vector3 judgeThreshold;

        NoteData_DynamicGroundDownward noteData;
        Judgement currentMaxJudgement = Judgement.Miss;
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_DynamicGroundDownward data)
        {
            noteData = data;

            // 判定閾値の更新
            judgeThreshold = NoteJudgement.DynamicNote.CalcJudgementThresHold(JudgeVector);

            Bind();
        }

        private void Bind()
        {
            if (noteData == null) { return; }
            if (noteData.SpaceInput == null) { return; }

            // 右手
            noteData.SpaceInput?.GetSpaceInputReactiveDictionary(SpaceTrackingTag.RightHand).ObserveAdd()
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => isJudged)
                .Subscribe(_ => Judge(noteData.SpaceInput.GetMaxDifference(judgeTimeRange)))
                .AddTo(this.gameObject);
        }

        private void Update()
        {
            if (JudgeMiss()) 
            {
                RecordJudgement();
                SetDisable();
            }
        }

        /// <summary>
        /// 判定
        /// </summary>
        private void Judge(Vector3 diff)
        {
            // 閾値から出てるか判定
            if (!NoteJudgement.DynamicNote.JudgeThreshold(judgeThreshold, diff)) { return; }

            // 判定を更新
            currentMaxJudgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);

            // Perfectだったときは問答無用でPerfect
            if (currentMaxJudgement == Judgement.Perfect) { RecordJudgement(); }

            // Great以下だったときはMiss判定まで待ち

            return;
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

        /// <summary>
        /// 判定の記録
        /// </summary>
        private void RecordJudgement()
        {
            NoteJudgementData judgementData = new NoteJudgementData
            {
                Judgement = currentMaxJudgement,
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
