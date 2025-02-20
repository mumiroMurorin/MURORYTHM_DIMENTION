using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;
using VContainer;
using System;
using UniRx;

namespace Refactoring
{
    public class ChartGenerator : MonoBehaviour, IChartGenerator
    {
        [Header("���ꂼ���NoteFactory")]
        [SerializeField] NoteFactory<NoteData_Touch> touchNoteFactory;
        [SerializeField] NoteFactory<NoteData_DynamicGroundUpward> dynamicGroundUpwardNoteFactory;
        [SerializeField] NoteFactory<NoteData_DynamicGroundRightward> dynamicGroundRightwardNoteFactory;
        [SerializeField] NoteFactory<NoteData_DynamicGroundLeftward> dynamicGroundLeftwardNoteFactory;
        [SerializeField] NoteFactory<NoteData_DynamicGroundDownward> dynamicGroundDownwardNoteFactory;
        [SerializeField] NoteFactory<NoteData_HoldStart> holdStartNoteFactory;
        [SerializeField] NoteFactory<NoteData_HoldRelay> holdRelayNoteFactory;
        [SerializeField] NoteFactory<NoteData_HoldEnd> holdEndNoteFactory;
        [SerializeField] NoteFactory<NoteData_HoldMesh> holdMeshNoteFactory;
        [SerializeField] NoteFactory<NoteData_HoldMeshSuper> holdMeshSuperNoteFactory;

        [Header("Factory�̏������ɕK�v�ȃf�[�^")]
        [SerializeField] GameObject groundObject;
        [SerializeField] Deformer groundDeformer;
        [SerializeField] SerializeInterface<ITimeGetter> timer;

        IChartDataGetter chartDataGetter;
        INoteSpawnDataOptionHolder spawnDataOptionHolder;
        ISliderInputGetter sliderInputGetter;
        ISpaceInputGetter spaceInputGetter;
        IJudgementRecorder judgementRecorder;

        private void Awake()
        {
            Initialize();
        }

        [Inject]
        public void Constructor(IChartDataGetter chartDataGetter, INoteSpawnDataOptionHolder optionHolder, IJudgementRecorder judgementRecorder,
            ISliderInputGetter sliderInputGetter, ISpaceInputGetter spaceInputGetter)
        {
            this.chartDataGetter = chartDataGetter;
            this.spawnDataOptionHolder = optionHolder;
            this.sliderInputGetter = sliderInputGetter;
            this.spaceInputGetter = spaceInputGetter;
            this.judgementRecorder = judgementRecorder;
            
        }

        /// <summary>
        /// ������
        /// </summary>
        private void Initialize()
        {
            // �������f�[�^�̐���
            NoteFactoryInitializingData data = new NoteFactoryInitializingData
            {
                GroundObject = this.groundObject,
                GroundDeformer = this.groundDeformer,
                OptionHolder = this.spawnDataOptionHolder,
                SliderInputGetter = this.sliderInputGetter,
                SpaceInputGetter = this.spaceInputGetter,
                Timer = this.timer.Value,
                JudgementRecorder = this.judgementRecorder
            };

            touchNoteFactory.Initialize(data);
            dynamicGroundUpwardNoteFactory.Initialize(data);
            dynamicGroundRightwardNoteFactory.Initialize(data);
            dynamicGroundLeftwardNoteFactory.Initialize(data);
            dynamicGroundDownwardNoteFactory.Initialize(data);
            holdStartNoteFactory.Initialize(data);
            holdRelayNoteFactory.Initialize(data);
            holdEndNoteFactory.Initialize(data);
            holdMeshNoteFactory.Initialize(data);
            holdMeshSuperNoteFactory.Initialize(data);
        }

        /// <summary>
        /// �m�[�c�S�̂̐���
        /// </summary>
        /// <param name="chartData"></param>
        public void Generate(Action callback = null)
        {
            GenerateTouchNote(chartDataGetter.Chart.noteData_Touches);
            GenerateDynamicGroundUpwardNote(chartDataGetter.Chart.noteData_DynamicGroundUpwards);
            GenerateDynamicGroundRightwardNote(chartDataGetter.Chart.noteData_DynamicGroundRightwards);
            GenerateDynamicGroundLeftwardNote(chartDataGetter.Chart.noteData_DynamicGroundLeftwards);
            GenerateDynamicGroundDownwardNote(chartDataGetter.Chart.noteData_DynamicGroundDownwards);
            GenerateHoldStartNote(chartDataGetter.Chart.noteData_HoldStarts);
            GenerateHoldRelayNote(chartDataGetter.Chart.noteData_HoldRelays);
            GenerateHoldEndNote(chartDataGetter.Chart.noteData_HoldEnds);
            GenerateHoldMeshNote(chartDataGetter.Chart.noteData_HoldMeshes);
            GenerateHoldMeshSuperNote(chartDataGetter.Chart.noteData_HoldMeshesSuper);

            callback?.Invoke();
        }

        /// <summary>
        /// �^�b�`�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateTouchNote(List<NoteData_Touch> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_Touch data in noteDatas)
            {
                touchNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �_�C�i�~�b�N�O���E���h(��)�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateDynamicGroundUpwardNote(List<NoteData_DynamicGroundUpward> noteDatas)
        {
            if(noteDatas == null) { return; }

            foreach (NoteData_DynamicGroundUpward data in noteDatas)
            {
                dynamicGroundUpwardNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �_�C�i�~�b�N�O���E���h(��)�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateDynamicGroundRightwardNote(List<NoteData_DynamicGroundRightward> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_DynamicGroundRightward data in noteDatas)
            {
                dynamicGroundRightwardNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �_�C�i�~�b�N�O���E���h(��)�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateDynamicGroundLeftwardNote(List<NoteData_DynamicGroundLeftward> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_DynamicGroundLeftward data in noteDatas)
            {
                dynamicGroundLeftwardNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �_�C�i�~�b�N�O���E���h(��)�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateDynamicGroundDownwardNote(List<NoteData_DynamicGroundDownward> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_DynamicGroundDownward data in noteDatas)
            {
                dynamicGroundDownwardNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �z�[���h�m�[�c�n�_�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateHoldStartNote(List<NoteData_HoldStart> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_HoldStart data in noteDatas)
            {
                holdStartNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �z�[���h�m�[�c���p�_�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateHoldRelayNote(List<NoteData_HoldRelay> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_HoldRelay data in noteDatas)
            {
                holdRelayNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �z�[���h�m�[�c�I�_�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateHoldEndNote(List<NoteData_HoldEnd> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_HoldEnd data in noteDatas)
            {
                holdEndNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �z�[���h���b�V���̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateHoldMeshNote(List<NoteData_HoldMesh> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_HoldMesh data in noteDatas)
            {
                holdMeshNoteFactory.Spawn(data);
            }
        }

        /// <summary>
        /// �z�[���h���b�V���̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateHoldMeshSuperNote(List<NoteData_HoldMeshSuper> noteDatas)
        {
            if (noteDatas == null) { return; }

            foreach (NoteData_HoldMeshSuper data in noteDatas)
            {
                holdMeshSuperNoteFactory.Spawn(data);
            }
        }
    }
}