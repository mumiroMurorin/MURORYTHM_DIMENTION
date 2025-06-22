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
            // 一旦全部非表示
            foreach (var description in descriptionObjects)
            {
                description.InActive();
            }

            // 然るべき表示を行う
            foreach (var description in descriptionObjects)
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
                    if (currentEditMode.IsInEditModeList(targetEditModes)) { obj.SetActive(true); }
                }
            }

            public void InActive()
            {
                foreach (var obj in objs)
                {
                    obj.SetActive(false); 
                }
            }
        }
    }

}
