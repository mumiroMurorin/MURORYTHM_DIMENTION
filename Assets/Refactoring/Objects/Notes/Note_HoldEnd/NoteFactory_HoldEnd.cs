using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;

namespace Refactoring
{
    public class NoteFactory_HoldEnd : NoteFactory<NoteData_HoldEnd>
    {
        [SerializeField] GameObject noteObjectOriginPrefab;

        [Header("マスに応じたノーツタイル")]
        [SerializeField] GameObject singleTilePrefab;
        [SerializeField] GameObject rightEdgeTilePrefab;
        [SerializeField] GameObject centerTilePrefab;
        [SerializeField] GameObject leftEdgeTilePrefab;

        INoteSpawnDataOptionHolder optionHolder;
        ISliderInputGetter sliderInputGetter;
        IJudgementRecorder judgementRecorder;
        ITimeGetter timer;
        GameObject groundObject;
        Deformer groundDeformer;

        public override void Initialize(NoteFactoryInitializingData initializingData)
        {
            this.optionHolder = initializingData.OptionHolder;
            this.groundObject = initializingData.GroundObject;
            this.groundDeformer = initializingData.GroundDeformer;
            this.sliderInputGetter = initializingData.SliderInputGetter;
            this.judgementRecorder = initializingData.JudgementRecorder;
            this.timer = initializingData.Timer;
        }

        public override NoteObject<NoteData_HoldEnd> Spawn(NoteData_HoldEnd data)
        {
            // 生成
            NoteObject<NoteData_HoldEnd> note = GenerateNoteInstance(ConvertNoteData(data));

            // 位置調整
            SetTransform(note, data);

            // 初期化
            note.Initialize(data);

            return note;
        }

        /// <summary>
        /// ノートデータにさらなる情報を追加
        /// </summary>
        /// <param name="data"></param>
        private NoteData_HoldEnd ConvertNoteData(NoteData_HoldEnd data)
        {
            // ノーツデータにいろいろ追加
            data.SliderInput = this.sliderInputGetter;
            data.Timer = this.timer;
            data.JudgementRecorder = this.judgementRecorder;

            return data;
        }

        /// <summary>
        /// ノーツをインスタンス化して返す
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private NoteObject<NoteData_HoldEnd> GenerateNoteInstance(NoteData_HoldEnd data)
        {
            GameObject origin = Instantiate(noteObjectOriginPrefab);

            // ノーツオブジェクトを生成
            GameObject noteObj = GenerateNoteObject(data.Range.Length);

            // originにくっつける
            noteObj.transform.SetParent(origin.transform);

            // 角度(レーン)調整
            noteObj.transform.eulerAngles = new Vector3(0, 0, CalcNoteTransform.NoteAngle(data.Range));

            // コンポーネントを取得
            NoteObject<NoteData_HoldEnd> note = origin.GetComponent<NoteObject<NoteData_HoldEnd>>();

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
                
                //pos = new Vector3(10 * Mathf.Cos((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 10 * Mathf.Sin((((size - 1) / 2f - i) * 11.25f - 90f) * Mathf.Deg2Rad), 0);
                float radian = (5.625f * (2 * i - size + 1) - 90f) * Mathf.Deg2Rad;
                pos = new Vector3(10 * Mathf.Cos(radian), 10 * Mathf.Sin(radian), 0);
                rot = new Vector3(0, 0, ((size - 1) / 2f - (size - i - 1)) * 11.25f);

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
        private void SetTransform(NoteObject<NoteData_HoldEnd> note, NoteData_HoldEnd data)
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
