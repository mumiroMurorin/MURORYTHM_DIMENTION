using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

            // 配置モードはテクスチャ変更なし

            // 移動モード
            dataGetter_model?.MovableObject
                .Where(_ => currentEditMode == EditMode.Move)
                .Subscribe(obj => {
                    SetCursorTexture(currentEditMode, obj == null);
                })
                .AddTo(this.gameObject);

            // スケーリング
            dataGetter_model?.ScalableObject
                .Where(_ => currentEditMode == EditMode.Scale)
                .Subscribe(obj => {
                    SetCursorTexture(currentEditMode, obj == null);
                })
                .AddTo(this.gameObject);

            // 削除
            dataGetter_model?.DestroyableObject
                .Where(_ => currentEditMode == EditMode.Destroy)
                .Subscribe(obj => {
                    SetCursorTexture(currentEditMode, obj == null);
                })
                .AddTo(this.gameObject);

            // タイプ変更
            dataGetter_model?.ChangableObject
                .Where(_ => currentEditMode == EditMode.ChangeType)
                .Subscribe(obj => {
                    SetCursorTexture(currentEditMode, obj == null);
                })
                .AddTo(this.gameObject);

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
