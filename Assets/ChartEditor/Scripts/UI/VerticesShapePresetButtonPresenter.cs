using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace ChartEditor
{
    public class VerticesShapePresetButtonPresenter : MonoBehaviour
    {
        [SerializeField] GameObject presetButtons_prefab;
        [SerializeField] ButtonView registerVerticesButton_view;
        [SerializeField] RectTransform buttonParent;
        [SerializeField] VerticesShapePresetApplier presetApplier_model;
        [SerializeField] VerticesPresetViewportView verticesViewport_view;

        IChartEditorDataGetter dataGetter_model;

        readonly List<VerticesShapePresetButtonView> runtimeButtons = new();
       
        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter)
        {
            dataGetter_model = chartEditorDataGetter;
        }

        void Start()
        {
            BindForView();
            SetEvent();
            RebuildButtons();
        }

        void BindForView()
        {
            if (dataGetter_model == null) { return; }
        }

        void SetEvent()
        {
            if (presetApplier_model != null)
            {
                presetApplier_model.OnPresetsChanged += OnPresetsChanged;
            }

            if (dataGetter_model != null && verticesViewport_view != null)
            {
                dataGetter_model.EditNoteType
                    .Subscribe(verticesViewport_view.OnChangeEditNoteType)
                    .AddTo(this.gameObject);
            }

            if (registerVerticesButton_view != null)
            {
                registerVerticesButton_view.OnPushButtonListner += () => presetApplier_model.RegisterCurrentVerticesAsPreset();
            }    
        }

        void OnDestroy()
        {
            if (presetApplier_model != null)
            {
                presetApplier_model.OnPresetsChanged -= OnPresetsChanged;
            }
        }

        void OnPresetsChanged(IReadOnlyList<VerticesShapePreset> presets)
        {
            RebuildButtons(presets);
        }

        void RebuildButtons()
        {
            RebuildButtons(presetApplier_model?.Presets);
        }

        void RebuildButtons(IReadOnlyList<VerticesShapePreset> presets)
        {
            if (presetButtons_prefab == null || buttonParent == null) { return; }

            ClearRuntimeButtons();

            if (presets == null) { return; }

            foreach (var preset in presets)
            {
                var button = CreatePresetButton(preset);
                if (button == null) { continue; }

                runtimeButtons.Add(button);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(buttonParent);
        }

        VerticesShapePresetButtonView CreatePresetButton(VerticesShapePreset preset)
        {
            var button = Instantiate(presetButtons_prefab, buttonParent).GetComponent<VerticesShapePresetButtonView>();
            if (button == null) { return null; }

            button.name = $"VerticesShapePresetButton_{preset.DisplayName}";
            button.transform.SetSiblingIndex(buttonParent.childCount - 1);
            button.SetDisplayName(preset.DisplayName);
            button.SetVertices(preset.Vertices);
            BindPresetButtonEvents(button, preset);
            return button;
        }

        void BindPresetButtonEvents(VerticesShapePresetButtonView button, VerticesShapePreset preset)
        {
            button.OnPushButtonListner += () => presetApplier_model?.ApplyPreset(preset);
            button.OnPushDeleteButtonListner += () => presetApplier_model?.RemovePreset(preset.Id);
            button.OnPushOverwriteButtonListner += () => presetApplier_model?.OverwritePreset(preset.Id);
        }

        void ClearRuntimeButtons()
        {
            foreach (var button in runtimeButtons)
            {
                if (button == null) { continue; }
                Destroy(button.gameObject);
            }

            runtimeButtons.Clear();
        }

        [Serializable]
        public class VerticesShapePresetButton
        {
            [SerializeField] VerticesShapePresetButtonView button_view;

            public VerticesShapePresetButtonView ButtonView => button_view;
        }
    }
}
