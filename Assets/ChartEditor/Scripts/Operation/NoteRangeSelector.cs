using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor 
{
    public class NoteRangeSelector : MonoBehaviour
    {
        [SerializeField] MultiNoteSelector multiNoteSelector;
        [SerializeField] float minZ;
        [SerializeField] float maxZ;
        [SerializeField] Camera mainCamera;

        List<ISelectableNoteObject> selectingObjects = new List<ISelectableNoteObject>();

        Vector2 startPos;
        Vector2 endPos;
        bool isSelecting = false;

        public List<GameObject> selectableObjects;
        public List<GameObject> selectedObjects = new List<GameObject>();

        IChartEditorDataGetter chartEditorDataGetter;
        EditMode[] ignoreEditModes = new EditMode[] {
            EditMode.Deploy,
            EditMode.SpaceDeploy,
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
            if (collider != null) { return; }

            // 選択開始
            if (Input.GetMouseButtonDown(0))
            {
                startPos = Input.mousePosition;
                isSelecting = true;
            }

            // 選択終了
            if (Input.GetMouseButtonUp(0))
            {
                endPos = Input.mousePosition;
                isSelecting = false;
                SelectObjects();
            }
        }

        void OnGUI()
        {
            if (isSelecting)
            {
                var rect = GetScreenRect(startPos, Input.mousePosition);
                DrawScreenRect(rect, new Color(0.8f, 0.8f, 1f, 0.25f));
                DrawScreenRectBorder(rect, 2, Color.blue);
            }
        }

        void SelectObjects()
        {
            selectedObjects.Clear();

            Rect selectionRect = GetScreenRect(startPos, endPos);

            foreach (GameObject obj in selectableObjects)
            {
                Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);

                // カメラの前方にあるか
                if (screenPos.z < 0f) { continue; }

                if (selectionRect.Contains(screenPos, true))
                {
                    selectedObjects.Add(obj);
                    // ここで色を変えるなどのフィードバックを入れても良い
                }
            }
        }

        // Rect作成ユーティリティ
        public static Rect GetScreenRect(Vector2 screenPosition1, Vector2 screenPosition2)
        {
            screenPosition1.y = Screen.height - screenPosition1.y;
            screenPosition2.y = Screen.height - screenPosition2.y;
            var topLeft = Vector2.Min(screenPosition1, screenPosition2);
            var bottomRight = Vector2.Max(screenPosition1, screenPosition2);
            return Rect.MinMaxRect(topLeft.x, topLeft.y, bottomRight.x, bottomRight.y);
        }

        // GUI描画用
        public static void DrawScreenRect(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = Color.white;
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
