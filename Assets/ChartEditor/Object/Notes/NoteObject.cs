using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using UnityFx.Outline;

namespace ChartEditor
{
    public abstract class NoteObject : MonoBehaviour , IDeployableObject, IMovableObject, IScalableObject
    {
        [Tooltip("移動時のOutline色")]
        [SerializeField] Color outlineColorOnMoving;

        [SerializeField] OutlineBehaviour outline;
        [SerializeField] GameObject origin;
        [SerializeField] Renderer _renderer;
        [SerializeField] Collider[] _colliders;

        protected NoteData noteData = new NoteData();

        void IDeployableObject.OnDeploy()
        {
            _renderer.material.color *= new Color(1, 1, 1, 2f);

            EnableCollider(true);
        }

        void IDeployableObject.OnInstantiate()
        {
            Initialize();
            Bind();

            _renderer.material.color *= new Color(1, 1, 1, 0.5f);

            EnableCollider(false);
        }

        private void Initialize()
        {
            if (noteData == null) { noteData = new NoteData(); }

            // 仮
            noteData.SetRange(new List<float>() { 0 });
        }

        private void Bind()
        {
            // 大きさの変更
            noteData.Range.ObserveCountChanged()
                .Subscribe(OnChangeScale)
                .AddTo(this.gameObject);

            noteData.Range.ObserveReplace()
                .Subscribe(value => OnChangeHorizontalPosition())
                .AddTo(this.gameObject);
        }

        private void EnableCollider(bool isActive)
        {
            foreach (Collider collider in _colliders)
            {
                collider.enabled = isActive;
            }
        }

        void IMovableObject.OnMoveStart()
        {
            // 色の変更
            outline.OutlineColor = outlineColorOnMoving;
            outline.enabled = true;

            // 持ち上げる
            transform.position += new Vector3(0, 2f, 0);
        }

        void IMovableObject.OnMoveEnd()
        {
            outline.enabled = false;
            transform.position -= new Vector3(0, 2f, 0);
        }

        /// <summary>
        /// ノートの移動
        /// </summary>
        /// <param name="parent"></param>
        void IMovableObject.OnMove(Transform parent)
        {
            transform.position = new Vector3(parent.position.x, transform.position.y, parent.position.z);
            transform.SetParent(parent);
        }

        void IScalableObject.OnScale()
        {
            noteData.AddRange(true);
            Debug.Log(string.Join(", ", noteData.Range));
        }

        private void OnChangeScale(int size)
        {
            Debug.Log(size);
            Transform tr = origin.transform;
            tr.localScale = new Vector3(size, tr.localScale.y, tr.localScale.z);
            tr.localPosition = new Vector3((size - 1) / 2f, tr.localPosition.y, tr.localPosition.z);
        }

        private void OnChangeHorizontalPosition()
        {
            Debug.Log("きちゃ");
        }
    }

    public class NoteData
    {
        ReactiveCollection<float> range = new ReactiveCollection<float>();

        public IReadOnlyReactiveCollection<float> Range { get { return range; } } 

        public void SetRange(List<float> range)
        {
            this.range = new ReactiveCollection<float>(range);
        }

        public void AddRange(bool isAddLast)
        {
            float value = isAddLast ? range.Last() + 1 : range[0] - 1;
            range.Insert(isAddLast ? range.Count : 0, value);
        }
    }
}