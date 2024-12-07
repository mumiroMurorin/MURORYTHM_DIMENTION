using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class GroundTouchNoteSpawner : MonoBehaviour, INotePositionSetter
    {
        /// <summary>
        /// スタート地点への移動
        /// </summary>
        /// <param name="spawnData"></param>
        public void SetPosition(INoteSpawnData spawnData)
        {
            spawnData.NoteGameObject.transform.position = spawnData.SpawnPosition;
        }
    }

}