using System.Collections;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor 
{
    public class NoteRangeSelector : MonoBehaviour
    {
        [SerializeField] MultiNoteSelector multiNoteSelector;
        [SerializeField] NoteObjectsController notesController;
        [SerializeField] float minY;
        [SerializeField] float maxY;
        [SerializeField] Camera mainCamera;

        Vector2 startPos;
        Vector2 endPos;
        bool isSelecting = false;

        IChartEditorDataGetter chartEditorDataGetter;
        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.Connecting,
            EditMode.EditBarConfig,
            EditMode.EditingBarConfig,
            EditMode.EditSubDivisionConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
        };

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
        }

        void Update()
        {
            if (chartEditorDataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return; }

            // カーソル先にオブジェクトがある場合は返す
            var collider = chartEditorDataGetter.GetInteractableCollider<ISelectableNoteCollider>();
            if (!isSelecting && collider != null) { return; }

            // 範囲選択開始
            if (Input.GetMouseButtonDown(0))
            {
                // カーソルがUI上にあるときは返す
                if (EventSystem.current.IsPointerOverGameObject()) { return; }

                startPos = Input.mousePosition;
                isSelecting = true;
            }
            
            // 範囲選択終了
            if (Input.GetMouseButtonUp(0))
            {
                endPos = Input.mousePosition;
                isSelecting = false;
                SelectObjects();
            }
        }

        void OnGUI()
        {
            if (!isSelecting) return;

            Rect guiRect = GetScreenRectForGUI(startPos, Input.mousePosition);
            DrawScreenRect(guiRect, new Color(0, 0.5f, 1f, 0.25f));
            DrawScreenRectBorder(guiRect, 2, Color.blue);
        }

        void SelectObjects()
        {
            Rect selectionRect = GetScreenRectForContains(startPos, endPos);

            foreach (var dtn in notesController.DataToObj.List)
            {
                var obj = dtn.Object;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);

                // カメラの前方にあるか
                if (screenPos.z < 0f) { continue; }
                if (obj.transform.position.y < minY || maxY < obj.transform.position.y) { continue; }

                if (!selectionRect.Contains((Vector2)screenPos)) { continue; }
                if (!dtn.Object.TryGetComponent(out ISelectableNoteObject selectable)) { continue; }

                multiNoteSelector.SelectMulti(selectable);
            }
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
