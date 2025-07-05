using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace ChartEditor
{
    [RequireComponent(typeof(NoteObject))]
    public class SpaceDeployable : MonoBehaviour, IFreedomDeployableObject
    {
        [Tooltip("配置時のアウトライン色")]
        [SerializeField] private ColorSetting outlineColorOnDeploying;
        [SerializeField] private Renderer noteRenderer;

        NoteObject noteObject;
        public Action OnDestroyListner { get; set; }

        private void Awake()
        {
            noteObject = GetComponent<NoteObject>();
        }

        void IFreedomDeployableObject.OnInstantiate(IDeployableNoteData noteData, Func<AddressWithinRange, Transform> getParentTransformFunc)
        {
            noteRenderer.material.color *= new Color(1, 1, 1, 0.5f);
            this.gameObject.SetActive(false);
            noteObject.SetCollidersActive(false);

            // アウトラインの設定
            noteObject.OutlineColors.Add(outlineColorOnDeploying);

            noteObject.NoteData = noteData;
            noteObject.GetParentTransformFunc = getParentTransformFunc;

        }

        void IFreedomDeployableObject.OnDeploy()
        {
            // アウトラインを消す
            noteObject.OutlineColors.Remove(outlineColorOnDeploying);

            noteRenderer.material.color *= new Color(1, 1, 1, 2f);
            noteObject.SetCollidersActive(true);
        }

        void IFreedomDeployableObject.OnMove(Transform parent)
        {
            // 親オブジェクトに合わせた位置調整
            // まだ設置していないので引数のTransformを参照
            this.transform.position = parent.position;

            this.transform.SetParent(parent);
            this.gameObject.SetActive(true);
        }

        void IFreedomDeployableObject.OnDisable()
        {
            noteObject.Destroy();
        }

        private void OnDestroy()
        {
            OnDestroyListner?.Invoke();
        }
    }

}
