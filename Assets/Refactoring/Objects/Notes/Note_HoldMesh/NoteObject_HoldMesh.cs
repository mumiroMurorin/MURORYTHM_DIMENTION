using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace Refactoring
{
    /// <summary>
    /// タッチノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_HoldMesh : NoteObject<NoteData_HoldMesh>
    {
        [Header("meshのマテリアル(未判定時)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("meshのマテリアル(タッチ時)")]
        [SerializeField] Material meshMaterialTouching;
        [Header("meshのマテリアル(非タッチ時)")]
        [SerializeField] Material meshMaterialUntouching;

        NoteData_HoldMesh noteData;
        List<MeshRenderer> meshRenderers;

        List<int> judgeRange = new List<int>();
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMesh data)
        {
            noteData = data;

            // マテリアルの設定
            meshRenderers = new List<MeshRenderer>();
            foreach(Transform child in this.gameObject.transform)
            {
                if (child.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    meshRenderers.Add(meshRenderer);
                    meshRenderer.material = meshMaterialDefault;
                }
            }

            Bind();
        }

        private void Bind()
        {
            if(noteData == null) { return; }


        }

        private void Update()
        {
            if (noteData == null) { return; }
            if (noteData.Timer.Time < noteData.Timing) { return; }

            UpdateJudgeRange();
            UpdateTouchStatus();
        }

        /// <summary>
        /// ノーツの判定範囲を更新する
        /// </summary>
        private void UpdateJudgeRange()
        {
            if (noteData.Timer == null) { return; }

            // 時間外判定
            if (noteData.TimeToRanges[0].Timing > noteData.Timer.Time) { return; }
            if (noteData.TimeToRanges[^1].Timing < noteData.Timer.Time) { return; }

            // 今ホールドノーツのどの時間を判定しているのか調べる
            TimeToRange former = new TimeToRange();
            TimeToRange latter = new TimeToRange();
            for(int i = 0; i < noteData.TimeToRanges.Count; i++)
            {
                if (noteData.TimeToRanges[i].Timing > noteData.Timer.Time) { continue; }
                if (noteData.TimeToRanges[i + 1].Timing < noteData.Timer.Time) { continue; }

                former = noteData.TimeToRanges[i];
                latter = noteData.TimeToRanges[i + 1];
            }

            // 判定範囲の計算
            float t0 = former.Timing;
            float t1 = latter.Timing;
            float x0 = former.Range[0];
            float x1 = latter.Range[0];
            float t = noteData.Timer.Time;

            float startRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[0];

            x0 = former.Range[^1];
            x1 = latter.Range[^1];

            float endRange = x1 - x0 != 0 ?
                (t - t1) * (x1 - x0) / (t1 - t0) + x1 :
                former.Range[^1];

            judgeRange = Enumerable.Range((int)startRange, (int)Mathf.Ceil(endRange) - (int)startRange + 1).ToList();

            //Debug.Log($"Range: {startRange} , {endRange}");
            //Debug.Log("judgeRange: " + string.Join(",", judgeRange.Select(n => n.ToString())));
        }

        /// <summary>
        /// タッチ判定を更新する
        /// </summary>
        private void UpdateTouchStatus()
        {
            if (noteData.Timer == null) { return; }

            // 判定範囲内のスライダー入力を調べる
            foreach(int index in judgeRange)
            {
                if (!noteData.SliderInput.GetSliderInputReactiveProperty(index).Value) { continue; }

                // もしどこかが押されていたら成功
                SetTouchStatus(true);
                return;
            }

            // どこも押されていなかったら失敗
            SetTouchStatus(false);

            return;
        }

        /// <summary>
        /// タッチされているかどうかでマテリアルを変更する
        /// </summary>
        /// <param name="isTouching"></param>
        public void SetTouchStatus(bool isTouching)
        {
            foreach(MeshRenderer meshRenderer in meshRenderers)
            {
                meshRenderer.material = isTouching ? meshMaterialTouching : meshMaterialUntouching;
            }
        }

        /// <summary>
        /// ノーツを機能停止する
        /// </summary>
        private void SetDisable()
        {
             this.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
    /// </summary>
    public class NoteData_HoldMesh : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public List<TimeToRange> TimeToRanges { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }
    }

}
