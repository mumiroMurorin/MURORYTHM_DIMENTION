using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// プレビューのノーツのうち、再生位置の近くにあるものだけを表示する
    /// ノーツは生成の途中でも1個ずつ追加・削除できる
    /// </summary>
    public class PreviewNoteVisibilityController : MonoBehaviour
    {
        [SerializeField] float visibleBehindDistance = 5f;
        [SerializeField] float visibleAheadDistance = 100f;
        [SerializeField] SerializeInterface<ITimeGetter> timer;

        // 開始距離の小さい順に並べたノーツ
        readonly List<INoteVisibilityTarget> targets = new List<INoteVisibilityTarget>();
        readonly List<float> prefixMaxEndDistances = new List<float>();
        HashSet<INoteVisibilityTarget> visibleTargets = new HashSet<INoteVisibilityTarget>();
        HashSet<INoteVisibilityTarget> nextVisibleTargets = new HashSet<INoteVisibilityTarget>();

        INotePositionCalculator positionCalculator;
        float noteSpeed = 1f;
        float effectiveVisibleBehindDistance;
        float effectiveVisibleAheadDistance;
        bool isReady;
        bool requiresVisibilityRefresh;
        bool isPrefixDirty;

        /// <summary>
        /// 現在の再生位置(譜面上の累積距離)。準備ができていないときは0
        /// </summary>
        public float CurrentDistance
        {
            get
            {
                if (timer == null || timer.Value == null || positionCalculator == null) { return 0f; }
                return positionCalculator.GetPosition(timer.Value.Time) * noteSpeed;
            }
        }

        /// <summary>
        /// 譜面上の距離が、今プレビューに表示している範囲(再生位置の少し手前から先まで)に入っているか
        /// </summary>
        public bool IsInVisibleRange(float distance)
        {
            // 準備ができていないときは判断できないので、入っているものとして扱う
            if (!isReady || timer == null || timer.Value == null || positionCalculator == null) { return true; }

            float currentDistance = CurrentDistance;
            return distance >= currentDistance - effectiveVisibleBehindDistance &&
                   distance <= currentDistance + effectiveVisibleAheadDistance;
        }

        /// <summary>
        /// 位置計算に使う値をセットする (登録済みのノーツはそのまま残す)
        /// </summary>
        public void Initialize(INotePositionCalculator positionCalculator, float noteSpeed, float curveRadius)
        {
            this.positionCalculator = positionCalculator;
            this.noteSpeed = noteSpeed;
            float maxVisibleSpan = Mathf.Max(0f, 2f * Mathf.PI * curveRadius - 0.01f);
            effectiveVisibleBehindDistance = Mathf.Clamp(visibleBehindDistance, 0f, maxVisibleSpan);
            effectiveVisibleAheadDistance = Mathf.Clamp(
                visibleAheadDistance,
                0f,
                maxVisibleSpan - effectiveVisibleBehindDistance);
            isReady = true;
        }

        /// <summary>
        /// ノーツを登録する。表示するかどうかは次の LateUpdate で決まる
        /// </summary>
        public void Add(Component spawnedNote)
        {
            if (spawnedNote is not INoteVisibilityTarget target) { return; }

            target.SetActive(false);

            // 開始距離の順を保つ位置に挿入する
            targets.Insert(UpperBoundStart(target.StartChartDistance), target);
            isPrefixDirty = true;
        }

        /// <summary>
        /// ノーツの登録を解除して非表示にする
        /// </summary>
        public void Remove(Component spawnedNote)
        {
            if (spawnedNote is not INoteVisibilityTarget target) { return; }
            if (!targets.Remove(target)) { return; }

            Hide(target);
            isPrefixDirty = true;
        }

        /// <summary>
        /// 複数のノーツの登録をまとめて解除して非表示にする (1個ずつ解除するより速い)
        /// </summary>
        public void RemoveAll(HashSet<INoteVisibilityTarget> removeTargets)
        {
            if (removeTargets == null || removeTargets.Count == 0) { return; }

            if (targets.RemoveAll(removeTargets.Contains) == 0) { return; }

            foreach (INoteVisibilityTarget target in removeTargets)
            {
                Hide(target);
            }

            isPrefixDirty = true;
        }

        /// <summary>
        /// すべてのノーツの登録を解除して非表示にする
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
            requiresVisibilityRefresh = false;
            isPrefixDirty = false;
        }

        private void Hide(INoteVisibilityTarget target)
        {
            if (visibleTargets.Remove(target))
            {
                SetTargetVisible(target, false);
            }
        }

        private void LateUpdate()
        {
            UpdateVisibleRange(requiresVisibilityRefresh);
            requiresVisibilityRefresh = false;
        }

        private void UpdateVisibleRange(bool forceUpdate)
        {
            if (!isReady || timer == null || timer.Value == null || positionCalculator == null) { return; }

            if (isPrefixDirty)
            {
                RebuildPrefixMaxEndDistances();
                isPrefixDirty = false;
            }

            float currentDistance = positionCalculator.GetPosition(timer.Value.Time) * noteSpeed;
            float minDistance = currentDistance - effectiveVisibleBehindDistance;
            float maxDistance = currentDistance + effectiveVisibleAheadDistance;

            int candidateStartIndex = LowerBoundPrefixMaxEnd(minDistance);
            int candidateEndIndex = UpperBoundStart(maxDistance);

            nextVisibleTargets.Clear();
            for (int i = candidateStartIndex; i < candidateEndIndex; i++)
            {
                INoteVisibilityTarget target = targets[i];
                if (target.EndChartDistance >= minDistance &&
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
                foreach (INoteVisibilityTarget target in visibleTargets)
                {
                    if (!nextVisibleTargets.Contains(target))
                    {
                        SetTargetVisible(target, false);
                    }
                }

                foreach (INoteVisibilityTarget target in nextVisibleTargets)
                {
                    if (!visibleTargets.Contains(target))
                    {
                        SetTargetVisible(target, true);
                    }
                }
            }

            foreach (INoteVisibilityTarget target in nextVisibleTargets)
            {
                if (target is INoteRangeVisibilityTarget rangeVisibilityTarget)
                {
                    rangeVisibilityTarget.SetVisibleRange(minDistance, maxDistance);
                }
            }

            HashSet<INoteVisibilityTarget> previousVisibleTargets = visibleTargets;
            visibleTargets = nextVisibleTargets;
            nextVisibleTargets = previousVisibleTargets;
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
}
