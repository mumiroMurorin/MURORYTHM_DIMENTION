using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using VContainer;
using UniRx;
using UnityEngine.EventSystems;

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
        // プレビューの上で出すカーソル (EditMode.None のときは、プレビューからの指定なし)
        EditMode previewCursorMode = EditMode.None;
        Dictionary<EditMode, Type> dependenceObjectType = new Dictionary<EditMode, Type>
        {
            { EditMode.Move,typeof(IMovableCollider) },
            { EditMode.Scale,typeof(IScalableCollider) },
            { EditMode.ChangeType,typeof(IChangableCollider) },
            { EditMode.Connect,typeof(IConnectableCollider) },
            { EditMode.Connecting,typeof(IConnectableCollider) },
            { EditMode.DisConnect,typeof(IConnectableCollider) },
        };

        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.ChangeType,
            EditMode.Preview,
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
                    // プレビューがカーソルを指定している間は、そちらを優先する
                    if (previewCursorMode != EditMode.None) { return; }
                    if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }

                    SetCursorTexture(value);
                })
                .AddTo(this.gameObject);

            // クリアされたとき
            dataGetter_model?.InteractableColliders.ObserveReset()
                .Subscribe(_ => {
                    if (previewCursorMode != EditMode.None) { return; }
                    SetCursorTexture(currentEditMode, true);
                }).AddTo(this.gameObject);

            // コライダーが追加されたときカーソルを変更する
            dataGetter_model?.InteractableColliders.ObserveAdd()
                .Subscribe(col => { 
                    if (previewCursorMode != EditMode.None) { return; }
                    foreach(var type in dependenceObjectType)
                    {
                        if (currentEditMode == type.Key && type.Value.IsAssignableFrom(col.Value.GetType())) 
                        {
                            SetCursorTexture(currentEditMode); 
                        }
                    }
                }).AddTo(this.gameObject);
        }

        /// <summary>
        /// プレビューの上でのカーソルを指定する (プレビューのノーツを動かす・幅を変えるとき用)
        /// 指定している間は、エディットモードに関わらず、そのモードのカーソルを出し続ける
        /// </summary>
        /// <param name="editMode">カーソルを出すモード (EditMode.Move なら移動のカーソル)。EditMode.None で指定をやめる</param>
        public void SetPreviewCursor(EditMode editMode)
        {
            if (previewCursorMode == editMode) { return; }
            previewCursorMode = editMode;

            if (editMode != EditMode.None)
            {
                SetCursorTexture(editMode);
                return;
            }

            // 指定をやめたとき: 今のエディットモードのカーソルに戻す
            // (ボタンなどの画面部品の上にいるときと、カーソルを切り替えないモードのときは、標準のカーソルにする)
            bool isOnUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            bool isStandard = isOnUI || currentEditMode.IsInEditModeList(ignoreEditModes);
            SetCursorTexture(currentEditMode, isStandard);
        }

        /// <summary>
        /// カーソルのテクスチャを変更する
        /// </summary>
        public void SetCursorTexture(EditMode editMode, bool isDisable = false)
        {
            Texture2D texture = null;

            if (!isDisable) 
            {
                foreach (var modeToTexture in cursorTextures)
                {
                    texture = modeToTexture.CheckAndGetTexture(editMode);

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

