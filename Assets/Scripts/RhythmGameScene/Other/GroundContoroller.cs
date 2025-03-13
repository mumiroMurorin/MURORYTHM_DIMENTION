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
        /// グラウンドを動かす
        /// Time.deltaTime基準で動かすので注意
        /// </summary>
        private void MoveGround()
        {
            // 譜面を進める
            this.gameObject.transform.position += Vector3.back * optionHolder.NoteSpeed * Time.deltaTime;
        }
    }
}

