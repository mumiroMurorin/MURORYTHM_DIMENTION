using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Linq;
using VContainer;
using System;

namespace Refactoring
{
    public class ChartLoaderDebug : MonoBehaviour, IChartLoader
    {
        IMusicDataGetter musicDataGetter;
        IChartDataSetter chartDataSetter;

        [Inject]
        public void Constructor(IMusicDataGetter musicDataGetter)
        {
            this.musicDataGetter = musicDataGetter;
        }

        [Inject]
        public void Constructor(IChartDataSetter chartDataSetter)
        {
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
        /// データを読み込む
        /// </summary>
        /// <param name="textAsset"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        public ChartData LoadChartData()
        {
            ChartData chartData = new ChartData
            {
                MaxCombo = 150,

                noteData_Touches = new List<NoteData_Touch>
                {
                      
                },

                noteData_HoldStarts = new List<NoteData_HoldStart>
                {

                },

                noteData_HoldRelays = new List<NoteData_HoldRelay>
                {

                },

                noteData_HoldMeshes = new List<NoteData_HoldMesh>
                {

                },

                noteData_HoldEnds = new List<NoteData_HoldEnd>
                {

                },

                noteData_DynamicGroundUpwards = new List<NoteData_DynamicGroundUpward>
                {

                },

                noteData_DynamicGroundDownwards = new List<NoteData_DynamicGroundDownward>
                {

                },

                noteData_DynamicGroundRightwards = new List<NoteData_DynamicGroundRightward>
                {

                },

                noteData_DynamicGroundLeftwards = new List<NoteData_DynamicGroundLeftward>
                {

                },

                noteData_SpaceHoldRelays = new List<NoteData_SpaceHoldRelay>
                {
                    
                },

                noteData_SpaceHoldMeshes = new List<NoteData_SpaceHoldMesh>
                {
                    
                }
            };

            float interval = 0.025f;
            float timing = 2f;

            /*
            NoteData_SpaceHoldMesh spaceHoldMesh = new NoteData_SpaceHoldMesh();
            spaceHoldMesh.Timing = 2f;
            spaceHoldMesh.TimeToVertices = new List<TimeToVertices>();
            chartData.noteData_SpaceHoldMeshes.Add(spaceHoldMesh);

            for (int j = 0; j < 5; j++)
            {
                // ○形
                for (int i = 0; i < 60; i++)
                {
                    Vector2[] vertices = new Vector2[32];

                    for (int k = 0; k < 32; k++)
                    {
                        float angle = (2 * Mathf.PI * k) / 32;  // 角度を計算
                        vertices[k] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                    }

                    float radian = i * 15 * Mathf.Deg2Rad;
                    vertices = vertices.Select(v => new Vector2(v.x * 0.3f + Mathf.Sin(radian) * 0.35f, v.y * 0.3f + Mathf.Cos(radian) * 0.35f) - new Vector2(0, 0.4f)).ToArray();

                    spaceHoldMesh.TimeToVertices.Add(new TimeToVertices { Timing = timing, Vertices = vertices });
                    if (i % 3 == 0) { chartData.noteData_SpaceHoldRelays.Add(new NoteData_SpaceHoldRelay { Timing = timing, Vertices = vertices }); }

                    timing += interval;
                }

                // △形
                for (int i = 0; i < 60; i++)
                {
                    Vector2[] vertices = new Vector2[]
                    {
                    new Vector2( 0.00f,  1.00f),
                    new Vector2( 1.00f, -1.00f),
                    new Vector2(-1.00f, -1.00f),
                    };

                    float radian = i * 15 * Mathf.Deg2Rad;
                    vertices = vertices.Select(v => new Vector2(v.x * 0.3f + Mathf.Sin(radian) * 0.35f, v.y * 0.3f + Mathf.Cos(radian) * 0.35f) - new Vector2(0, 0.4f)).ToArray();

                    spaceHoldMesh.TimeToVertices.Add(new TimeToVertices { Timing = timing, Vertices = vertices });
                    if (i % 3 == 0) { chartData.noteData_SpaceHoldRelays.Add(new NoteData_SpaceHoldRelay { Timing = timing, Vertices = vertices }); }

                    timing += interval;
                }

                // □形
                for (int i = 0; i < 60; i++)
                {
                    Vector2[] vertices = new Vector2[]
                    {
                    new Vector2(-1.00f, -1.00f),
                    new Vector2(-1.00f,  1.00f),
                    new Vector2( 1.00f,  1.00f),
                    new Vector2( 1.00f, -1.00f),
                    };

                    float radian = i * 15 * Mathf.Deg2Rad;
                    vertices = vertices.Select(v => new Vector2(v.x * 0.3f + Mathf.Sin(radian) * 0.35f, v.y * 0.3f + Mathf.Cos(radian) * 0.35f) - new Vector2(0, 0.4f)).ToArray();

                    spaceHoldMesh.TimeToVertices.Add(new TimeToVertices { Timing = timing, Vertices = vertices });
                    if (i % 3 == 0) { chartData.noteData_SpaceHoldRelays.Add(new NoteData_SpaceHoldRelay { Timing = timing, Vertices = vertices }); }

                    timing += interval;
                }

                // 星形
                for (int i = 0; i < 60; i++)
                {
                    Vector2[] vertices = new Vector2[]
                    {
                    new Vector2(0.00f, 1.00f),
                    new Vector2(0.31f, 0.38f),
                    new Vector2(0.95f, 0.31f),
                    new Vector2(0.50f, -0.16f),
                    new Vector2(0.59f, -0.81f),
                    new Vector2(0.00f, -0.50f),
                    new Vector2(-0.59f, -0.81f),
                    new Vector2(-0.50f, -0.16f),
                    new Vector2(-0.95f, 0.31f),
                    new Vector2(-0.31f, 0.38f),
                    };

                    float radian = i * 15 * Mathf.Deg2Rad;
                    vertices = vertices.Select(v => new Vector2(v.x * 0.3f + Mathf.Sin(radian) * 0.35f, v.y * 0.3f + Mathf.Cos(radian) * 0.35f) - new Vector2(0, 0.4f)).ToArray();

                    spaceHoldMesh.TimeToVertices.Add(new TimeToVertices { Timing = timing, Vertices = vertices });
                    if (i % 3 == 0) { chartData.noteData_SpaceHoldRelays.Add(new NoteData_SpaceHoldRelay { Timing = timing, Vertices = vertices }); }

                    timing += interval;
                }

                // ハート形
                for (int i = 0; i < 60; i++)
                {
                    Vector2[] vertices = new Vector2[]
                    {
                    new Vector2( 0.00f,  0.20f),  // 上部の中央くぼみ

                    new Vector2( 0.10f,  0.35f),  // 右上の丸い部分
                    new Vector2( 0.20f,  0.60f),
                    new Vector2( 0.50f,  0.80f),
                    new Vector2( 0.70f,  0.60f),
                    new Vector2( 0.85f,  0.35f),
                    new Vector2( 0.80f,  0.10f),  // 右下カーブ  

                    new Vector2( 0.60f, -0.10f),
                    new Vector2( 0.30f, -0.30f),
                    new Vector2( 0.10f, -0.60f),

                    new Vector2( 0.00f, -0.80f),  // ハートの下の尖った部分
                    new Vector2(-0.10f, -0.60f),
                    new Vector2(-0.30f, -0.30f),
                    new Vector2(-0.60f, -0.10f),  // 左下カーブ  

                    new Vector2(-0.80f,  0.10f),  // 左上の丸い部分
                    new Vector2(-0.85f,  0.35f),
                    new Vector2(-0.70f,  0.60f),
                    new Vector2(-0.50f,  0.80f),
                    new Vector2(-0.20f,  0.60f),
                    new Vector2(-0.10f,  0.35f),
                    };

                    float radian = i * 15 * Mathf.Deg2Rad;
                    vertices = vertices.Select(v => new Vector2(v.x * 0.3f + Mathf.Sin(radian) * 0.35f, v.y * 0.3f + Mathf.Cos(radian) * 0.35f) - new Vector2(0, 0.4f)).ToArray();

                    spaceHoldMesh.TimeToVertices.Add(new TimeToVertices { Timing = timing, Vertices = vertices });
                    if (i % 3 == 0) { chartData.noteData_SpaceHoldRelays.Add(new NoteData_SpaceHoldRelay { Timing = timing, Vertices = vertices }); }
                    timing += interval;
                }

            }
            */

            // ダイナミックノーツ、タッチノーツ
            timing = 2f;
            int before = -1;
            for (int i = 0; i < 600; i++)
            {
                switch (i % 4)
                {
                    case 0:
                        System.Random rand = new System.Random();

                        int start;
                        do
                        {
                            start = rand.Next(0, 4) * 2 + 4;
                        } while (before == start);
                        before = start;

                        int[] rangeArray = Enumerable.Range(start, 2).ToArray();
                        chartData.noteData_Touches.Add(new NoteData_Touch { Timing = timing, Range = rangeArray });
                        break;
                }

                timing += interval;
            }

                //// ホールドノーツ
                //timing = 2f;

                //NoteData_HoldMesh holdMesh = new NoteData_HoldMesh();
                //chartData.noteData_HoldMeshes.Add(holdMesh);
                //holdMesh.Timing = 2f;
                //holdMesh.TimeToRanges = new List<TimeToRange>();

                //holdMesh.TimeToRanges.Add(new TimeToRange { Timing = timing, Range = new float[] { 6, 7, 8, 9 } });
                //chartData.noteData_HoldStarts.Add(new NoteData_HoldStart { Timing = timing, Range = new int[] { 6, 7, 8, 9 } });
                //timing += interval;

                //for (int i = 1; i < 150; i++)
                //{
                //    if(i % 4 == 0)
                //    {
                //        System.Random rand = new System.Random();

                //        int start = rand.Next(0, 16);
                //        int end = rand.Next(start, 16);
                //        float[] rangeArray = Enumerable.Range(start, end - start + 1).Select(r => (float)r).ToArray();

                //        holdMesh.TimeToRanges.Add(new TimeToRange { Timing = timing, Range = rangeArray });

                //        if (i % 16 == 0)
                //        {
                //            chartData.noteData_HoldRelays.Add(new NoteData_HoldRelay { Timing = timing, Range = rangeArray.Select(r => (int)r).ToArray() });
                //        }
                //    }

                //    timing += interval;
                //}

                return chartData;
        }
    }
}
