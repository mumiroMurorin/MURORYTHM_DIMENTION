using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UIInRootScene
{
    public class TrackingModeDropDownView : MonoBehaviour
    {
        private static readonly IReadOnlyList<TrackingMode> SelectableModes = new[]
        {
            TrackingMode.BodyTracking,
            TrackingMode.HandTracking,
            TrackingMode.LeapMotion,
            TrackingMode.Kinect,
        };

        [SerializeField] private TMP_Dropdown dropdown;

        public Action<TrackingMode> OnTrackingModeChangedListener { get; set; }

        private void Awake()
        {
            if (dropdown == null) { return; }

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>
            {
                "Body Tracking",
                "Hand Tracking",
                "Leap Motion",
                "Kinect",
            });

            dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        public void OnChangeTrackingMode(TrackingMode trackingMode)
        {
            if (dropdown == null) { return; }

            if (trackingMode == TrackingMode.GraphRunnerHandTracking)
            {
                trackingMode = TrackingMode.HandTracking;
            }

            var index = IndexOfMode(trackingMode);
            dropdown.SetValueWithoutNotify(index >= 0 ? index : 0);
        }

        private void OnValueChanged(int value)
        {
            if (value < 0 || value >= SelectableModes.Count) { return; }

            OnTrackingModeChangedListener?.Invoke(SelectableModes[value]);
        }

        private static int IndexOfMode(TrackingMode trackingMode)
        {
            for (var i = 0; i < SelectableModes.Count; i++)
            {
                if (SelectableModes[i] == trackingMode) { return i; }
            }

            return -1;
        }
    }
}
