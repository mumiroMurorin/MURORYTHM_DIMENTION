using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using VContainer;

namespace ChartEditor
{
    public class VertexDetailPresenter : MonoBehaviour
    {
        [SerializeField] MultiVertexSelector vertexSelector_model;
        [SerializeField] VertexDetailViewportView vertexDetailViewport_view;
        [SerializeField] VertexPositionPanelView positionPanelView_view;
        [SerializeField] GuideImagePanelView guideImagePanelView_view;
        [SerializeField] GuideImageObject guideImageObject_view;
        [SerializeField] TextGuidePanelView textGuidePanelView_view;
        [SerializeField] TextGuideObject textGuideObject_view;
        [SerializeField, Min(1)] int textGuideFontSize = 64;
        [SerializeField, Min(0)] float initialSize = 0.5f;
        [SerializeField, Min(0)] float initialAlpha = 0.6f;
        [SerializeField, Min(0)] int decimalDigits = 3;

        readonly CompositeDisposable disposables = new CompositeDisposable();
        readonly CompositeDisposable selectedVertexDisposables = new CompositeDisposable();

        IChartEditorDataGetter dataGetter_model; 
        VertexData selectedVertex;
        CancellationTokenSource guideImageLoadCts;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter)
        {
            dataGetter_model = dataGetter;
        }


        void Start()
        {
            Bind();
            RefreshSelectionState();
        }

        void OnDestroy()
        {
            if (positionPanelView_view != null)
            {
                positionPanelView_view.OnXValueChangedListener -= OnXValueChanged;
                positionPanelView_view.OnYValueChangedListener -= OnYValueChanged;
            }

            if (guideImagePanelView_view != null)
            {
                guideImagePanelView_view.OnSelectImageButtonClickedListener -= OnSelectGuideImageButtonClicked;
                guideImagePanelView_view.OnImageEnableValueChangedListener -= SetGuideImageEnabled;
                guideImagePanelView_view.OnXValueChangedListener -= OnGuideImageXChanged;
                guideImagePanelView_view.OnYValueChangedListener -= OnGuideImageYChanged;
                guideImagePanelView_view.OnScaleValueChangedListener -= OnGuideImageScaleChanged;
                guideImagePanelView_view.OnRotationValueChangedListener -= OnGuideImageRotationChanged;
                guideImagePanelView_view.OnAlphaValueChangedListener -= OnGuideImageAlphaChanged;
            }

            if (textGuidePanelView_view != null)
            {
                textGuidePanelView_view.OnFontNameChangedListener -= OnTextGuideFontNameChanged;
                textGuidePanelView_view.OnTextEnableValueChangedListener -= SetTextGuideEnabled;
                textGuidePanelView_view.OnTextValueChangedListener -= OnTextGuideTextChanged;
                textGuidePanelView_view.OnXValueChangedListener -= OnTextGuideXChanged;
                textGuidePanelView_view.OnYValueChangedListener -= OnTextGuideYChanged;
                textGuidePanelView_view.OnScaleValueChangedListener -= OnTextGuideScaleChanged;
                textGuidePanelView_view.OnRotationValueChangedListener -= OnTextGuideRotationChanged;
                textGuidePanelView_view.OnAlphaValueChangedListener -= OnTextGuideAlphaChanged;
            }

            CancelGuideImageLoading();
            selectedVertexDisposables.Dispose();
            disposables.Dispose();
        }

        void Bind()
        {
            if (vertexSelector_model != null)
            {
                vertexSelector_model.OnSelectionChanged
                    .Subscribe(_ => RefreshSelectionState())
                    .AddTo(disposables);
            }

            if(dataGetter_model != null)
            {
                dataGetter_model.EditNoteType
                    .Subscribe(vertexDetailViewport_view.OnChangeEditNoteType)
                    .AddTo(disposables);
            }

            if (positionPanelView_view != null)
            {
                positionPanelView_view.OnXValueChangedListener += OnXValueChanged;
                positionPanelView_view.OnYValueChangedListener += OnYValueChanged;
            }

            if (guideImagePanelView_view != null)
            {
                guideImagePanelView_view.OnSelectImageButtonClickedListener += OnSelectGuideImageButtonClicked;
                guideImagePanelView_view.OnImageEnableValueChangedListener += SetGuideImageEnabled;
                guideImagePanelView_view.OnXValueChangedListener += OnGuideImageXChanged;
                guideImagePanelView_view.OnYValueChangedListener += OnGuideImageYChanged;
                guideImagePanelView_view.OnScaleValueChangedListener += OnGuideImageScaleChanged;
                guideImagePanelView_view.OnRotationValueChangedListener += OnGuideImageRotationChanged;
                guideImagePanelView_view.OnAlphaValueChangedListener += OnGuideImageAlphaChanged;
            }

            if (textGuidePanelView_view != null)
            {
                textGuidePanelView_view.OnFontNameChangedListener += OnTextGuideFontNameChanged;
                textGuidePanelView_view.OnTextEnableValueChangedListener += SetTextGuideEnabled;
                textGuidePanelView_view.OnTextValueChangedListener += OnTextGuideTextChanged;
                textGuidePanelView_view.OnXValueChangedListener += OnTextGuideXChanged;
                textGuidePanelView_view.OnYValueChangedListener += OnTextGuideYChanged;
                textGuidePanelView_view.OnScaleValueChangedListener += OnTextGuideScaleChanged;
                textGuidePanelView_view.OnRotationValueChangedListener += OnTextGuideRotationChanged;
                textGuidePanelView_view.OnAlphaValueChangedListener += OnTextGuideAlphaChanged;
            }

            RefreshInstalledFontList();
        }

        void RefreshSelectionState()
        {
            if (positionPanelView_view == null || guideImagePanelView_view == null || textGuidePanelView_view == null) { return; }

            UpdateGuideImageView();
            UpdateTextGuideView();

            if (vertexSelector_model == null) { return; }

            if (vertexSelector_model.SelectingVertices.Count != 1)
            {
                ClearSelectedVertex();
                SetPositionPanelEnabled(false);
                return;
            }

            var nextVertex = vertexSelector_model.SelectingVertices[0];
            if (selectedVertex == nextVertex)
            {
                SetPositionPanelEnabled(true);
                UpdatePositionView(nextVertex.Position.Value);
                return;
            }

            ClearSelectedVertex();
            selectedVertex = nextVertex;
            SetPositionPanelEnabled(true);
            UpdatePositionView(selectedVertex.Position.Value);

            selectedVertex.Position
                .Subscribe(position =>
                {
                    UpdatePositionView(position);
                })
                .AddTo(selectedVertexDisposables);
        }

        void ClearSelectedVertex()
        {
            selectedVertexDisposables.Clear();
            selectedVertex = null;
        }

        void SetPositionPanelEnabled(bool enabled)
        {
            positionPanelView_view.SetInteractable(enabled);
            if (!enabled)
            {
                positionPanelView_view.Clear();
            }
        }

        void UpdatePositionView(Vector2 position)
        {
            positionPanelView_view.SetPosition(position, decimalDigits);
        }

        void SetGuideImagePanelEnabled(bool enabled)
        {
            guideImagePanelView_view.SetInteractable(enabled && guideImageObject_view != null && guideImageObject_view.IsSettingImage);
            if (!enabled)
            {
                guideImagePanelView_view.Clear();
            }
        }

        void SetTextGuidePanelEnabled(bool enabled)
        {
            textGuidePanelView_view.SetInteractable(enabled);
            if (!enabled)
            {
                textGuidePanelView_view.Clear();
            }
        }

        void OnXValueChanged(float value)
        {
            if (selectedVertex == null) { return; }

            var current = selectedVertex.Position.Value;
            selectedVertex.SetPosition(new Vector2(value, current.y));
        }

        void OnYValueChanged(float value)
        {
            if (selectedVertex == null) { return; }

            var current = selectedVertex.Position.Value;
            selectedVertex.SetPosition(new Vector2(current.x, value));
        }

        void OnSelectGuideImageButtonClicked()
        {
            SelectGuideImageAsync().Forget();
        }

        async UniTaskVoid SelectGuideImageAsync()
        {
            if (dataGetter_model != null && dataGetter_model.EditNoteType.Value != EditNoteType.Vertices) { return; }

            CancelGuideImageLoading();
            guideImageLoadCts = new CancellationTokenSource();

            try
            {
                var sprite = await ImageFileSelector.SelectImageSpriteAsync(guideImageLoadCts.Token);
                if (sprite == null) { return; }
                if (guideImageObject_view == null) { return; }

                guideImageObject_view.SetSprite(sprite);
                guideImageObject_view.SetScale(1f);
                guideImageObject_view.SetRotation(0f);
                guideImageObject_view.SetAlpha(initialAlpha);
                UpdateGuideImageView();
                SetGuideImagePanelEnabled(true);
            }
            catch (System.OperationCanceledException)
            {
            }
        }

        void SetGuideImageEnabled(bool enabled)
        {
            if (guideImageObject_view == null) { return; }

            guideImageObject_view.SetEnabled(enabled);
        }

        void RefreshInstalledFontList()
        {
            var installedFontNames = InstalledFontProvider.GetInstalledFontNames();
            textGuidePanelView_view?.SetFontOptions(installedFontNames);

            if (textGuideObject_view != null && !string.IsNullOrWhiteSpace(textGuideObject_view.FontName))
            {
                textGuidePanelView_view?.SetSelectedFontName(textGuideObject_view.FontName);
            }
        }

        void OnTextGuideFontNameChanged(string fontName)
        {
            if (dataGetter_model != null && dataGetter_model.EditNoteType.Value != EditNoteType.Vertices) { return; }
            if (textGuideObject_view == null) { return; }
            if (string.IsNullOrWhiteSpace(fontName) || fontName == "No Fonts") { return; }

            var fontPath = InstalledFontProvider.GetFontFilePath(fontName);
            var font = !string.IsNullOrWhiteSpace(fontPath)
                ? new Font(fontPath)
                : Font.CreateDynamicFontFromOSFont(fontName, textGuideFontSize);

            if (font == null)
            {
                Debug.LogWarning($"\u3010TextGuide\u3011Could not create font: {fontName}");
                return;
            }

            textGuideObject_view.SetFont(font, textGuideFontSize);
            textGuideObject_view.SetText(string.IsNullOrWhiteSpace(textGuideObject_view.Text) ? "Text" : textGuideObject_view.Text);
            textGuideObject_view.SetScale(initialSize);
            textGuideObject_view.SetRotation(0f);
            textGuideObject_view.SetAlpha(initialAlpha);
            textGuideObject_view.SetEnabled(true);

            UpdateTextGuideView();
            SetTextGuidePanelEnabled(true);
        }

        void SetTextGuideEnabled(bool enabled)
        {
            if (textGuideObject_view == null) { return; }

            textGuideObject_view.SetEnabled(enabled);
        }

        void OnTextGuideTextChanged(string value)
        {
            if (textGuideObject_view == null) { return; }

            textGuideObject_view.SetText(value);
            UpdateTextGuideView();
        }

        void OnTextGuideXChanged(float value)
        {
            if (textGuideObject_view == null || !textGuideObject_view.IsSettingFont) { return; }

            var current = textGuideObject_view.LocalPosition;
            textGuideObject_view.SetLocalPosition(new Vector3(value, current.y, current.z));
            UpdateTextGuideView();
        }

        void OnTextGuideYChanged(float value)
        {
            if (textGuideObject_view == null || !textGuideObject_view.IsSettingFont) { return; }

            var current = textGuideObject_view.LocalPosition;
            textGuideObject_view.SetLocalPosition(new Vector3(current.x, value, current.z));
            UpdateTextGuideView();
        }

        void OnTextGuideScaleChanged(float value)
        {
            if (textGuideObject_view == null || !textGuideObject_view.IsSettingFont) { return; }

            textGuideObject_view.SetScale(value);
            UpdateTextGuideView();
        }

        void OnTextGuideRotationChanged(float value)
        {
            if (textGuideObject_view == null || !textGuideObject_view.IsSettingFont) { return; }

            textGuideObject_view.SetRotation(value);
            UpdateTextGuideView();
        }

        void OnTextGuideAlphaChanged(float value)
        {
            if (textGuideObject_view == null || !textGuideObject_view.IsSettingFont) { return; }

            textGuideObject_view.SetAlpha(value);
            UpdateTextGuideView();
        }

        void OnGuideImageXChanged(float value)
        {
            if (guideImageObject_view == null) { return; }

            var current = guideImageObject_view.LocalPosition;
            guideImageObject_view.SetLocalPosition(new Vector3(value, current.y, current.z));
            UpdateGuideImageView();
        }

        void OnGuideImageYChanged(float value)
        {
            if (guideImageObject_view == null) { return; }

            var current = guideImageObject_view.LocalPosition;
            guideImageObject_view.SetLocalPosition(new Vector3(current.x, value, current.z));
            UpdateGuideImageView();
        }

        void OnGuideImageScaleChanged(float value)
        {
            if (guideImageObject_view == null) { return; }

            guideImageObject_view.SetScale(value);
            UpdateGuideImageView();
        }

        void OnGuideImageRotationChanged(float value)
        {
            if (guideImageObject_view == null) { return; }

            guideImageObject_view.SetRotation(value);
            UpdateGuideImageView();
        }

        void OnGuideImageAlphaChanged(float value)
        {
            if (guideImageObject_view == null) { return; }

            guideImageObject_view.SetAlpha(value);
            UpdateGuideImageView();
        }

        void UpdateGuideImageView()
        {
            if (guideImagePanelView_view == null) { return; }
            if (guideImageObject_view == null) { return; }

            SetGuideImagePanelEnabled(true);
            guideImagePanelView_view.SetData(
                guideImageObject_view.LocalPosition,
                guideImageObject_view.Scale,
                guideImageObject_view.RotationZ,
                guideImageObject_view.Alpha,
                decimalDigits);
        }

        void UpdateTextGuideView()
        {
            if (textGuidePanelView_view == null || textGuideObject_view == null) { return; }

            SetTextGuidePanelEnabled(true);
            textGuidePanelView_view.SetEnabledState(textGuideObject_view.IsEnabled);
            textGuidePanelView_view.SetData(
                textGuideObject_view.Text,
                textGuideObject_view.LocalPosition,
                textGuideObject_view.Scale,
                textGuideObject_view.RotationZ,
                textGuideObject_view.Alpha,
                decimalDigits);
        }

        void CancelGuideImageLoading()
        {
            if (guideImageLoadCts == null) { return; }

            guideImageLoadCts.Cancel();
            guideImageLoadCts.Dispose();
            guideImageLoadCts = null;
        }

    }
}
