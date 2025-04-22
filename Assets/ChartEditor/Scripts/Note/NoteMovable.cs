using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteMovable : MonoBehaviour, IMovableObject
    {
        [Tooltip("移動時のアウトライン色")]
        [SerializeField] private Color outlineColorOnMove;
        [Tooltip("移動時浮く高さ")]
        [SerializeField] float addHeightOnMove = 1f;

        NoteObject noteObject;
        NoteObject IMovableObject.Note => noteObject;
        
        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData.Address != null, cancellationToken: token);

            // アドレスの変更通知に対して場所の更新
            noteObject.NoteData.Address.BarIndexRP
                .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
                .AddTo(this.gameObject);

            noteObject.NoteData.Address.SubDivisionIndexRP
                .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
                .AddTo(this.gameObject);

            noteObject.NoteData.Address.SliderIndexRP
                .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
                .AddTo(this.gameObject);
        }

        void IMovableObject.OnMoveStart()
        {
            noteObject.SetOutlineColor(outlineColorOnMove, true);
            noteObject.SetOutlineActive(true);
            noteObject.SetCollidersActive(false);

            this.transform.position += Vector3.up * addHeightOnMove;
        }

        void IMovableObject.OnMove()
        {

        }

        void IMovableObject.OnMoveEnd()
        {
            noteObject.SetOutlineActive(false);
            noteObject.SetCollidersActive(true);
            this.transform.position -= Vector3.up * addHeightOnMove;
        }

        /// <summary>
        /// アドレスが更新された時の動作
        /// </summary>
        /// <param name="address"></param>
        private void OnChangeAddress(AddressInChart address)
        {
            Transform parent = noteObject.GetParentTransformFunc(address);

            Vector3 pos = new Vector3(parent.position.x, this.transform.position.y, parent.position.z);
            this.transform.position = pos;
            this.transform.SetParent(parent);
        }

        private void OnDestroy()
        {
            if (cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }
    }

}
