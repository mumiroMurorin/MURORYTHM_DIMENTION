using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;


namespace Refactoring.UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        

        [Inject] 
        public void Construct()
        {
            
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            
        }

        private void SetEvent()
        {

        }
    }
}
