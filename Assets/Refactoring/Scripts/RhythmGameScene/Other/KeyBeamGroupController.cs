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
                // UniRx�̎d�l�������m��Ȃ���int�^���Q�Ƃ���Ă��܂��̂�
                // i�𒼐ڑ������̂ł͂Ȃ�index���
                int index = i;

                inputGetter.GetSliderInputReactiveProperty(index)
                    .Subscribe(value => keyBeams[index].SetActive(value))
                    .AddTo(this.gameObject);
            }
        }

    }

}
