using UniRx;
using UnityEngine;
using VContainer;
using static UndoRedo.Vertices.VerticesMoveRecord;

namespace ChartEditor
{
    public class VertexDetailPresenter : MonoBehaviour
    {
        [SerializeField] MultiVertexSelector vertexSelector_model;
        [SerializeField] VertexDetailViewportView vertexDetailViewport_view;
        [SerializeField] VertexPositionPanelView positionPanelView_view;
        [SerializeField, Min(0)] int decimalDigits = 3;

        readonly CompositeDisposable disposables = new CompositeDisposable();
        readonly CompositeDisposable selectedVertexDisposables = new CompositeDisposable();

        IChartEditorDataGetter dataGetter_model; 
        VertexData selectedVertex;
        Vector2 editingStartPosition;
        bool isEditing;

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
            CommitCurrentEdit();

            if (positionPanelView_view != null)
            {
                positionPanelView_view.OnBeginEditListener -= BeginEdit;
                positionPanelView_view.OnEndEditListener -= CommitCurrentEdit;
                positionPanelView_view.OnXValueChangedListener -= OnXValueChanged;
                positionPanelView_view.OnYValueChangedListener -= OnYValueChanged;
            }

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
                positionPanelView_view.OnBeginEditListener += BeginEdit;
                positionPanelView_view.OnEndEditListener += CommitCurrentEdit;
                positionPanelView_view.OnXValueChangedListener += OnXValueChanged;
                positionPanelView_view.OnYValueChangedListener += OnYValueChanged;
            }
        }

        void RefreshSelectionState()
        {
            CommitCurrentEdit();

            if (vertexSelector_model == null) { return; }
            if (positionPanelView_view == null) { return; }

            if (vertexSelector_model.SelectingVertices.Count != 1)
            {
                ClearSelectedVertex();
                SetPanelEnabled(false);
                return;
            }

            var nextVertex = vertexSelector_model.SelectingVertices[0];
            if (selectedVertex == nextVertex)
            {
                SetPanelEnabled(true);
                UpdateView(nextVertex.Position.Value);
                return;
            }

            ClearSelectedVertex();
            selectedVertex = nextVertex;
            SetPanelEnabled(true);
            UpdateView(selectedVertex.Position.Value);

            selectedVertex.Position
                .Subscribe(position =>
                {
                    if (isEditing) { return; }
                    UpdateView(position);
                })
                .AddTo(selectedVertexDisposables);
        }

        void ClearSelectedVertex()
        {
            selectedVertexDisposables.Clear();
            selectedVertex = null;
            isEditing = false;
        }

        void SetPanelEnabled(bool enabled)
        {
            positionPanelView_view.SetInteractable(enabled);
            if (!enabled)
            {
                positionPanelView_view.Clear();
            }
        }

        void UpdateView(Vector2 position)
        {
            positionPanelView_view.SetPosition(position, decimalDigits);
        }

        void BeginEdit()
        {
            if (selectedVertex == null || isEditing) { return; }

            editingStartPosition = selectedVertex.Position.Value;
            isEditing = true;
        }

        void OnXValueChanged(float value)
        {
            if (selectedVertex == null) { return; }
            if (!isEditing) { BeginEdit(); }

            var current = selectedVertex.Position.Value;
            selectedVertex.SetPosition(new Vector2(value, current.y));
        }

        void OnYValueChanged(float value)
        {
            if (selectedVertex == null) { return; }
            if (!isEditing) { BeginEdit(); }

            var current = selectedVertex.Position.Value;
            selectedVertex.SetPosition(new Vector2(current.x, value));
        }

        void CommitCurrentEdit()
        {
            if (selectedVertex == null) { return; }
            if (!isEditing) { return; }

            var currentPosition = selectedVertex.Position.Value;
            isEditing = false;

            if (currentPosition != editingStartPosition)
            {
                RecordVertexMoving(selectedVertex, editingStartPosition, currentPosition);
            }

            UpdateView(currentPosition);
        }
    }
}
