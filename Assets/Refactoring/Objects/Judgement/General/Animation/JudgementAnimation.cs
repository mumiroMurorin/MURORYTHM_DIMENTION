using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class JudgementAnimation : MonoBehaviour
    {
        public void Destroy()
        {
            Destroy(this.gameObject);
        }
    }

}
