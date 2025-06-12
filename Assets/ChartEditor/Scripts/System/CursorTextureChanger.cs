using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;
using UniRx;

namespace ChartEditor
{
    /// <summary>
    /// 状況に応じてカーソルのアイコンを変更する 
    /// </summary>
    public class CursorTextureChanger : MonoBehaviour
    {
        [SerializeField] List<EditModeToTexture> cursorTextures;

        IChartEditorDataGetter dataGetter_model;

        EditMode currentEditMode;
        List<Type> dependenceObjectType = new List<Type>
        {
            typeof(IMovableCollider),
            typeof(IScalableCollider),
            typeof(IDestroyableCollider),
            typeof(IChangableCollider),
            typeof(IConnectableCollider),
            typeof(ISpaceEditableCollider),
        };

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            dataGetter_model = chartEditorDataGetter;
        }


        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // エディットモードの保存
            dataGetter_model?.CurrentEditMode
                .Subscribe(value =>
                {
                    currentEditMode = value;

                    // オートモードの時はそのままやっちゃう
                    if (dataGetter_model.AutoEditMode.Value)
                    {
                        if (currentEditMode == EditMode.Destroy) { return; }
                        if (currentEditMode == EditMode.ChangeType) { return; }
                        SetCursorTexture(value);
                    }
                })
                .AddTo(this.gameObject);

            // コライダーが追加されたときカーソルを変更する
            dataGetter_model?.InteractableColliders.ObserveAdd()
                .Subscribe(col => { 
                    foreach(var type in dependenceObjectType)
                    {
                        if(type.IsAssignableFrom(col.GetType())) 
                        { 
                            SetCursorTexture(currentEditMode, true); 
                        }
                    }
                }).AddTo(this.gameObject);

            // クリアされたとき
            dataGetter_model?.InteractableColliders.ObserveReset()
                .Subscribe(_ => {
                    SetCursorTexture(currentEditMode, false);
                }).AddTo(this.gameObject);
        }

        /// <summary>
        /// カーソルのテクスチャを変更する
        /// </summary>
        public void SetCursorTexture(EditMode editMode, bool isDisable = false)
        {
            Texture2D texture = null;

            if (!isDisable) 
            {
                foreach (var et in cursorTextures)
                {
                    texture = et.CheckAndGetTexture(editMode);

                    if (texture == null) { continue; }
                    else { break; }
                }
            }

            Vector2 hotspot = Vector2.zero;
            if (texture != null)
            {
                hotspot = new Vector2(texture.width / 2f, texture.height / 2f);
            }
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        #region その他雑クラス

        [System.Serializable]
        public class EditModeToTexture
        {
            [SerializeField] EditMode editMode;
            [SerializeField] Texture2D texture;

            public Texture2D CheckAndGetTexture(EditMode editMode)
            {
                if (this.editMode != editMode) { return null; }
                return texture;
            }
        }

        #endregion
    }

}
