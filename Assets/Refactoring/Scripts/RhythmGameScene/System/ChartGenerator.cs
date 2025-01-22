using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Deform;
using VContainer;
using System;

namespace Refactoring
{
    public class ChartGenerator : MonoBehaviour, IChartGenerator
    {
        [Header("それぞれのNoteFactory")]
        [SerializeField] NoteFactory<NoteData_Touch> touchNoteFactory;

        [Header("Factoryの初期化に必要なデータ")]
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
            Debug.Log(optionHolder);

        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void Initialize()
        {
            // 初期化データの生成
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

            Debug.Log(data.OptionHolder);
            touchNoteFactory.Initialize(data);
        }

        /// <summary>
        /// ノーツ全体の生成
        /// </summary>
        /// <param name="chartData"></param>
        public void Generate(Action callback = null)
        {
            GenerateTouchNote(chartDataGetter.Chart.noteData_Touches);

            callback?.Invoke();
        }

        /// <summary>
        /// タッチノーツの生成
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
