using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// タッチノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_HoldMesh : NoteObject<NoteData_HoldMesh>
    {
        NoteData_HoldMesh noteData;
        
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMesh data)
        {
            noteData = data;

            Bind();
        }

        private void Bind()
        {
            if(noteData == null) { return; }


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

        }
    }

    /// <summary>
    /// (初期化に必要な変数も含む)ホールドメッシュノーツのデータ
    /// </summary>
    public class NoteData_HoldMesh : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public float EndTiming { get; set; }

        public int[] StartRange { get; set; }

        public int[] EndRange { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }
    }

}
