using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
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

        private void Start()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void IMovableObject.OnMoveStart()
        {
            noteObject.SetOutlineColor(outlineColorOnMove);
            noteObject.SetOutlineActive(true);
            noteObject.SetCollidersActive(false);
            this.transform.position += Vector3.up * addHeightOnMove;
        }

        void IMovableObject.OnMove(IDeployableCollider deployableCollider)
        {
            Transform parent = deployableCollider.deployParent;

            Vector3 pos = new Vector3(parent.position.x, this.transform.position.y, parent.position.z);
            this.transform.position = pos;
            this.transform.SetParent(parent);

            noteObject.NoteData.SetAddress(deployableCollider.Address);
        }

        void IMovableObject.OnMoveEnd()
        {
            noteObject.SetOutlineActive(false);
            noteObject.SetCollidersActive(true);
            this.transform.position -= Vector3.up * addHeightOnMove;
        }
    }

}
