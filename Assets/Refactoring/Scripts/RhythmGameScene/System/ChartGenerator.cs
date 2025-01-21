using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;
using VContainer;

namespace Refactoring
{
    public class ChartGenerator : MonoBehaviour, IChartGenerator
    {
        [Header("���ꂼ���NoteFactory")]
        [SerializeField] NoteFactory<NoteData_Touch> touchNoteFactory;

        [Header("Factory�̏������ɕK�v�ȃf�[�^")]
        [SerializeField] GameObject groundObject;
        [SerializeField] Deformer groundDeformer;
        [SerializeField] SerializeInterface<ITimeGetter> timer;

        INoteSpawnDataOptionHolder spawnDataOptionHolder;
        ISliderInputGetter sliderInputGetter;
        ISpaceInputGetter spaceInputGetter;
        IJudgementRecorder judgementRecorder;

        private void Start()
        {
            // ��
            ChartData data = new ChartData
            {
                noteData_Touches = new List<NoteData_Touch>
                 {
                      new NoteData_Touch
                      {
                           Range = new int[4] {0,1,2,3 },
                           Timing = 1f,
                      },
                      new NoteData_Touch
                      {
                           Range = new int[5] {4,5,6,7,8 },
                           Timing = 2f,
                      },
                      new NoteData_Touch
                      {
                           Range = new int[6] {9,10,11,12,13,14 },
                           Timing = 3f,
                      }
                 }
            };

            Initialize();



            Generate(data);
        }

        [Inject]
        public void Constructor(INoteSpawnDataOptionHolder optionHolder, IJudgementRecorder judgementRecorder, 
            ISliderInputGetter sliderInputGetter, ISpaceInputGetter spaceInputGetter)
        {
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
        }

        /// <summary>
        /// �m�[�c�S�̂̐���
        /// </summary>
        /// <param name="chartData"></param>
        public void Generate(ChartData chartData)
        {
            GenerateTouchNote(chartData.noteData_Touches);
        }

        /// <summary>
        /// �^�b�`�m�[�c�̐���
        /// </summary>
        /// <param name="noteData_Touches"></param>
        private void GenerateTouchNote(List<NoteData_Touch> noteData_Touches)
        {
            foreach(NoteData_Touch data in noteData_Touches)
            {
                touchNoteFactory.Spawn(data);
            }
        }
    }

}
