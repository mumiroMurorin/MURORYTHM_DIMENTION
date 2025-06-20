using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public class VertexIndicesSliderButtonView : MonoBehaviour
    {
        public Action OnClickedListner;

        public void OnClickButton()
        {
            OnClickedListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

}