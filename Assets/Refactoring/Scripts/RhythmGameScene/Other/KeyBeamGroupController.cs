using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class KeyBeamGroupController : MonoBehaviour
    {
        [SerializeField] KeyBeamController[] keyBeams;

        ISliderInputGetter inputGetter;

        [Inject]
        public void Inject(ISliderInputGetter getter)
        {
            inputGetter = getter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            for (int i = 0; i < keyBeams.Length; i++) 
            {
                // UniRxの仕様か何か知らないがint型が参照されてしまうので
                // iを直接代入するのではなくindexを介す
                int index = i;

                inputGetter.GetSliderInputReactiveProperty(index)
                    .Subscribe(value => keyBeams[index].SetActive(value))
                    .AddTo(this.gameObject);
            }
        }

    }

}
