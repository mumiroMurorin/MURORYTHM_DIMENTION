using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class OperationDescriptionView : MonoBehaviour
    {
        [SerializeField] EditModeToDescription[] descriptionObjects;

        public void OnChangeEditMode(EditMode editMode)
        {
            foreach(var description in descriptionObjects)
            {
                description.CheckAndSet(editMode);
            }
        }

        [System.Serializable]
        class EditModeToDescription
        {
            [SerializeField] string name = "type";
            [SerializeField] EditMode[] targetEditModes;
            [SerializeField] GameObject[] objs;

            public void CheckAndSet(EditMode currentEditMode)
            {
                foreach(var obj in objs)
                {
                    obj.SetActive(currentEditMode.IsInEditModeList(targetEditModes));
                }
            }
        }
    }

}
