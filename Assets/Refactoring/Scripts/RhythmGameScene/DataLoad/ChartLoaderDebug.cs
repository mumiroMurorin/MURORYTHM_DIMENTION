using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using VContainer;
using System;

namespace Refactoring
{
    public class ChartLoaderDebug : MonoBehaviour, IChartLoader
    {
        IMusicDataGetter musicDataGetter;
        IChartDataSetter chartDataSetter;

        [Inject]
        public void Constructor(IMusicDataGetter musicDataGetter, IChartDataSetter chartDataSetter)
        {
            this.musicDataGetter = musicDataGetter;
            this.chartDataSetter = chartDataSetter;
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            
        }

        void IChartLoader.LoadChart(Action callback)
        {
            
            chartDataSetter.SetChartData(LoadChartData());
            callback.Invoke();
        }

        /// <summary>
        /// ÉfÅ[É^Çì«Ç›çûÇﬁ
        /// </summary>
        /// <param name="textAsset"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ChartData LoadChartData()
        {
            ChartData chartData = new ChartData
            {
                noteData_Touches = new List<NoteData_Touch>
                 {
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 0,1,2,3 },
                           Timing = 1f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 0,1,2,3 },
                           Timing = 2f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 4,5,6,7,8,9,10,11,12 },
                           Timing = 3f
                      }
                 }
            };

            return chartData;
        }
    }
}
