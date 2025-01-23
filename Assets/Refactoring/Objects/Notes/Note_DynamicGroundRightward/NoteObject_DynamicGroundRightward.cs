using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// ダイナミックグラウンドノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_DynamicGroundRightward : NoteObject<NoteData_DynamicGroundRightward>
    {
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_DynamicGroundRightward noteData;
        
        bool isJudged;

        /// <summary>
        /// 初期化
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

            // 成功判定
            foreach (int index in noteData.Range)
            {
                if(noteData.SpaceInput == null) { break; }
                if(noteData.Timer == null) { break; }

                //noteData.SpaceInput?.GetSpaceInputReactiveProperty(SpaceTrackingTag.RightHand)
                //    // タッチされたとき且つ
                //    .Where(isTouch => isTouch)
                //    // 未判定のとき且つ
                //    .Where(_ => !isJudged)
                //    // Good判定時間に含まれているとき判定
                //    .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                //    .Subscribe(_ => {
                //        Judge();
                //        SetDisable();
                //    })
                //    .AddTo(this.gameObject);
            }
        }

        /// <summary>
        /// 判定
        /// </summary>
        private void Judge()
        {
            // 判定を得る
            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            noteData.JudgementRecorder?.RecordJudgement(judgement);
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
    public class NoteData_DynamicGroundRightward : INoteData
    {
        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISpaceInputGetter SpaceInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
