using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using MeshGenerate;
using Deform;

public class NoteFactory_HoldMesh : NoteFactory<NoteData_HoldMesh>
{
    sealed class SpawnedHold
    {
        public List<TimeToRange> TimeToRanges;
        public int SortingOrder;
    }

    const int MaxSortingOrder = NoteLayerUtility.DefaultNoteSortingOrder - 1;
    const float OverlapEpsilon = 0.0001f;

    [SerializeField] GameObject noteObjectOriginPrefab;
    [SerializeField] GameObject noteMeshPrefab;

    [Header("meshの1レーン内の分割数")]
    [SerializeField] int meshHorizontalDivisionNum = 10;

    [Header("mesh1単位の最大長さ")]
    [SerializeField] float maxTriangleLength = 0.5f;

    INoteSpawnDataOptionGetter optionHolder;
    ISliderInputGetter sliderInputGetter;
    IJudgementRecorder judgementRecorder;
    ITimeGetter timer;
    Transform noteParent;
    readonly List<SpawnedHold> spawnedHolds = new List<SpawnedHold>();

    public override void Initialize(NoteFactoryInitializingData initializingData)
    {
        this.optionHolder = initializingData.OptionHolder;
        this.noteParent = initializingData.NoteParent;
        this.sliderInputGetter = initializingData.SliderInputGetter;
        this.judgementRecorder = initializingData.JudgementRecorder;
        this.timer = initializingData.Timer;
        spawnedHolds.Clear();
    }

    public override NoteObject<NoteData_HoldMesh> Spawn(NoteData_HoldMesh data, INotePositionCalculator positionCalculator)
    {
        // 位置調整
        float startDistance = positionCalculator.GetPosition(data.Timing) * optionHolder.NoteSpeed.Value;
        float endTiming = data.TimeToRanges != null && data.TimeToRanges.Count > 0
            ? data.TimeToRanges.Max(x => x.Timing)
            : data.Timing;
        NoteObject<NoteData_HoldMesh> note = GenerateNoteInstance(ConvertNoteData(data), positionCalculator);
        float endDistance = positionCalculator.GetPosition(endTiming) * optionHolder.NoteSpeed.Value;
        SetTransform(note, startDistance, endDistance);
        int sortingOrder = GetSortingOrder(data.TimeToRanges);
        NoteLayerUtility.SetSortingOrderRecursively(note.gameObject, sortingOrder);
        LongNoteMeshVisibility.Attach(
            note,
            startDistance,
            optionHolder.NoteCurveRadius.Value,
            timer,
            positionCalculator,
            optionHolder.NoteSpeed.Value);

        // 初期化
        note.Initialize(data);

        return note;
    }

    private int GetSortingOrder(List<TimeToRange> timeToRanges)
    {
        int sortingOrder = 0;
        foreach (SpawnedHold spawnedHold in spawnedHolds)
        {
            if (!DoHoldMeshesOverlap(timeToRanges, spawnedHold.TimeToRanges)) { continue; }

            sortingOrder = Mathf.Max(sortingOrder, spawnedHold.SortingOrder + 1);
        }

        sortingOrder = Mathf.Min(sortingOrder, MaxSortingOrder);
        spawnedHolds.Add(new SpawnedHold
        {
            TimeToRanges = timeToRanges,
            SortingOrder = sortingOrder
        });
        return sortingOrder;
    }

    private static bool DoHoldMeshesOverlap(List<TimeToRange> first, List<TimeToRange> second)
    {
        if (first == null || second == null || first.Count < 2 || second.Count < 2) { return false; }

        for (int firstIndex = 0; firstIndex < first.Count - 1; firstIndex++)
        {
            TimeToRange firstStart = first[firstIndex];
            TimeToRange firstEnd = first[firstIndex + 1];
            if (!IsValidRangeSegment(firstStart, firstEnd)) { continue; }

            for (int secondIndex = 0; secondIndex < second.Count - 1; secondIndex++)
            {
                TimeToRange secondStart = second[secondIndex];
                TimeToRange secondEnd = second[secondIndex + 1];
                if (!IsValidRangeSegment(secondStart, secondEnd)) { continue; }

                float overlapStart = Mathf.Max(firstStart.Timing, secondStart.Timing);
                float overlapEnd = Mathf.Min(firstEnd.Timing, secondEnd.Timing);
                if (overlapEnd - overlapStart <= OverlapEpsilon) { continue; }

                if (DoRangeSegmentsOverlap(
                    firstStart,
                    firstEnd,
                    secondStart,
                    secondEnd,
                    overlapStart,
                    overlapEnd))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsValidRangeSegment(TimeToRange start, TimeToRange end)
    {
        return start != null &&
               end != null &&
               start.Range != null &&
               end.Range != null &&
               start.Range.Length > 0 &&
               end.Range.Length > 0 &&
               end.Timing - start.Timing > OverlapEpsilon;
    }

    private static bool DoRangeSegmentsOverlap(
        TimeToRange firstStart,
        TimeToRange firstEnd,
        TimeToRange secondStart,
        TimeToRange secondEnd,
        float overlapStart,
        float overlapEnd)
    {
        List<float> candidateTimings = new List<float> { overlapStart, overlapEnd };
        AddBoundaryIntersectionTiming(candidateTimings, firstStart, firstEnd, secondStart, secondEnd, true, true, overlapStart, overlapEnd);
        AddBoundaryIntersectionTiming(candidateTimings, firstStart, firstEnd, secondStart, secondEnd, false, false, overlapStart, overlapEnd);
        AddBoundaryIntersectionTiming(candidateTimings, firstStart, firstEnd, secondStart, secondEnd, false, true, overlapStart, overlapEnd);
        AddBoundaryIntersectionTiming(candidateTimings, secondStart, secondEnd, firstStart, firstEnd, false, true, overlapStart, overlapEnd);

        candidateTimings.Sort();
        for (int i = 0; i < candidateTimings.Count; i++)
        {
            if (RangesOverlapAtTiming(firstStart, firstEnd, secondStart, secondEnd, candidateTimings[i]))
            {
                return true;
            }

            if (i + 1 >= candidateTimings.Count) { continue; }

            float midpoint = (candidateTimings[i] + candidateTimings[i + 1]) * 0.5f;
            if (RangesOverlapAtTiming(firstStart, firstEnd, secondStart, secondEnd, midpoint))
            {
                return true;
            }
        }

        return false;
    }

    private static void AddBoundaryIntersectionTiming(
        List<float> candidateTimings,
        TimeToRange firstStart,
        TimeToRange firstEnd,
        TimeToRange secondStart,
        TimeToRange secondEnd,
        bool useFirstLeft,
        bool useSecondLeft,
        float overlapStart,
        float overlapEnd)
    {
        float differenceAtStart = GetBoundary(firstStart, firstEnd, overlapStart, useFirstLeft) -
                                  GetBoundary(secondStart, secondEnd, overlapStart, useSecondLeft);
        float differenceAtEnd = GetBoundary(firstStart, firstEnd, overlapEnd, useFirstLeft) -
                                GetBoundary(secondStart, secondEnd, overlapEnd, useSecondLeft);
        float denominator = differenceAtStart - differenceAtEnd;
        if (Mathf.Abs(denominator) <= OverlapEpsilon) { return; }

        float ratio = differenceAtStart / denominator;
        if (ratio <= 0f || ratio >= 1f) { return; }

        candidateTimings.Add(Mathf.Lerp(overlapStart, overlapEnd, ratio));
    }

    private static bool RangesOverlapAtTiming(
        TimeToRange firstStart,
        TimeToRange firstEnd,
        TimeToRange secondStart,
        TimeToRange secondEnd,
        float timing)
    {
        float firstLeft = GetBoundary(firstStart, firstEnd, timing, true);
        float firstRight = GetBoundary(firstStart, firstEnd, timing, false);
        float secondLeft = GetBoundary(secondStart, secondEnd, timing, true);
        float secondRight = GetBoundary(secondStart, secondEnd, timing, false);

        return Mathf.Min(firstRight, secondRight) - Mathf.Max(firstLeft, secondLeft) > OverlapEpsilon;
    }

    private static float GetBoundary(TimeToRange start, TimeToRange end, float timing, bool useLeft)
    {
        float duration = end.Timing - start.Timing;
        float ratio = duration > OverlapEpsilon ? (timing - start.Timing) / duration : 0f;
        float startBoundary = useLeft ? start.Range[0] : start.Range[^1] + 1f;
        float endBoundary = useLeft ? end.Range[0] : end.Range[^1] + 1f;
        return Mathf.Lerp(startBoundary, endBoundary, ratio);
    }

    /// <summary>
    /// ノートデータにさらなる情報を追加
    /// </summary>
    /// <param name="data"></param>
    private NoteData_HoldMesh ConvertNoteData(NoteData_HoldMesh data)
    {
        // ノーツデータにいろいろ追加
        data.SliderInput = this.sliderInputGetter;
        data.Timer = this.timer;
        data.OptionGetter = optionHolder;
        
        return data;
    }

    /// <summary>
    /// ノーツをインスタンス化して返す
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    private NoteObject<NoteData_HoldMesh> GenerateNoteInstance(
        NoteData_HoldMesh data,
        INotePositionCalculator positionCalculator)
    {
        GameObject origin = Instantiate(noteObjectOriginPrefab);

        // ノーツオブジェクトを生成
        GameObject noteObj = GenerateMeshObject(data, positionCalculator);

        // originにくっつける
        noteObj.transform.SetParent(origin.transform);

        // コンポーネントを取得
        NoteObject<NoteData_HoldMesh> note = origin.GetComponent<NoteObject<NoteData_HoldMesh>>();

        return note;
    }

    /// <summary>
    /// ホールドのメッシュ部分の生成
    /// </summary>
    private GameObject GenerateMeshObject(
        NoteData_HoldMesh noteData,
        INotePositionCalculator positionCalculator)
    {
        var obj = Instantiate(noteMeshPrefab);
        NoteLayerUtility.SetNotesLayerRecursively(obj);
        if (!obj.TryGetComponent(out MeshFilter meshFilter)) { meshFilter = obj.AddComponent<MeshFilter>(); }
        if (!obj.TryGetComponent(out MeshRenderer meshRenderer)) { meshRenderer = obj.AddComponent<MeshRenderer>(); }

        Mesh mesh = GroundHoldMeshGenerator.GenerateGroundHoldMesh(
            noteData.TimeToRanges,
            positionCalculator,
            optionHolder.NoteSpeed.Value,
            meshHorizontalDivisionNum,
            maxTriangleLength,
            optionHolder.NoteCurveRadius.Value);
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshFilter.mesh = mesh;

        return obj;
    }

    /// <summary>
    /// 位置調整など
    /// </summary>
    private void SetTransform(NoteObject<NoteData_HoldMesh> note, float startDistance, float endDistance)
    {
        // 動く地面を親登録
        note.transform.SetParent(noteParent);

        // 位置の調整
        note.SetPosition(startDistance, endDistance, optionHolder.NoteCurveRadius.Value);
    }
}
