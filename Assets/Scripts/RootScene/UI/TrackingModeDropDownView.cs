using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UIInRootScene
{
    public class TrackingModeDropDownView : MonoBehaviour
    {
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
                "GraphRunner Hand",
                "Leap Motion",
                "Kinect",
            });

            dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        public void OnChangeTrackingMode(TrackingMode trackingMode)
        {
            if (dropdown == null) { return; }

            dropdown.SetValueWithoutNotify((int)trackingMode);
        }

        private void OnValueChanged(int value)
        {
            OnTrackingModeChangedListener?.Invoke((TrackingMode)value);
        }
    }
}
