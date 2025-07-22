using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class ScreenSizeDropDownView : MonoBehaviour
    {
        [SerializeField] TMP_Dropdown dropdown;

        public Action<Resolution> OnChangeValueListner { get; set; }

        private void Start()
        {
            var names = Enum.GetNames(typeof(Resolution)).ToList();
            dropdown.ClearOptions();
            dropdown.AddOptions(names);

            // 2. 値が選ばれた時のコールバックを登録
            dropdown.onValueChanged.AddListener(OnChangeDropDown);
        }

        public void OnChangeResolution(Resolution resolution)
        {
            dropdown.value = (int)resolution;
        }

        public void OnChangeDropDown(int index)
        {
            Resolution selectedResolution = (Resolution)index;

            OnChangeValueListner?.Invoke(selectedResolution);
        }
    }

}
