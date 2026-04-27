using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;
using static MeshGenerate.SpaceHoldMeshGenerator;

namespace ChartConvert
{
    /// <summary>
    /// スペースホールド始点(中継点データに変換)
    /// </summary>
    public class SpaceHoldStartConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToVertices>> holdNumberToVertices)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldStartData)
            {
                if (holdNumberToVertices.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】SpaceHoldStartの変換の際、既にHoldNumberが存在しました: {noteOrigin.HoldNumber}");
                    return false;
                }

                var timeToVertices = new List<TimeToVertices>() { new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()) };
                holdNumberToVertices.Add(noteOrigin.HoldNumber, timeToVertices);

                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    TimeToVertices = timeToVertices,
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldStart) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldStartData == null)
            {
                dataOrigin.SpaceHoldStartData = new List<NoteDataOrigin_SpaceHoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldStart data = new NoteDataOrigin_SpaceHoldStart()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldStartData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldStartData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.SpaceHoldStart);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールド中継点
    /// </summary>
    public class SpaceHoldRelayConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToVertices>> holdNumberToVertices)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                if (!holdNumberToVertices.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】SpaceHoldRelayの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                holdNumberToVertices[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));

                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    TimeToVertices = holdNumberToVertices[noteOrigin.HoldNumber],
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldRelay) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldRelayData == null)
            {
                dataOrigin.SpaceHoldRelayData = new List<NoteDataOrigin_SpaceHoldRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldRelay data = new NoteDataOrigin_SpaceHoldRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.SpaceHoldRelay);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールドメッシュ中継ノーツ
    /// </summary>
    public class SpaceHoldMeshRelayConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToVertices>> holdNumberToVertices)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                if (!holdNumberToVertices.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】SpaceHoldMeshRelayの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                holdNumberToVertices[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldMeshRelay) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (backNote == null || nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldMeshRelayData == null)
            {
                dataOrigin.SpaceHoldMeshRelayData = new List<NoteDataOrigin_SpaceHoldMeshRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldMeshRelay data = new NoteDataOrigin_SpaceHoldMeshRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldMeshRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                typeChangableData.SetNoteType(DeploymentNoteType.SpaceHoldMeshRelay);
                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールド終点(中継点データに変換)
    /// </summary>
    public class SpaceHoldEndConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        const int MESH_DIVISION_NUM = 10;
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing, Dictionary<int, List<TimeToVertices>> holdNumberToVertices)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                if (!holdNumberToVertices.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】SpaceHoldRelayの変換の際、ノーツが見つかりませんでした: {noteOrigin.HoldNumber}");
                    return false;
                }

                holdNumberToVertices[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));

                VertexCountNormalizer(holdNumberToVertices[noteOrigin.HoldNumber], MESH_DIVISION_NUM);

                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(),
                    TimeToVertices = holdNumberToVertices[noteOrigin.HoldNumber],
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldEnd) { return false; }
            if (noteDataInEditor is not IChainNoteData chainData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }
            
            // 前後ノーツチェック
            var backNote = chainData.NoteObject.BackNote.Value;
            var nextNote = chainData.NoteObject.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldEndData == null)
            {
                dataOrigin.SpaceHoldEndData = new List<NoteDataOrigin_SpaceHoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldEnd data = new NoteDataOrigin_SpaceHoldEnd()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = chainData.ChainIndex.Value
            };

            dataOrigin.SpaceHoldEndData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldEndData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData chainData) { return false; }
                if (noteData is not ITypeChangableNoteData typeChangableData) { return false; }
                if (noteData is not IVerticesControlableNoteData verticesData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarData.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);

                chainData.SetChainIndex(noteDataOrigin.HoldNumber);
                noteData.SetAddress(address);
                typeChangableData.SetNoteType(DeploymentNoteType.SpaceHoldEnd);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                onAddNoteData(noteData);
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールドメッシュ
    /// </summary>
    public class SpaceHoldMeshConverter : IUnchainDataToRhythmGameConvertable
    {
        const int MESH_DIVISION_NUM = 10;
        Dictionary<int, List<TimeToVertices>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToVertices>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            AddHoldStartData(dataOrigin, timing);
            AddHoldRelayData(dataOrigin, timing);
            AddHoldMeshRelayData(dataOrigin, timing);
            AddHoldEndData(dataOrigin, onAddNoteData, timing);

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
                if (numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                var meshList = new List<TimeToVertices>();
                meshList.Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));
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
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));
            }

            return true;
        }

        private bool AddHoldMeshRelayData(SubDivisionDataOrigin dataOrigin, float timing)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));
            }

            return true;
        }

        private bool AddHoldEndData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToVertices(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray()));
                onAddNoteData(GenerateNoteData_SpaceHoldMesh(numberToHoldMeshDataOrigin[noteOrigin.HoldNumber]));
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
            VertexCountNormalizer(timeToVertices, MESH_DIVISION_NUM);

            noteData.Timing = timeToVertices[0].Timing;
            noteData.TimeToVertices = timeToVertices;

            return noteData;
        }
    }

    /// <summary>
    /// スペースホールド判定点
    /// </summary>
    public class SpaceHoldJudgementPointConverter : IUnchainDataToRhythmGameConvertable
    {
        const int MESH_DIVISION_NUM = 10;

        Dictionary<int, List<TimeToDetail>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToDetail>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            AddHoldStartData(dataOrigin.SpaceHoldStartData, dataOrigin.Bpm, timing);
            AddHoldRelayData(dataOrigin.SpaceHoldRelayData, dataOrigin.Bpm, timing);
            AddHoldMeshRelayData(dataOrigin.SpaceHoldMeshRelayData, dataOrigin.Bpm, timing);
            AddHoldEndData(dataOrigin.SpaceHoldEndData, onAddNoteData, dataOrigin.Bpm, timing);

            return true;
        }

        private bool AddHoldStartData(List<NoteDataOrigin_SpaceHoldStart> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            // 始点、メッシュデータ格納リストの作成
            foreach (var noteOrigin in dataOrigin)
            {
                // 一度ディクショナリーに格納
                // ディクショナリーに登録されていなければ新規作成
                if (numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが既に登録されています: {noteOrigin.HoldNumber}");
                    return false;
                }

                var meshList = new List<TimeToDetail>();
                meshList.Add(new TimeToDetail(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), true, bpm));
                numberToHoldMeshDataOrigin.Add(noteOrigin.HoldNumber, meshList);
            }

            return true;
        }

        private bool AddHoldRelayData(List<NoteDataOrigin_SpaceHoldRelay> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToDetail(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), true, bpm));
            }

            return true;
        }

        private bool AddHoldMeshRelayData(List<NoteDataOrigin_SpaceHoldMeshRelay> dataOrigin, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されてたらエラー
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToDetail(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), false, bpm));
            }

            return true;
        }

        private bool AddHoldEndData(List<NoteDataOrigin_SpaceHoldEnd> dataOrigin, Action<INoteData> onAddNoteData, float bpm, float timing)
        {
            if (dataOrigin == null) { return true; }

            // 終点、メッシュデータ格納リストにデータを追加後、譜面データに代入
            foreach (var noteOrigin in dataOrigin)
            {
                // ディクショナリーに登録されていなければ返す
                if (!numberToHoldMeshDataOrigin.ContainsKey(noteOrigin.HoldNumber))
                {
                    Debug.LogWarning($"【Converter】始点データが登録されていません: {noteOrigin.HoldNumber}");
                    return false;
                }

                numberToHoldMeshDataOrigin[noteOrigin.HoldNumber].Add(new TimeToDetail(timing, noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), true, bpm));
                foreach(var note in GenerateNoteData_JudgementPoint(numberToHoldMeshDataOrigin[noteOrigin.HoldNumber])) 
                {
                    onAddNoteData(note);
                }
            }

            return true;
        }

        /// <summary>
        /// List＜HoldMeshOriginAndTiming＞ → List<NoteData_SpaceHoldRelayHidden>
        /// </summary>
        /// <param name="meshDataList"></param>
        /// <returns></returns>
        private List<NoteData_SpaceHoldRelayHidden> GenerateNoteData_JudgementPoint(List<TimeToDetail> timeToDetails)
        {
            var noteDatas = new List<NoteData_SpaceHoldRelayHidden>();

            var timeToVertices = timeToDetails.Select(x => x.ToTimeToVertices()).ToList();
            float interval = CalcInterval(timeToDetails[0].Bpm);
            float mergin = interval / 2f;
            int index = 0;

            VertexCountNormalizer(timeToVertices, MESH_DIVISION_NUM);

            for (float count = timeToDetails[0].Timing; count < timeToDetails[^1].Timing; count += interval)
            {
                var detail = timeToDetails[index];

                // インターバルの更新
                while (index + 1 < timeToDetails.Count && timeToDetails[index + 1].Timing < count)
                {
                    index++;
                    interval = CalcInterval(detail.Bpm);
                    mergin = interval / 2f;
                }

                // 近くに判定点があったら追加しない
                if (IsNearJudgementPoint(timeToDetails, count, mergin)) { continue; }

                // 判定点の追加
                noteDatas.Add(ConvertNoteData(timeToVertices, count));
            }

            return noteDatas;
        }

        private NoteData_SpaceHoldRelayHidden ConvertNoteData(List<TimeToVertices> timeToVertices, float time)
        {
            var noteData = new NoteData_SpaceHoldRelayHidden();

            Vector2[] backVertices = new Vector2[0];
            Vector2[] nextVertices = new Vector2[0];
            float t = 0;

            for(int i = 1; i < timeToVertices.Count; i++)
            {
                if(timeToVertices[i].Timing < time) { continue; }
                
                backVertices = timeToVertices[i - 1].Vertices;
                nextVertices = timeToVertices[i].Vertices;

                t = (time - timeToVertices[i - 1].Timing) / (timeToVertices[i].Timing - timeToVertices[i - 1].Timing);
                break;
            }

            noteData.Timing = time;
            noteData.TimeToVertices = timeToVertices;
            noteData.Vertices = InterpolatePoints(backVertices.ToList(), nextVertices.ToList(), t, MESH_DIVISION_NUM).ToArray();

            return noteData;
        }

        private bool IsNearJudgementPoint(List<TimeToDetail> details, float timing, float margin)
        {
            foreach (var detail in details)
            {
                if (Mathf.Abs(detail.Timing - timing) < margin && detail.IsJudgement) { return true; }
            }

            return false;
        }

        private float CalcInterval(float bpm)
        {
            // 8分間隔
            return 30f / bpm;
        }


        private class TimeToDetail
        {
            public TimeToDetail(float timing, Vector2[] vertices, bool isJudgement, float bpm)
            {
                Timing = timing;
                Vertices = vertices;
                IsJudgement = isJudgement;
                Bpm = bpm;
            }

            public float Timing { get; set; }
            public Vector2[] Vertices { get; set; }
            public float Bpm { get; set; }
            public bool IsJudgement { get; set; }
            public TimeToVertices ToTimeToVertices()
            {
                return new TimeToVertices(this.Timing, this.Vertices);
            }
        }
    }

    /// <summary>
    /// スペースブレイク
    /// </summary>
    public class SpaceBreakConverter : IUnchainDataToRhythmGameConvertable, IUnchainedNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, Action<INoteData> onAddNoteData, float timing)
        {
            if (dataOrigin.SpaceBreakData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceBreakData)
            {
                var noteData = new NoteData_SpaceBreak
                {
                    Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(),
                    Timing = timing
                };

                onAddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceBreak) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData verticesData) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceBreakData == null)
            {
                dataOrigin.SpaceBreakData = new List<NoteDataOrigin_SpaceBreak>();
            }

            // 追加するデータのインスタンス化
            var data = new NoteDataOrigin_SpaceBreak()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
            };

            dataOrigin.SpaceBreakData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ISubDivisionDataGetter dataInChartEditor, Action<IDeployableNoteData> onAddNoteData)
        {
            // ※未実装
            return true;
        }

    }

}