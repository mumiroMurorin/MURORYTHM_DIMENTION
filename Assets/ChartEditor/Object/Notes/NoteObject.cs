using System.Collections.Generic;
using UnityEngine;
using UniRx;
using UnityFx.Outline;

namespace ChartEditor
{
    /// <summary>
    /// 譜面上のノーツオブジェクト。Unity のコンポーネントとして動作し、
    /// 各種操作はピュアクラスに委譲する。
    /// </summary>
    public abstract class NoteObject : MonoBehaviour, IDeployableObject, IMovableObject, IScalableObject, IDestroyableObject
    {
        [Header("Basic Settings")]
        [Tooltip("配置時の元となる GameObject")]
        public GameObject origin;

        [SerializeField] private Renderer noteRenderer;
        [SerializeField] private Collider[] colliders;
        [SerializeField] private OutlineBehaviour outline;
        [Tooltip("移動時のアウトライン色")]
        [SerializeField] private Color outlineColorOnMove;

        private NoteData noteData = new NoteData();

        // ピュアクラスのインスタンス
        private DeployableNotePure deployableNote;
        private MovableNotePure movableNote;
        private ScalableNotePure scalableNote;
        private DestroyableNotePure destroyableNote;

        private void Awake()
        {
            // noteData 初期化（必要に応じて）
            if (noteData == null) { noteData = new NoteData(); }
            noteData.SetRange(new List<float> { 0 });

            // 必要な依存（GameObject、Renderer、NoteData、origin など）や、
            // コライダーの有効化、アウトライン設定用の delegate を渡してピュアクラスのインスタンスを作成
            deployableNote = new DeployableNotePure(
                gameObject,
                noteRenderer,
                SetCollidersActive,
                Destroy);

            // 例として移動開始時のアウトライン色は引数で渡す
            movableNote = new MovableNotePure(
                gameObject,
                () => SetOutlineColor(outlineColorOnMove),
                SetOutlineActive,
                SetCollidersActive,
                new Vector3(0, 2f, 0));

            scalableNote = new ScalableNotePure(
                noteData,
                origin,
                gameObject);

            destroyableNote = new DestroyableNotePure(Destroy);
        }

        #region Utility Methods

        /// <summary>
        /// 全てのコライダーの有効／無効を切り替える
        /// </summary>
        private void SetCollidersActive(bool isActive)
        {
            foreach (var col in colliders)
            {
                col.enabled = isActive;
            }
        }

        /// <summary>
        /// アウトラインカラーの設定用メソッド
        /// </summary>
        private void SetOutlineColor(Color color)
        {
            if (outline != null) { outline.OutlineColor = color; }
        }

        /// <summary>
        /// アウトラインのON/OFFを切り替える
        /// </summary>
        private void SetOutlineActive(bool active)
        {
            if (outline != null) { outline.enabled = active; }
        }

        /// <summary>
        /// このオブジェクトの削除
        /// </summary>
        private void Destroy()
        {
            Destroy(this.gameObject);
        }

        #endregion

        #region IDeployableObject Implementation

        void IDeployableObject.OnInstantiate() => deployableNote.OnInstantiate();

        void IDeployableObject.OnDeploy() => deployableNote.OnDeploy();

        void IDeployableObject.OnMove(Transform parent) => deployableNote.OnMove(parent);

        void IDeployableObject.OnDisable() => deployableNote.OnDisable();

            #endregion

        #region IMovableObject Implementation

        void IMovableObject.OnMoveStart() => movableNote.OnMoveStart();

        void IMovableObject.OnMove(Transform parent) => movableNote.OnMove(parent);

        void IMovableObject.OnMoveEnd() => movableNote.OnMoveEnd();

        #endregion

        #region IScalableObject Implementation

        void IScalableObject.OnScale() => scalableNote.OnScale();

        #endregion

        #region IDestroyable Implemention

        void IDestroyableObject.OnDestroy() => destroyableNote.OnDestroy();

        #endregion
    }

    /// <summary>
    /// ピュアクラス：配置（Deploy）処理を実装。
    /// </summary>
    public class DeployableNotePure : IDeployableObject
    {
        private readonly GameObject noteGO;
        private readonly Renderer renderer;
        private readonly System.Action<bool> setCollidersActive;
        private readonly System.Action destroy;

        public DeployableNotePure(GameObject noteGO, Renderer renderer, System.Action<bool> setCollidersActive, System.Action destroy)
        {
            this.noteGO = noteGO;
            this.renderer = renderer;
            this.setCollidersActive = setCollidersActive;
            this.destroy = destroy;
        }

        public void OnInstantiate()
        {
            // 初期状態：半透明、非アクティブ、コライダー無効
            renderer.material.color *= new Color(1, 1, 1, 0.5f);
            noteGO.SetActive(false);
            setCollidersActive(false);
        }

        public void OnDeploy()
        {
            // 配置時：濃い色、コライダー有効
            renderer.material.color *= new Color(1, 1, 1, 2f);
            setCollidersActive(true);
        }

        public void OnMove(Transform parent)
        {
            // 親オブジェクトに合わせた位置調整（Y 座標は維持）
            Vector3 pos = new Vector3(parent.position.x, noteGO.transform.position.y, parent.position.z);
            noteGO.transform.position = pos;
            noteGO.transform.SetParent(parent);
            noteGO.SetActive(true);
        }

        public void OnDisable()
        {
            destroy.Invoke();
        }
    }

    /// <summary>
    /// ピュアクラス：移動（Move）処理を実装。
    /// </summary>
    public class MovableNotePure : IMovableObject
    {
        private readonly GameObject noteGO;
        private readonly System.Action setOutlineColor;
        private readonly System.Action<bool> setOutlineActive;
        private readonly System.Action<bool> setCollidersActive;
        private readonly Vector3 moveOffset;

        GameObject IMovableObject.gameObject => noteGO;

        public MovableNotePure(GameObject noteGO, System.Action setOutlineColor, System.Action<bool> setOutlineActive, System.Action<bool> setCollidersActive, Vector3 moveOffset)
        {
            this.noteGO = noteGO;
            this.setOutlineColor = setOutlineColor;
            this.setOutlineActive = setOutlineActive;
            this.setCollidersActive = setCollidersActive;
            this.moveOffset = moveOffset;
        }

        public void OnMoveStart()
        {
            setOutlineColor();
            setOutlineActive(true);
            setCollidersActive(false);
            noteGO.transform.position += moveOffset;
        }

        public void OnMove(Transform parent)
        {
            Vector3 pos = new Vector3(parent.position.x, noteGO.transform.position.y, parent.position.z);
            noteGO.transform.position = pos;
            noteGO.transform.SetParent(parent);
        }

        public void OnMoveEnd()
        {
            setOutlineActive(false);
            setCollidersActive(true);
            noteGO.transform.position -= moveOffset;
        }
    }

    /// <summary>
    /// ピュアクラス：拡大縮小（Scale）処理を実装。
    /// </summary>
    public class ScalableNotePure : IScalableObject
    {
        private readonly GameObject noteGO;
        private readonly GameObject originGO;
        private readonly NoteData noteData;

        GameObject IScalableObject.gameObject => noteGO;

        public ScalableNotePure(NoteData noteData, GameObject originGO, GameObject noteGO)
        {
            this.noteGO = noteGO;
            this.noteData = noteData;
            this.originGO = originGO;

            Bind();
        }

        /// <summary>
        /// ノートデータの変化に応じてスケールや横位置を更新する。
        /// </summary>
        private void Bind()
        {
            // 大きさの変更通知に対してスケール更新
            noteData.Range.ObserveCountChanged()
                .Subscribe(OnChangeScale)
                .AddTo(noteGO);
        }

        public void OnScale()
        {
            noteData.AddRange(true);
        }

        public void OnChangeScale(int size)
        {
            Transform tr = originGO.transform;
            tr.localScale = new Vector3(size, tr.localScale.y, tr.localScale.z);
            tr.localPosition = new Vector3((size - 1) / 2f, tr.localPosition.y, tr.localPosition.z);
        }
    }

    /// <summary>
    /// ピュアクラス：削除（Destroy）処理を実装。
    /// </summary>
    public class DestroyableNotePure : IDestroyableObject
    {
        private readonly System.Action destroy;

        public DestroyableNotePure(System.Action destroy) 
        {
            this.destroy = destroy;
        }

        public void OnDestroy()
        {
            destroy.Invoke();
        }
    }
}
