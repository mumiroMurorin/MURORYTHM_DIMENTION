using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class CircleController : MonoBehaviour
    {
        [SerializeField] SymphonyTypeToView[] objs;

        public void OnChangeSymphonyType(SymphonyType type)
        {
            if(objs == null) { return; }
            
            foreach(var obj in objs)
            {
                obj.SetActive(obj.CheckCondition(type));
            }
        }

        [System.Serializable]
        class SymphonyTypeToView
        {
            [SerializeField] SymphonyType symphonyType;
            [SerializeField] GameObject obj;

            public bool CheckCondition(SymphonyType symphonyType)
            {
                return this.symphonyType == symphonyType;
            }

            public void SetActive(bool isActive)
            {
                obj.SetActive(isActive);
            }
        }
    }

}
