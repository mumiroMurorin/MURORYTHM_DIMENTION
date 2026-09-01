using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 譜面上の累積距離を基準に、表示範囲内のノーツだけを表示する
/// </summary>
public class NoteVisibilityController : MonoBehaviour
{
    readonly List<INoteVisibilityTarget> targets = new();
    readonly List<float> prefixMaxEndDistances = new();
    HashSet<INoteVisibilityTarget> visibleTargets = new();
    HashSet<INoteVisibilityTarget> nextVisibleTargets = new();

    ITimeGetter timer;
    INotePositionCalculator positionCalculator;
    float noteSpeed;
    float visibleBehindDistance;
    float visibleAheadDistance;
    bool isReady;
    bool requiresVisibilityRefresh;
    float previousDistance;
    bool hasPreviousDistance;

    /// <summary>
    /// 現在位置の計算に必要な参照を受け取る
    /// </summary>
    public void Initialize(
        ITimeGetter timer,
        INotePositionCalculator positionCalculator,
        float noteSpeed,
        float curveRadius,
        float visibleBehindDistance,
        float visibleAheadDistance)
    {
        this.timer = timer;
        this.positionCalculator = positionCalculator;
        this.noteSpeed = noteSpeed;

        // 表示区間の合計を円周未満に制限し、次周のノーツとの重複表示を防ぐ
        float maxVisibleSpan = Mathf.Max(0f, 2f * Mathf.PI * curveRadius - 0.01f);
        this.visibleBehindDistance = Mathf.Clamp(visibleBehindDistance, 0f, maxVisibleSpan);
        this.visibleAheadDistance = Mathf.Clamp(
            visibleAheadDistance,
            0f,
            maxVisibleSpan - this.visibleBehindDistance);
        previousDistance = 0f;
        hasPreviousDistance = false;
        isReady = false;
    }

    /// <summary>
    /// 生成済みノーツを表示管理対象へ追加する
    /// </summary>
    public void Register(Component spawnedNote)
    {
        if (spawnedNote is not INoteVisibilityTarget target) { return; }

        target.SetActive(false);
        targets.Add(target);
    }

    /// <summary>
    /// 距離順に並べ、現在の表示範囲を反映する
    /// </summary>
    public void CompleteRegistration()
    {
        targets.Sort((x, y) => x.StartChartDistance.CompareTo(y.StartChartDistance));
        RebuildPrefixMaxEndDistances();
        isReady = true;
        requiresVisibilityRefresh = true;
    }

    /// <summary>
    /// 前回の譜面情報を破棄する
    /// </summary>
    public void Clear()
    {
        foreach (INoteVisibilityTarget target in visibleTargets)
        {
            SetTargetVisible(target, false);
        }

        targets.Clear();
        prefixMaxEndDistances.Clear();
        visibleTargets.Clear();
        nextVisibleTargets.Clear();
        isReady = false;
        requiresVisibilityRefresh = false;
        previousDistance = 0f;
        hasPreviousDistance = false;
    }

    private void LateUpdate()
    {
        UpdateVisibleRange(requiresVisibilityRefresh);
        requiresVisibilityRefresh = false;
    }

    private void UpdateVisibleRange(bool forceUpdate)
    {
        if (!isReady || timer == null || positionCalculator == null) { return; }

        float currentDistance = positionCalculator.GetPosition(timer.Time) * noteSpeed;
        float minDistance = currentDistance - visibleBehindDistance;
        float maxDistance = currentDistance + visibleAheadDistance;

        bool isMovingForward = !hasPreviousDistance || currentDistance >= previousDistance;
        UpdateVisibilityLocks(currentDistance, isMovingForward);
        previousDistance = currentDistance;
        hasPreviousDistance = true;

        int candidateStartIndex = LowerBoundPrefixMaxEnd(minDistance);
        int candidateEndIndex = UpperBoundStart(maxDistance);

        nextVisibleTargets.Clear();
        for (int i = candidateStartIndex; i < candidateEndIndex; i++)
        {
            INoteVisibilityTarget target = targets[i];
            if (!target.IsVisibilityLocked &&
                target.EndChartDistance >= minDistance &&
                target.StartChartDistance <= maxDistance)
            {
                nextVisibleTargets.Add(target);
            }
        }

        if (forceUpdate)
        {
            foreach (INoteVisibilityTarget target in targets)
            {
                SetTargetVisible(target, nextVisibleTargets.Contains(target));
            }
        }
        else
        {
            // 新しい表示範囲から外れたノーツだけを非表示にする
            foreach (INoteVisibilityTarget target in visibleTargets)
            {
                if (!nextVisibleTargets.Contains(target))
                {
                    SetTargetVisible(target, false);
                }
            }

            // 新しく表示範囲へ入ったノーツだけを表示する
            foreach (INoteVisibilityTarget target in nextVisibleTargets)
            {
                if (!visibleTargets.Contains(target))
                {
                    SetTargetVisible(target, true);
                }
            }
        }

        HashSet<INoteVisibilityTarget> previousVisibleTargets = visibleTargets;
        visibleTargets = nextVisibleTargets;
        nextVisibleTargets = previousVisibleTargets;
    }

    private void UpdateVisibilityLocks(float currentDistance, bool isMovingForward)
    {
        foreach (INoteVisibilityTarget target in targets)
        {
            if (isMovingForward)
            {
                if (target.IsVisibilityLocked) { continue; }
                if (!target.ShouldLockVisibility(currentDistance)) { continue; }

                target.LockVisibility();
                visibleTargets.Remove(target);
            }
            else
            {
                if (!target.IsVisibilityLocked) { continue; }
                if (target.ShouldLockVisibility(currentDistance)) { continue; }

                target.UnlockVisibility();
            }
        }
    }

    private void RebuildPrefixMaxEndDistances()
    {
        prefixMaxEndDistances.Clear();

        float maxEndDistance = float.NegativeInfinity;
        foreach (INoteVisibilityTarget target in targets)
        {
            maxEndDistance = Mathf.Max(maxEndDistance, target.EndChartDistance);
            prefixMaxEndDistances.Add(maxEndDistance);
        }
    }

    private int LowerBoundPrefixMaxEnd(float distance)
    {
        int left = 0;
        int right = prefixMaxEndDistances.Count;

        while (left < right)
        {
            int middle = left + (right - left) / 2;
            if (prefixMaxEndDistances[middle] < distance) { left = middle + 1; }
            else { right = middle; }
        }

        return left;
    }

    private int UpperBoundStart(float distance)
    {
        int left = 0;
        int right = targets.Count;

        while (left < right)
        {
            int middle = left + (right - left) / 2;
            if (targets[middle].StartChartDistance <= distance) { left = middle + 1; }
            else { right = middle; }
        }

        return left;
    }

    private static void SetTargetVisible(INoteVisibilityTarget target, bool isVisible)
    {
        if (target is not Component component || component == null) { return; }

        target.SetActive(isVisible);
    }
}
