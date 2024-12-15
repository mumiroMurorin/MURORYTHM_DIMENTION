using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    public class GroundTouchNoteGenerator : MonoBehaviour, ITouchNoteGenerator
    {
        [SerializeField] GameObject noteObjectOriginPrefab;
        [SerializeField] Deformer groundBendDeformer;

        [Header("マスに応じたノーツタイル")]
        [SerializeField] GameObject singleTilePrefab;
        [SerializeField] GameObject rightEdgeTilePrefab;
        [SerializeField] GameObject centerTilePrefab;
        [SerializeField] GameObject leftEdgeTilePrefab;

        public NoteObject GenerateNote(IGroundNoteGenerationData generationData, SliderJudgableDataSet judgableDataSet)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクトを生成してoriginにくっつける
            GenerateNoteObject(generationData.NoteLaneWidth).transform.SetParent(origin.transform);

            // 角度(レーン)調整
            origin.transform.eulerAngles = generationData.NoteEulerAngles;

            // コンポーネントを取得
            GroundTouchNoteObject note = origin.GetComponent<GroundTouchNoteObject>();

            // 判定用のデータを渡す
            note.SetSliderJudgableDatas(judgableDataSet);

            return note;
        }

        /// <summary>
        /// ノーツタイルを組み合わせてノーツを生成
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        private GameObject GenerateNoteObject(int size) 
        {
            Vector3 pos, rot;
            GameObject pre = new GameObject("NoteObject");   //まとめ役のオブジェクト生成

            // 1マスずつ生成
            for (int i = 0; i < size; i++)
            {
                // ※ポジションと角度の計算
                pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
                rot = new Vector3(0, 0, ((size - 1) / 2f - i) * 11.25f);

                // 1マスノートの時
                if (size == 1) { Instantiate(singleTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // ノート左端の時
                else if (i == 0) { Instantiate(leftEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // ノート右端の時
                else if (i == size - 1) { Instantiate(rightEdgeTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
                // ノート中の時
                else { Instantiate(centerTilePrefab, pos, Quaternion.Euler(rot), pre.transform); }
            }

            // Deformの設定
            foreach (Transform t in pre.transform)
            {
                Deformable d = t.GetComponentInChildren<Deformable>();
                d.AddDeformer(groundBendDeformer);
            }

            return pre;
        }

    }

}
