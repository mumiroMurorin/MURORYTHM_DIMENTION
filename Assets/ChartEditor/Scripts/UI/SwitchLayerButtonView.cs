using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace ChartEditor
{
    public class SwitchLayerButtonView : MonoBehaviour
    {
        [SerializeField] Button switchButton;
        [SerializeField] TextMeshProUGUI tmp;
        [SerializeField] Color groundColor = Color.white;
        [SerializeField] Color spaceColor = Color.white;
        [SerializeField] Color verticesColor = Color.white;

        public Action OnClickCloseButtonListner;

        public void OnChangeEditMode(EditMode editMode)
        {
            if(editMode == EditMode.Connecting) { this.gameObject.SetActive(false); }

            this.gameObject.SetActive(true);
        }

        public void OnChangeEditNoteType(EditNoteType editNoteType)
        {
            switch (editNoteType)
            {
                case EditNoteType.Ground:
                    tmp.text = $"レイヤー\n<size=30><B><color={groundColor.ToHexString()}>Ground</color></B></size>";
                    break;
                case EditNoteType.Space:
                    tmp.text = $"レイヤー\n<size=30><B><color={spaceColor.ToHexString()}>Space</color></B></size>";
                    break;
                case EditNoteType.Vertices:
                    tmp.text = $"レイヤー\n<size=25><B><color={verticesColor.ToHexString()}>Vertices</color></B></size>";
                    break;
            }
        }

        public void OnClickSwitchButton()
        {
            OnClickCloseButtonListner?.Invoke();
        }
    }

}