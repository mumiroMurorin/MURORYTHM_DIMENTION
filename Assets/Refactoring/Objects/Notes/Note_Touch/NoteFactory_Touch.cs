using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    public class NoteFactory_Touch : NoteFactory<NoteData_Touch>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("マスに応じたノーツタイル")]
        [SerializeField] GameObject singleTilePrefab;
        [SerializeField] GameObject rightEdgeTilePrefab;
        [SerializeField] GameObject centerTilePrefab;
        [SerializeField] GameObject leftEdgeTilePrefab;

        Deformer groundDeformer;
        INoteSpawnDataOptionHolder optionHolder;
        GameObject groundObject;

        public override void Initialize(NoteFactoryInitializingData initializingData)
        {
            this.optionHolder = initializingData.optionHolder;
            this.groundObject = initializingData.groundObject;
            this.groundDeformer = initializingData.groundDeformer;
        }

        public override NoteObject<NoteData_Touch> Spawn(NoteData_Touch data)
        {
            // 生成
            NoteObject<NoteData_Touch> note = GenerateNoteInstance(data);

            // 位置調整
            SetTransform(note, data);

            // 初期化
            note.Initialize(data);

            return note;
        }

        /// <summary>
        /// ノーツをインスタンス化して返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public NoteObject<NoteData_Touch> GenerateNoteInstance(NoteData_Touch data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクトを生成してoriginにくっつける
            GenerateNoteObject(data.Range.Length).transform.SetParent(origin.transform);

            // 角度(レーン)調整
            origin.transform.eulerAngles = new Vector3(0, 0, CalcNoteTransform.NoteAngle(data.Range));

            // コンポーネントを取得
            NoteObject<NoteData_Touch> note = origin.GetComponent<NoteObject<NoteData_Touch>>();

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
            GameObject pre = new GameObject("NoteObjects");   //まとめ役のオブジェクト生成

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
                d.AddDeformer(groundDeformer);
            }

            return pre;
        }

        /// <summary>
        /// 位置調整など
        /// </summary>
        private void SetTransform(NoteObject<NoteData_Touch> note, NoteData_Touch data)
        {
            // 位置の調整
            note.transform.position = new Vector3(
                note.transform.position.x,
                note.transform.position.y,
                optionHolder.NoteSpeed * data.Timing
                );

            // 動く地面を親登録
            note.transform.SetParent(groundObject.transform);
        }
    }

}
