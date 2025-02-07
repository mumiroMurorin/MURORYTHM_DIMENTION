using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// ダイナミックグラウンドノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_DynamicGroundLeftward : NoteObject<NoteData_DynamicGroundLeftward>
    {
        Vector3 JudgeVector => Vector3.left;

        [SerializeField] float judgeMagnitude;
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_DynamicGroundLeftward noteData;
        DynamicJudgement dynamicJudgement;
        Judgement bestJudgement = Judgement.Miss;
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_DynamicGroundLeftward data)
        {
            noteData = data;

            dynamicJudgement = new DynamicJudgement(noteData.Range, JudgeVector, judgeMagnitude);

            Bind();
        }

        private void Bind()
        {
            if (noteData == null) { return; }
            if (noteData.SpaceInput == null) { return; }

            // 右手
            noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.RightHand)
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !isJudged)
                .Subscribe(Judge)
                .AddTo(this.gameObject);

            // 左手
            noteData.SpaceInput?.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand)
                .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                .Where(_ => !isJudged)
                .Subscribe(Judge)
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
        private void Judge(Vector3 velocity)
        {
            //Debug.Log($"【Judge】Downward velocity:{velocity}, {dynamicJudgement.Judge(velocity)}, {this.gameObject.name}");

            // 閾値から出てるか判定
            if (!dynamicJudgement.Judge(velocity)) { return; }

            // 判定を更新
            Judgement currentJudgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if ((int)bestJudgement < (int)currentJudgement)
            {
                bestJudgement = currentJudgement;
            }

            // Perfectだったときは問答無用でPerfect
            if (bestJudgement == Judgement.Perfect) { RecordJudgement(); }

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
                Judgement = bestJudgement,
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
    /// (初期化に必要な変数も含む)ダイナミックノーツ(←)のデータ
    /// </summary>
    public class NoteData_DynamicGroundLeftward : INoteData
    {
        public NoteType NoteType => NoteType.DynamicGroundLeftward;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISpaceInputGetter SpaceInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
