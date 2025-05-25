using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{

    /// <summary>
    /// スペースホールド中継点
    /// </summary>
    public class SpaceHoldRelayConverter : IOriginDataToRhythmGameConvertable
    {
        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = (Vector2[])noteOrigin.Vertices.Clone(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールドメッシュ
    /// </summary>
    public class SpaceHoldMeshConverter : IOriginDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToVertices>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToVertices>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            AddHoldRelayData(dataOrigin, timing);
            AddHoldStartData(dataOrigin, timing);
            AddHoldMeshRelayData(dataOrigin, timing);
            AddHoldHiddenJudgedRelay(dataOrigin, timing);
            AddHoldEndData(dataOrigin, chartData, timing);

            return true;
        }

        private bool AddHoldStartData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin.SpaceHoldStartData)
            {
                // 一度ディクショナリーに格納
                // ディクショナリーに登録されていなければ新規作成
                if (numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList = new List<TimeToVertices>();
                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.ToArray(), Timing = timing });
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            return true;
        }

        private bool AddHoldRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldMeshRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldHiddenJudgedRelay(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldHiddenJudgedRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldHiddenJudgedRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.ToArray(), Timing = timing });
            }

            return true;
        }

        private bool AddHoldEndData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.TryGetValue(noteOrigin.HoldNumber, out var meshList))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.ToArray(), Timing = timing });
                chartData.AddNoteData(GenerateNoteData_SpaceHoldMesh(meshList));
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → NoteData_HoldMesh
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private NoteData_SpaceHoldMesh GenerateNoteData_SpaceHoldMesh(List<TimeToVertices> timeToVertices)
        {
            var noteData = new NoteData_SpaceHoldMesh();

            noteData.Timing = timeToVertices[0].Timing;
            noteData.TimeToVertices = timeToVertices;

            return noteData;
        }
    }
}