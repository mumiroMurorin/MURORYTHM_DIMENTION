using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// タッチノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_Touch : NoteObject<NoteData_Touch>
    {
        [SerializeField] JudgementWindow judgementWindow;
        [SerializeField] JudgementSoundEffects judgementSoundEffects;

        NoteData_Touch noteData;
        
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_Touch data)
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
                if(noteData.SliderInput == null) { break; }
                if(noteData.Timer == null) { break; }

                noteData.SliderInput?.GetSliderInputReactiveProperty(index)
                    // タッチされたとき且つ
                    .Where(isTouch => isTouch)
                    // 未判定のとき且つ
                    .Where(_ => !isJudged)
                    // Good判定時間に含まれているとき判定
                    .Where(_ => judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing) != Judgement.None)
                    .Subscribe(_ => {
                        Judge();
                        SetDisable();
                    })
                    .AddTo(this.gameObject);
            }
        }

        /// <summary>
        /// 判定
        /// </summary>
        private void Judge()
        {
            // 判定を得る
            Judgement judgement = judgementWindow.GetJudgement(noteData.Timer.Time, noteData.Timing);

            float radian = (11.25f * ((noteData.Range[noteData.Range.Length - 1] - noteData.Range[0]) / 2f + 0.5f) - 180f) * Mathf.Deg2Rad;

            NoteJudgementData judgementData = new NoteJudgementData
            {
                Judgement = judgement,
                NoteData = this.noteData,
                TimingError = noteData.Timing - noteData.Timer.Time,
                PositionJudged = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0)
            };

            noteData.JudgementRecorder?.RecordJudgement(judgementData);
            judgementSoundEffects.PlaySE(judgement);
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
    /// (初期化に必要な変数も含む)タッチノーツのデータ
    /// </summary>
    public class NoteData_Touch : INoteData
    {
        public NoteType NoteType => NoteType.Touch;

        public float Timing { get; set; }

        public int[] Range { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public IJudgementRecorder JudgementRecorder { get; set; } 
    }

}
