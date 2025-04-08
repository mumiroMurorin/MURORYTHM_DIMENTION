using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteScalable : MonoBehaviour, IScalableObject
    {
        [Tooltip("配置時の元となる GameObject")]
        [SerializeField] GameObject origin;

        NoteObject noteObject;
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

            // 大きさの変更通知に対してスケール更新
            noteObject.NoteData.Range.ObserveCountChanged()
                .Subscribe(OnChangeScale)
                .AddTo(this.gameObject);
        }

        void IScalableObject.OnStartScale()
        {
            noteObject.SetCollidersActive(false);
        }

        void IScalableObject.OnScale(IDeployableCollider deployableCollider)
        {
            AddressInChart address = deployableCollider.Address;
            
            if(address.SliderIndex < noteObject.NoteData.Address.SliderIndex) 
            {
                Transform parent = deployableCollider.deployParent;

                Vector3 pos = new Vector3(parent.position.x, this.transform.position.y, parent.position.z);
                this.transform.position = pos;
                this.transform.SetParent(parent);
            }

            noteObject.NoteData.ChangeRange(address.SliderIndex);
        }

        void IScalableObject.OnFinishScale()
        {
            noteObject.SetCollidersActive(true);

        }

        public void OnChangeScale(int size)
        {
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