using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteScalable : MonoBehaviour, IScalableObject
    {
        [Tooltip("配置時の元となる GameObject")]
        [SerializeField] GameObject origin;
        [Tooltip("選択時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnScaling;

        NoteObject noteObject;
        CancellationTokenSource cts = new CancellationTokenSource();

        public NoteObject Note => noteObject;

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが存在するまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // ObserveCountChanged()は初期化してくれないので、最初に大きさを変える
            OnChangeScale(noteObject.NoteData.Range.Count);

            // 大きさの変更通知に対してスケール更新
            noteObject.NoteData.Range.ObserveCountChanged()
                .Subscribe(OnChangeScale)
                .AddTo(this.gameObject);
        }

        void IScalableObject.OnStartScale()
        {
            noteObject.OutlineColors.Add(outlineColorOnScaling);
            noteObject.SetCollidersActive(false);
        }

        void IScalableObject.OnScale()
        {
            
        }

        void IScalableObject.OnFinishScale()
        {
            noteObject.OutlineColors.Remove(outlineColorOnScaling);
            noteObject.SetCollidersActive(true);
        }

        public void OnChangeScale(int size)
        {
            // 大きさの更新
            Transform tr = origin.transform;
            tr.localScale = new Vector3(size, tr.localScale.y, tr.localScale.z);
            tr.localPosition = new Vector3((size - 1) / 2f, tr.localPosition.y, tr.localPosition.z);
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