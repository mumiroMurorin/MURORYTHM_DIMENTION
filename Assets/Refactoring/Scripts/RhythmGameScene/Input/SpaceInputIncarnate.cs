using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace Refactoring
{
    public class SpaceInputIncarnate : MonoBehaviour
    {
        [SerializeField] HandCaptureObject rightHandCaptureObject;
        [SerializeField] HandCaptureObject leftHandCaptureObject;

        [SerializeField] Vector3 judgeFieldCenter;
        [SerializeField] Vector3 judgeFieldSize;

        ISpaceInputGetter spaceInputGetter;

        [Inject]
        public void Constructor(ISpaceInputGetter spaceInputGetter)
        {
            this.spaceInputGetter = spaceInputGetter;
        }

        void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // 右手
            spaceInputGetter?.GetSpaceInputReactiveDictionary(SpaceTrackingTag.RightHand)
                .ObserveAdd().Subscribe(value => MoveCaptureObject(rightHandCaptureObject.gameObject, value.Value))
                .AddTo(this.gameObject);

            // 左手
            spaceInputGetter?.GetSpaceInputReactiveDictionary(SpaceTrackingTag.LeftHand)
                .ObserveAdd().Subscribe(value => MoveCaptureObject(leftHandCaptureObject.gameObject, value.Value))
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// ハンドオブジェクトの位置を動かす
        /// </summary>
        /// <param name="handObject"></param>
        /// <param name="position"></param>
        private void MoveCaptureObject(GameObject handObject, Vector3 position)
        {
            position = new Vector3(
                judgeFieldCenter.x + position.x * (judgeFieldSize.x / 2f),
                judgeFieldCenter.y + position.y * (judgeFieldSize.y / 2f),
                judgeFieldCenter.z + position.z * (judgeFieldSize.z / 2f)
           );

            Debug.Log($"【Capture】{handObject.name},{position}");
            handObject.transform.position = position; 
        }
    }

}