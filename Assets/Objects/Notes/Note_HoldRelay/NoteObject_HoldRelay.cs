using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// タッチノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_HoldRelay : NoteObject<NoteData_HoldRelay>
    {
        [SerializeField] JudgementWindow judgementWindow;

        NoteData_HoldRelay noteData;

        bool isJudged;
        Judgement bestJudgement = Judgement.Miss;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldRelay data)
        {
            noteData = data;
        }

        private void Update()
        {
            // 判定時間内かつスライダーが押されているとき
            if (IsInJudgementRange() && IsTouchingSlider())
            {
                // 記録した判定よりいい判定だったとき判定の更新
                Judgement currentJudgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
                if ((int)bestJudgement < (int)currentJudgement)
                {
                    bestJudgement = currentJudgement;
                }

                // 最高判定のとき確定
                if (bestJudgement == Judgement.Perfect)
                {
                    SendJudgementData();
                }
            }
            // 判定時間を過ぎたとき
            else if (IsPassJudgementRange())
            {
                SendJudgementData();
                SetDisable();
            }
        }

        /// <summary>
        /// 判定データを送信
        /// </summary>
        private void SendJudgementData()
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

        /// <summary>
        /// 判定範囲内か調べる
        /// </summary>
        /// <returns></returns>
        private bool IsInJudgementRange()
        {
            if (noteData == null) { return false; }
            if (noteData.Timer == null) { return false; }
            if (isJudged) { return false; }

            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);
            if (judgement == Judgement.Miss || judgement == Judgement.None) { return false; }

            return true;
        }

        /// <summary>
        /// ノーツ範囲内のスライダーがタッチされているか判定
        /// </summary>
        /// <returns></returns>
        private bool IsTouchingSlider()
        {
            if (noteData.SliderInput == null) { return false; }
            if (noteData.Timer == null) { return false; }

            foreach (int index in noteData.Range)
            {
                if (noteData.SliderInput.GetSliderInputReactiveProperty(index).Value) { return true; }
            }

            return false;
        }

        /// <summary>
        /// ノーツ判定範囲外？
        /// </summary>
        /// <returns></returns>
        private bool IsPassJudgementRange()
        {
            if (noteData == null) { return false; }
            if (noteData.Timer == null) { return false; }
            if (judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.Miss) { return false; }
            if (isJudged) { return false; }

            return true;
        }
    }

    /// <summary>
    /// (初期化に必要な変数も含む)ホールド中点ノーツのデータ
    /// </summary>
    public class NoteData_HoldRelay : INoteData
    {
        public NoteType NoteType => NoteType.HoldRelay;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
