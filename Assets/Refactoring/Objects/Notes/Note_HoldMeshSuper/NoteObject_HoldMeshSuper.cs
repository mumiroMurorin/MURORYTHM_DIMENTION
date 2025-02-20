using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    /// <summary>
    /// タッチノーツにアタッチされるクラス
    /// </summary>
    public class NoteObject_HoldMeshSuper : NoteObject<NoteData_HoldMeshSuper>
    {
        [Header("meshのマテリアル(未判定時)")]
        [SerializeField] Material meshMaterialDefault;
        [Header("meshのマテリアル(タッチ時)")]
        [SerializeField] Material meshMaterialTouching;
        [Header("meshのマテリアル(非タッチ時)")]
        [SerializeField] Material meshMaterialUntouching;

        NoteData_HoldMeshSuper noteData;
        List<MeshRenderer> meshRenderers;
        bool isJudged;

        /// <summary>
        /// 初期化
        /// </summary>
        /// <param name="data"></param>
        public override void Initialize(NoteData_HoldMeshSuper data)
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

            // 次のホールドノーツのステータスも変更
            noteData.noteNext?.SetTouchStatus(isTouching);
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
    public class NoteData_HoldMeshSuper : INoteData
    {
        public NoteType NoteType => NoteType.HoldMesh;

        public float Timing { get; set; }

        public List<TimeToRange> TimeToRanges { get; set; }

        public ISliderInputGetter SliderInput { get; set; }

        public ITimeGetter Timer { get; set; }

        public NoteObject_HoldMesh noteNext { get; set; }
    }

}
