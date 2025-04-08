using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteDeployable : MonoBehaviour, IDeployableObject
    {
        [SerializeField] private Renderer noteRenderer;

        NoteObject noteObject;
        NoteObject IDeployableObject.Note => noteObject;

        private void Awake()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void IDeployableObject.OnInstantiate(NoteData noteData)
        {
            noteRenderer.material.color *= new Color(1, 1, 1, 0.5f);
            this.gameObject.SetActive(false);
            noteObject.SetCollidersActive(false);

            noteObject.NoteData = noteData;
        }

        void IDeployableObject.OnDeploy()
        {
            noteRenderer.material.color *= new Color(1, 1, 1, 2f);
            noteObject.SetCollidersActive(true);
        }

        void IDeployableObject.OnMove(Transform parent)
        {
            // 親オブジェクトに合わせた位置調整（Y 座標は維持）
            Vector3 pos = new Vector3(parent.position.x, this.transform.position.y, parent.position.z);
            this.transform.position = pos;
            this.transform.SetParent(parent);
            this.gameObject.SetActive(true);
        }

        void IDeployableObject.OnDisable()
        {
            noteObject.Destroy();
        }
    }

}
