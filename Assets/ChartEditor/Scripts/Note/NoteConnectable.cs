using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteConnectable : MonoBehaviour, IConnectableObject
    {
        NoteObject noteObject;
        NoteObject IConnectableObject.Note => noteObject;

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
            //noteObject.NoteData.Address.BarIndexRP
            //    .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
            //    .AddTo(this.gameObject);

            //noteObject.NoteData.Address.SubDivisionIndexRP
            //    .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
            //    .AddTo(this.gameObject);

            //noteObject.NoteData.Address.SliderIndexRP
            //    .Subscribe(_ => OnChangeAddress(noteObject.NoteData.Address))
            //    .AddTo(this.gameObject);
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