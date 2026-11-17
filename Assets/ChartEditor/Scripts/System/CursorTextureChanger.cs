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
    ///
    /// 頂点の編集ツール (EditMode.VertexEdit) だけは、同じモードのまま、カーソルの下にあるもので絵が変わる:
    /// 頂点・中心点の上では「動かす」絵、頂点を置ける所では「置く」絵 (一覧の VertexEdit の絵)、それ以外は標準のカーソル
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

        // 頂点の編集ツールで、今出しているカーソル
        enum VertexEditCursor
        {
            // まだ決めていない (次の Update で決め直す)
            Unknown,
            Standard,
            Move,
            Place,
        }

        VertexEditCursor vertexEditCursor = VertexEditCursor.Unknown;


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
                    // 頂点の編集ツールのカーソルは、カーソルの下にあるもので決まる (Update で決める)
                    vertexEditCursor = VertexEditCursor.Unknown;
                    // プレビューがカーソルを指定している間は、そちらを優先する
                    if (previewCursorMode != EditMode.None) { return; }
                    if (currentEditMode.IsInEditModeList(ignoreEditModes)) { return; }
                    if (IsVertexTool(currentEditMode)) { return; }

                    SetCursorTexture(value);
                })
                .AddTo(this.gameObject);

            // クリアされたとき
            dataGetter_model?.InteractableColliders.ObserveReset()
                .Subscribe(_ => {
                    if (previewCursorMode != EditMode.None) { return; }
                    if (IsVertexTool(currentEditMode)) { return; }
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
            // (頂点の編集ツールは、いったん標準のカーソルにして、次の Update で決め直す)
            bool isStandard = isOnUI || currentEditMode.IsInEditModeList(ignoreEditModes) || IsVertexTool(currentEditMode);
            SetCursorTexture(currentEditMode, isStandard);
            vertexEditCursor = VertexEditCursor.Unknown;
        }

        private void Update()
        {
            // 頂点の編集ツール・配置ツール: カーソルの下にあるものに合わせて、カーソルの絵を変える
            if (!IsVertexTool(currentEditMode)) { return; }
            if (previewCursorMode != EditMode.None) { return; }

            VertexEditCursor cursor = GetVertexEditCursor();
            if (cursor == vertexEditCursor) { return; }
            vertexEditCursor = cursor;

            switch (cursor)
            {
                // 頂点・中心点の上: 動かす (頂点を動かしている最中と同じ絵)
                case VertexEditCursor.Move:
                    SetCursorTexture(EditMode.VertexMoving);
                    break;
                // 頂点を置ける所: 置く (絵は、一覧の VertexEdit の絵)
                case VertexEditCursor.Place:
                    SetCursorTexture(EditMode.VertexEdit);
                    break;
                default:
                    SetCursorTexture(EditMode.VertexEdit, true);
                    break;
            }
        }

        // 頂点の編集ツール・配置ツール (同じモードのまま、カーソルの下にあるもので絵が変わる)
        private static bool IsVertexTool(EditMode mode) => mode == EditMode.VertexEdit || mode == EditMode.VertexPlace;

        /// <summary>
        /// 頂点の編集ツール・配置ツールで、今出すカーソル (カーソルの下にあるもので決まる)
        /// </summary>
        private VertexEditCursor GetVertexEditCursor()
        {
            // ボタンなどの画面部品の上と、範囲選択の操作 (Shift) の間は、標準のカーソル
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) { return VertexEditCursor.Standard; }
            if (ModifierKey.Shift) { return VertexEditCursor.Standard; }

            // 頂点・中心点の上: 掴んで動かせる
            if (dataGetter_model.GetInteractableCollider<IPointMovableCollider>() != null) { return VertexEditCursor.Move; }
            // 配置ツールで、何も無い所の頂点を置ける所 (Ctrl を押している間は、複数選択の操作なので、置かない。編集ツールは置かない)
            if (currentEditMode == EditMode.VertexPlace && !ModifierKey.Ctrl && dataGetter_model.GetInteractableCollider<IPointDeployableCollider>() != null) { return VertexEditCursor.Place; }

            return VertexEditCursor.Standard;
        }

        /// <summary>
        /// カーソルのテクスチャを変更する
        /// </summary>
        public void SetCursorTexture(EditMode editMode, bool isDisable = false)
        {
            Texture2D texture = null;
            // 絵のどこを「指している点」にするか (左上が 0,0、右下が 1,1)
            Vector2 hotspotRatio = new Vector2(0.5f, 0.5f);

            if (!isDisable)
            {
                foreach (var modeToTexture in cursorTextures)
                {
                    texture = modeToTexture.CheckAndGetTexture(editMode);
                    if (texture == null) { continue; }

                    hotspotRatio = modeToTexture.Hotspot;
                    break;
                }
            }

            Vector2 hotspot = Vector2.zero;
            if (texture != null)
            {
                // 指している点は、絵の中に収める (外にあると、カーソルを設定できない)
                hotspot = new Vector2(
                    Mathf.Clamp(texture.width * hotspotRatio.x, 0f, texture.width - 1),
                    Mathf.Clamp(texture.height * hotspotRatio.y, 0f, texture.height - 1));
            }
            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }

        #region その他雑クラス

        [System.Serializable]
        public class EditModeToTexture
        {
            [SerializeField] EditMode editMode;
            [SerializeField] Texture2D texture;
            [Tooltip("絵のどこを「指している点」にするか (左上が 0,0、右下が 1,1。0.5,0.5 で絵の真ん中)。クリックした位置は、この点になる")]
            [SerializeField] Vector2 hotspot = new Vector2(0.5f, 0.5f);

            /// <summary>
            /// 絵のどこを「指している点」にするか (左上が 0,0、右下が 1,1)
            /// </summary>
            public Vector2 Hotspot => new Vector2(Mathf.Clamp01(hotspot.x), Mathf.Clamp01(hotspot.y));

            public Texture2D CheckAndGetTexture(EditMode editMode)
            {
                if (this.editMode != editMode) { return null; }
                return texture;
            }
        }

        #endregion
    }

}

