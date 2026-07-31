using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class SpaceNoteMovable : MonoBehaviour, IFreedomMovableObject
    {
        [SerializeField] NoteObject noteObject;

        [Tooltip("移動時のアウトライン色")]
        [SerializeField] ColorSetting outlineColorOnMove;
        [Tooltip("移動時に浮かせる高さ")]
        [SerializeField] float addHeightOnMove = 1f;

        NoteObject IFreedomMovableObject.Note => noteObject;

        Vector3 addPos;
        CancellationTokenSource cts = new CancellationTokenSource();

        private void Start()
        {
            Bind(cts.Token).Forget();
        }

        private async UniTask Bind(CancellationToken token)
        {
            // ノートデータが設定されるまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData != null, cancellationToken: token);

            // アドレスが設定されるまで待つ
            await UniTask.WaitUntil(() => noteObject.NoteData.Address != null, cancellationToken: token);

            // アドレス変更時に配置先へ追従する
            noteObject.NoteData.Address.BarIndexRP
                .Subscribe(_ =>
                {
                    OnChangeAddress(noteObject.NoteData.Address);
                })
                .AddTo(this.gameObject);

            noteObject.NoteData.Address.SubDivisionIndexRP
                .Subscribe(_ =>
                {
                    OnChangeAddress(noteObject.NoteData.Address);
                })
                .AddTo(this.gameObject);

            noteObject.NoteData.Address.RangeRP.ObserveAdd()
                .Subscribe(_ =>
                {
                    OnChangeAddress(noteObject.NoteData.Address);
                })
                .AddTo(this.gameObject);
        }

        void IFreedomMovableObject.OnMoveStart()
        {
            noteObject.OutlineColors.Add(outlineColorOnMove);
            noteObject.SetCollidersActive(false);

            // 移動中は少し浮かせる
            addPos = Vector3.up * addHeightOnMove;
            this.transform.position += addPos;
        }

        void IFreedomMovableObject.OnMove()
        {
        }

        void IFreedomMovableObject.OnMoveEnd()
        {
            noteObject.OutlineColors.Remove(outlineColorOnMove);
            noteObject.SetCollidersActive(true);

            // 移動開始時に足した分を戻す
            this.transform.position -= addPos;
            addPos = Vector3.zero;
        }

        /// <summary>
        /// アドレス変更時にノーツの親位置へ追従する。
        /// </summary>
        private void OnChangeAddress(IReadOnlyAddressWithinRange address)
        {
            Transform parent = noteObject.GetParentTransformFunc(address);
            if (parent == null) { return; }

            Vector3 pos = parent.position + addPos;
            this.transform.position = pos;
            this.transform.SetParent(parent);

            if (TryGetComponent(out SpaceMeshController spaceMeshController))
            {
                spaceMeshController.RefreshVisualOriginFromAddress(addPos);
            }
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