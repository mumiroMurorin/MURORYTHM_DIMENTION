using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class NoteDeployable : MonoBehaviour, IDeployableObject
    {
        [Tooltip("配置時のアウトライン色")]
        [SerializeField] private Color outlineColorOnDeploying;
        [SerializeField] private Renderer noteRenderer;

        NoteObject noteObject;

        private void Awake()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void IDeployableObject.OnInstantiate(IGroundNoteData noteData, Func<AddressInChart, Transform> getParentTransformFunc)
        {
            noteRenderer.material.color *= new Color(1, 1, 1, 0.5f);
            this.gameObject.SetActive(false);
            noteObject.SetCollidersActive(false);

            // アウトラインの設定
            noteObject.SetOutlineColor(outlineColorOnDeploying, true);
            noteObject.SetOutlineActive(true);

            noteObject.NoteData = noteData;
            noteObject.GetParentTransformFunc = getParentTransformFunc;
        }

        void IDeployableObject.OnDeploy()
        {
            // アウトラインを消す
            noteObject.SetOutlineActive(false);

            noteRenderer.material.color *= new Color(1, 1, 1, 2f);
            noteObject.SetCollidersActive(true);
        }

        void IDeployableObject.OnMove(Transform parent)
        {
            // 親オブジェクトに合わせた位置調整（Y 座標は維持）
            // まだ設置していないので引数のTransformを参照
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
