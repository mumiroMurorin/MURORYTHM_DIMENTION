using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class GroundContoroller : MonoBehaviour, IGroundController
    {
        INoteSpawnDataOptionHolder optionHolder;

        bool isMoving;

        [Inject]
        public void Constructor(INoteSpawnDataOptionHolder optionHolder)
        {
            this.optionHolder = optionHolder;
        }

        void IGroundController.StartGroundMove()
        {
            isMoving = true;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return)) { isMoving = true; }
            if (isMoving) { MoveGround(); }
        }

        /// <summary>
        /// �O���E���h�𓮂���
        /// Time.deltaTime��œ������̂Œ���
        /// </summary>
        private void MoveGround()
        {
            // ���ʂ�i�߂�
            this.gameObject.transform.position += Vector3.back * optionHolder.NoteSpeed * Time.deltaTime;
        }
    }
}

