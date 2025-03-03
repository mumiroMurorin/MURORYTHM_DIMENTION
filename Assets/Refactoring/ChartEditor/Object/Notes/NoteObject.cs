using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;

namespace ChartEditor
{
    public abstract class NoteObject : MonoBehaviour , IDeployableObject, IMovableObject, IScalableObject
    {
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

            // ‰¼
            noteData.SetRange(new List<float>() { 0 });
        }

        private void Bind()
        {
            // ‘å‚«‚³‚Ì•ÏX
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
            transform.position += new Vector3(0, 2f, 0);
        }

        void IMovableObject.OnMoveEnd()
        {
            transform.position -= new Vector3(0, 2f, 0);
        }

        void IMovableObject.OnMove()
        {

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
            Debug.Log("‚«‚¿‚á");
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