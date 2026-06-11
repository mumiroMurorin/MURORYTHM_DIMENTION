using UnityEngine;
using TMPro;

namespace ChartEditor
{
    public class VerticesShapePresetButtonView : ButtonView
    {
        [SerializeField] VerticesShapePreviewGraphic previewGraphic;
        [SerializeField] TMP_Text label;
        [SerializeField] ButtonView deleteButton_view;
        [SerializeField] ButtonView overwriteButton_view;

        public event System.Action OnPushDeleteButtonListner;
        public event System.Action OnPushOverwriteButtonListner;

        void Awake()
        {
            if (deleteButton_view != null)
            {
                deleteButton_view.OnPushButtonListner += OnPushDeleteButton;
            }

            if (overwriteButton_view != null)
            {
                overwriteButton_view.OnPushButtonListner += OnPushOverwriteButton;
            }
        }

        void OnDestroy()
        {
            if (deleteButton_view != null)
            {
                deleteButton_view.OnPushButtonListner -= OnPushDeleteButton;
            }

            if (overwriteButton_view != null)
            {
                overwriteButton_view.OnPushButtonListner -= OnPushOverwriteButton;
            }
        }

        void Reset()
        {
            previewGraphic = GetComponentInChildren<VerticesShapePreviewGraphic>(true);
            label = GetComponentInChildren<TMP_Text>(true);
        }

        public void SetVertices(Vector2[] vertices)
        {
            if (previewGraphic == null)
            {
                previewGraphic = GetComponentInChildren<VerticesShapePreviewGraphic>(true);
            }

            previewGraphic?.SetVertices(vertices);
        }

        public void SetDisplayName(string displayName)
        {
            if (label == null)
            {
                label = GetComponentInChildren<TMP_Text>(true);
            }

            if (label == null) { return; }

            label.gameObject.SetActive(!string.IsNullOrWhiteSpace(displayName));
            label.text = displayName;
        }

        void OnPushDeleteButton()
        {
            OnPushDeleteButtonListner?.Invoke();
        }

        void OnPushOverwriteButton()
        {
            OnPushOverwriteButtonListner?.Invoke();
        }
    }
}
