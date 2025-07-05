using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using ChartEditor;
using System;

namespace ChartConvert
{
    /// <summary>
    /// スペースホールド始点(中継点データに変換)
    /// </summary>
    public class SpaceHoldStartConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };
        int currentHoldNumber = 0;

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldStartData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin,  Dictionary<IChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData) { return false; }

            var verticesData = ((IVerticesControlableNoteData)noteDataInEditor);
            var backNote = ((IChainNoteData)noteDataInEditor).BackNote.Value;
            var nextNote = ((IChainNoteData)noteDataInEditor).NextNote.Value;
            if (backNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldStartData == null)
            {
                dataOrigin.SpaceHoldStartData = new List<NoteDataOrigin_SpaceHoldStart>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldStart data = new NoteDataOrigin_SpaceHoldStart()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = currentHoldNumber
            };

            dataOrigin.SpaceHoldStartData.Add(data);
            nextNoteToNumber.Add(nextNote, currentHoldNumber++);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,  Dictionary<int, IChainNoteData> numberToStartNote)
        {
            if (dataOrigin.SpaceHoldStartData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldStartData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData) { return false; }
                if (noteData is not IVerticesControlableNoteData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);
                var chainData = (IChainNoteData)noteData;
                var verticesData = (IVerticesControlableNoteData)noteData;

                // 頂点リストのセット
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                // その他情報のセット
                noteData.SetAddress(address);
                dataInChartEditor.AddNote(noteData);

                // ディクショナリーへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    Debug.LogWarning($"【Converter】SpaceHoldStartの変換の際、既にHoldNumberが存在しました: {noteDataOrigin.HoldNumber}");
                    return false;
                }
                else
                {
                    numberToStartNote.Add(noteDataOrigin.HoldNumber, chainData);
                }
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

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldRelayData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin,  Dictionary<IChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData) { return false; }
            if (noteDataInEditor is not ITypeChangableNoteData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData) { return false; }

            var thisNote = (IChainNoteData)noteDataInEditor;
            var backNote = thisNote.BackNote.Value;
            var nextNote = thisNote.NextNote.Value;
            var verticesData = ((IVerticesControlableNoteData)noteDataInEditor);
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】SpaceHoldRelayの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                nextNoteToNumber.Remove(thisNote);
                nextNoteToNumber.Add(nextNote, number);
            }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldRelayData == null) { dataOrigin.SpaceHoldRelayData = new List<NoteDataOrigin_SpaceHoldRelay>(); }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldRelay data = new NoteDataOrigin_SpaceHoldRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = number
            };

            dataOrigin.SpaceHoldRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,  Dictionary<int, IChainNoteData> numberToStartNote)
        {
            if (dataOrigin.SpaceHoldRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }
                if (noteData is not IVerticesControlableNoteData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);
                var chainData = (IChainNoteData)noteData;
                var verticesData = (IVerticesControlableNoteData)noteData;

                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.SpaceHold);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                noteData.SetAddress(address);
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData, false);
                }
                else
                {
                    Debug.LogWarning($"【Converter】SpaceHoldRelayの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
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

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            // 特に処理なし
            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin,  Dictionary<IChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldHidden) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData) { return false; }

            var verticesData = ((IVerticesControlableNoteData)noteDataInEditor);
            var thisNote = (IChainNoteData)noteDataInEditor;
            var backNote = thisNote.BackNote.Value;
            var nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】SpaceHoldHiddenの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                nextNoteToNumber.Remove(thisNote);
                nextNoteToNumber.Add(nextNote, number);
            }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldMeshRelayData == null)
            {
                dataOrigin.SpaceHoldMeshRelayData = new List<NoteDataOrigin_SpaceHoldMeshRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldMeshRelay data = new NoteDataOrigin_SpaceHoldMeshRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = number
            };

            dataOrigin.SpaceHoldMeshRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,  Dictionary<int, IChainNoteData> numberToStartNote)
        {
            if (dataOrigin.SpaceHoldMeshRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldMeshRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }
                if (noteData is not IVerticesControlableNoteData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);
                var chainData = (IChainNoteData)noteData;
                var verticesData = (IVerticesControlableNoteData)noteData;

                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.SpaceHoldHidden);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                noteData.SetAddress(address);
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData, false);
                }
                else
                {
                    Debug.LogWarning($"【Converter】SpaceHoldHiddenの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールド中継点
    /// </summary>
    public class SpaceHoldHiddenJudgedRelay : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldHiddenJudgedRelayData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldHiddenJudgedRelayData)
            {
                NoteData_SpaceHoldRelayHidden noteData = new NoteData_SpaceHoldRelayHidden
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin,  Dictionary<IChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHoldHiddenJudged) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData) { return false; }

            var verticesData = ((IVerticesControlableNoteData)noteDataInEditor);
            var thisNote = (IChainNoteData)noteDataInEditor;
            var backNote = thisNote.BackNote.Value;
            var nextNote = thisNote.NextNote.Value;
            if (backNote == null && nextNote != null) { return false; }
            if (backNote != null && nextNote == null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】SpaceHoldHiddenJudgedの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                nextNoteToNumber.Remove(thisNote);
                nextNoteToNumber.Add(nextNote, number);
            }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldHiddenJudgedRelayData == null)
            {
                dataOrigin.SpaceHoldHiddenJudgedRelayData = new List<NoteDataOrigin_SpaceHoldHiddenJudgedRelay>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldHiddenJudgedRelay data = new NoteDataOrigin_SpaceHoldHiddenJudgedRelay()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = number
            };

            dataOrigin.SpaceHoldHiddenJudgedRelayData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,  Dictionary<int, IChainNoteData> numberToStartNote)
        {
            if (dataOrigin.SpaceHoldHiddenJudgedRelayData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldHiddenJudgedRelayData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData) { return false; }
                if (noteData is not ITypeChangableNoteData) { return false; }
                if (noteData is not IVerticesControlableNoteData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);
                var chainData = (IChainNoteData)noteData;
                var verticesData = (IVerticesControlableNoteData)noteData;

                ((ITypeChangableNoteData)noteData).SetNoteType(DeploymentNoteType.SpaceHoldHiddenJudged);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                noteData.SetAddress(address);
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData, false);
                }
                else
                {
                    Debug.LogWarning($"【Converter】SpaceHoldHiddenJudgedの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }

    }

    /// <summary>
    /// スペースホールド終点(中継点データに変換)
    /// </summary>
    public class SpaceHoldEndConverter : ISpaceHoldDataToRhythmGameConvertable, IChainNoteConvertable
    {
        readonly List<float> RANGE_DEFAULT = new List<float>() { 100 };

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteOrigin in dataOrigin.SpaceHoldEndData)
            {
                NoteData_SpaceHoldRelay noteData = new NoteData_SpaceHoldRelay
                {
                    Vertices = noteOrigin.Vertices.Select(x=> x.ToVector2()).ToArray(),
                    Timing = timing
                };

                chartData.AddNoteData(noteData);
            }

            return true;
        }

        public bool AddDataForOrigin(IDeployableNoteData noteDataInEditor, SubDivisionDataOrigin dataOrigin,  Dictionary<IChainNoteData, int> nextNoteToNumber)
        {
            if (noteDataInEditor.NoteType != DeploymentNoteType.SpaceHold) { return false; }
            if (noteDataInEditor is not IChainNoteData) { return false; }
            if (noteDataInEditor is not IVerticesControlableNoteData) { return false; }

            var verticesData = ((IVerticesControlableNoteData)noteDataInEditor);
            var thisNote = (IChainNoteData)noteDataInEditor;
            var backNote = thisNote.BackNote.Value;
            var nextNote = thisNote.NextNote.Value;
            if (nextNote != null) { return false; }
            if (backNote == null && nextNote == null) { return false; }
            if (backNote != null && nextNote != null) { return false; }

            // ディクショナリーからHoldNumberを探す
            if (!nextNoteToNumber.TryGetValue(thisNote, out int number))
            {
                Debug.LogWarning($"【Converter】SpaceHoldEndの変換の際、ノーツが見つかりませんでした");
                return false;
            }
            else
            {
                // 終点なのでAddしない
                nextNoteToNumber.Remove(thisNote);
            }

            // 新たにインスタンス化
            if (dataOrigin.SpaceHoldEndData == null)
            {
                dataOrigin.SpaceHoldEndData = new List<NoteDataOrigin_SpaceHoldEnd>();
            }

            // 追加するデータのインスタンス化
            NoteDataOrigin_SpaceHoldEnd data = new NoteDataOrigin_SpaceHoldEnd()
            {
                Vertices = verticesData.SpaceHoldVertices.GetVertexArray(),
                HoldNumber = number
            };

            dataOrigin.SpaceHoldEndData.Add(data);
            return true;
        }

        public bool AddDataForEditorData(SubDivisionDataOrigin dataOrigin, ChartEditor.SubDivisionDataInBeat dataInChartEditor,  Dictionary<int, IChainNoteData> numberToStartNote)
        {
            if (dataOrigin.SpaceHoldEndData == null) { return true; }

            foreach (var noteDataOrigin in dataOrigin.SpaceHoldEndData)
            {
                IDeployableNoteData noteData = new ChartEditor.NoteData_SpaceHold();
                if (noteData is not IChainNoteData) { return false; }
                if (noteData is not IVerticesControlableNoteData) { return false; }

                // データのセット
                var address = new AddressWithinRange(dataInChartEditor.BarIndex, dataInChartEditor.SubDivisionIndex, RANGE_DEFAULT);
                var chainData = (IChainNoteData)noteData;
                var verticesData = (IVerticesControlableNoteData)noteData;

                noteData.SetAddress(address);
                verticesData.SpaceHoldVertices.SetVertices(noteDataOrigin.Vertices.Select(x => x.ToVector2()).ToArray());
                dataInChartEditor.AddNote(noteData);

                // リストへの保存
                if (numberToStartNote.TryGetValue(noteDataOrigin.HoldNumber, out var startNote))
                {
                    // 繋げる
                    startNote.AddChainNote(chainData, false);
                }
                else
                {
                    Debug.LogWarning($"【Converter】SpaceHoldEndの変換の際、HoldNumberが存在しませんでした: {noteDataOrigin.HoldNumber}");
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// スペースホールドメッシュ
    /// </summary>
    public class SpaceHoldMeshConverter : IUnchainDataToRhythmGameConvertable
    {
        Dictionary<int, List<TimeToVertices>> numberToHoldMeshDataOrigin = new Dictionary<int, List<TimeToVertices>>();

        public bool AddDataForGameData(SubDivisionDataOrigin dataOrigin, ChartData chartData, float timing)
        {
            AddHoldStartData(dataOrigin, timing);
            AddHoldRelayData(dataOrigin, timing);
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
                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
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

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
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

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
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

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
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

                meshList.Add(new TimeToVertices { Vertices = noteOrigin.Vertices.Select(x => x.ToVector2()).ToArray(), Timing = timing });
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