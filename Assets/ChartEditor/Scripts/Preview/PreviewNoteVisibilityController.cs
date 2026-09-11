using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class PreviewNoteVisibilityController : MonoBehaviour
    {
        [SerializeField] float visibleBehindDistance = 5f;
        [SerializeField] float visibleAheadDistance = 100f;
        [SerializeField] SerializeInterface<ITimeGetter> timer;

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
            targets.Clear();
            prefixMaxEndDistances.Clear();
            visibleTargets.Clear();
            nextVisibleTargets.Clear();
            isReady = false;
            requiresVisibilityRefresh = false;
        }

        public void Register(Component spawnedNote)
        {
            if (spawnedNote is not INoteVisibilityTarget target) { return; }

            target.SetActive(false);
            targets.Add(target);
        }

        public void CompleteRegistration()
        {
            targets.Sort((x, y) => x.StartChartDistance.CompareTo(y.StartChartDistance));
            RebuildPrefixMaxEndDistances();
            isReady = true;
            requiresVisibilityRefresh = true;
        }

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
        }

        private void LateUpdate()
        {
            UpdateVisibleRange(requiresVisibilityRefresh);
            requiresVisibilityRefresh = false;
        }

        private void UpdateVisibleRange(bool forceUpdate)
        {
            if (!isReady || timer == null || timer.Value == null || positionCalculator == null) { return; }

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
