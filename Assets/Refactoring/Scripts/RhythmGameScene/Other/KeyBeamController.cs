using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class KeyBeamController : MonoBehaviour
    {
        
        public void SetActive(bool isActive)
        {
            this.gameObject.SetActive(isActive);
        }

    }

}
