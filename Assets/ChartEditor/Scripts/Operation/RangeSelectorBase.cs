using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// ドラッグで矩形を描いて範囲選択する操作の共通処理 (ノーツ・頂点共通)
    /// 派生クラスは「いつ有効か」と「矩形内のどれを選択するか」を実装する
    /// </summary>
    public abstract class RangeSelectorBase : MonoBehaviour
    {
        [SerializeField] protected Camera mainCamera;

        protected IChartEditorDataGetter dataGetter;
        protected INotesDataGetter notesGetter;

        Vector2 startPos;
        bool isSelecting = false;

        /// <summary>
        /// 範囲選択を受け付ける状態か (編集レイヤーなど)
        /// </summary>
        protected abstract bool IsActive { get; }

        /// <summary>
        /// 範囲選択を受け付けないエディットモード
        /// </summary>
        protected abstract EditMode[] IgnoreEditModes { get; }

        /// <summary>
        /// カーソル下に選択可能なオブジェクトがあるか (あるときは範囲選択を始めない)
        /// </summary>
        protected abstract bool IsCursorOnSelectable { get; }

        /// <summary>
        /// 矩形(スクリーン座標、左下原点)の中にあるオブジェクトを選択する
        /// </summary>
        protected abstract void SelectObjects(Rect selectionRect);

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        void Update()
        {
            // 受け付けない状態になったら、描きかけの矩形も取り消す
            if (!IsActive || dataGetter.CurrentEditMode.Value.IsInEditModeList(IgnoreEditModes))
            {
                isSelecting = false;
                return;
            }

            // カーソル先にオブジェクトがある場合は返す
            if (!isSelecting && IsCursorOnSelectable) { return; }

            // 範囲選択開始
            if (Input.GetMouseButtonDown(0))
            {
                // カーソルがUI上にあるときは返す
                if (EventSystem.current.IsPointerOverGameObject()) { return; }

                startPos = Input.mousePosition;
                isSelecting = true;
            }

            // 範囲選択終了
            if (Input.GetMouseButtonUp(0) && isSelecting)
            {
                isSelecting = false;
                SelectObjects(GetScreenRectForContains(startPos, Input.mousePosition));
            }
        }

        void OnGUI()
        {
            if (!isSelecting) return;

            Rect guiRect = GetScreenRectForGUI(startPos, Input.mousePosition);
            DrawScreenRect(guiRect, new Color(0, 0.5f, 1f, 0.25f));
            DrawScreenRectBorder(guiRect, 2, Color.blue);
        }

        // GUI描画用
        public static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        /// <summary>
        /// 識別用：ScreenPoint 判定に使う Rect （左下原点）
        /// </summary>
        public static Rect GetScreenRectForContains(Vector2 screenPosition1, Vector2 screenPosition2)
        {
            Vector2 min = Vector2.Min(screenPosition1, screenPosition2);
            Vector2 max = Vector2.Max(screenPosition1, screenPosition2);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        /// <summary>
        /// 描画用：OnGUI で使う Rect （左上原点）
        /// </summary>
        public static Rect GetScreenRectForGUI(Vector2 screenPosition1, Vector2 screenPosition2)
        {
            // Y を反転
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;
            Vector2 min = Vector2.Min(screenPosition1, screenPosition2);
            Vector2 max = Vector2.Max(screenPosition1, screenPosition2);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            // 上、下、左、右の枠線
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            DrawScreenRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            DrawScreenRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            DrawScreenRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }
    }
}
