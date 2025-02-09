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
                MaxCombo = 0,

                noteData_Touches = new List<NoteData_Touch>
                {
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 1f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 2f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 3f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 4f
                      },
                       new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 5f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 5.5f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 6f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 6.5f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 7f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 7.25f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 7.5f
                      },
                      new NoteData_Touch
                      {
                           Range =  new int[]{ 6,7,8,9 },
                           Timing = 7.75f
                      },
                },


                //noteData_HoldStarts = new List<NoteData_HoldStart>
                //{
                //      new NoteData_HoldStart
                //      {
                //           Range =  new int[]{ 0 },
                //           Timing = 1f
                //      },
                //      new NoteData_HoldStart
                //      {
                //           Range =  new int[]{ 0,1,2,3 },
                //           Timing = 2f
                //      },
                //      new NoteData_HoldStart
                //      {
                //           Range =  new int[]{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 },
                //           Timing = 3f
                //      }
                //},

                //noteData_HoldRelays = new List<NoteData_HoldRelay>
                //{
                //      new NoteData_HoldRelay
                //      {
                //           Range =  new int[]{ 0 },
                //           Timing = 1f
                //      },
                //      new NoteData_HoldRelay
                //      {
                //           Range =  new int[]{ 0,1,2,3 },
                //           Timing = 2f
                //      },
                //      new NoteData_HoldRelay
                //      {
                //           Range =  new int[]{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 },
                //           Timing = 3f
                //      }
                //},

                //noteData_HoldMeshes = new List<NoteData_HoldMesh>
                //{
                //      new NoteData_HoldMesh
                //      {
                //           StartRange =  new int[]{ 0,1,2,3 },
                //           EndRange =  new int[]{ 0,1,2,3 },
                //           Timing = 1f,
                //           EndTiming = 2f
                //      },
                //      new NoteData_HoldMesh
                //      {
                //           StartRange =  new int[]{ 4,5,6,7 },
                //           EndRange =  new int[]{ 7,8,9,10,11,12 },
                //           Timing = 2f,
                //           EndTiming = 3f
                //      },
                //      new NoteData_HoldMesh
                //      {
                //           StartRange =  new int[]{ 15 },
                //           EndRange =  new int[]{ 0,1 },
                //           Timing = 3f,
                //           EndTiming = 4f
                //      }
                //},

                //noteData_HoldEnds = new List<NoteData_HoldEnd>
                //{
                //      new NoteData_HoldEnd
                //      {
                //           Range =  new int[]{ 0 },
                //           Timing = 1f
                //      },
                //      new NoteData_HoldEnd
                //      {
                //           Range =  new int[]{ 0,1,2,3 },
                //           Timing = 2f
                //      },
                //      new NoteData_HoldEnd
                //      {
                //           Range =  new int[]{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15 },
                //           Timing = 3f
                //      }
                //},

                //noteData_DynamicGroundUpwards = new List<NoteData_DynamicGroundUpward>
                //{
                //      new NoteData_DynamicGroundUpward
                //      {
                //           Range =  new int[]{ 6,7,8,9 },
                //           Timing = 1f
                //      },
                //},

                //noteData_DynamicGroundDownwards = new List<NoteData_DynamicGroundDownward>
                //{
                //      new NoteData_DynamicGroundDownward
                //      {
                //           Range =  new int[]{ 6,7,8,9 },
                //           Timing = 2f
                //      },
                //},

                //noteData_DynamicGroundRightwards = new List<NoteData_DynamicGroundRightward>
                //{
                //      new NoteData_DynamicGroundRightward
                //      {
                //           Range =  new int[]{ 6,7,8,9 },
                //           Timing = 3f
                //      },
                //},

                //noteData_DynamicGroundLeftwards = new List<NoteData_DynamicGroundLeftward>
                //{
                //      new NoteData_DynamicGroundLeftward
                //      {
                //           Range =  new int[]{ 6,7,8,9 },
                //           Timing = 4f
                //      },
                //},
            };

            return chartData;
        }
    }
}
