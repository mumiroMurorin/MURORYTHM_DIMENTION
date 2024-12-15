using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class NotesStarter : MonoBehaviour
    {
        [SerializeField] TimeCounter timeCounter;

        ChartData chartData;

        INoteSpawnDataOptionHolder option;

        [Inject]
        public void Inject(INoteSpawnDataOptionHolder option)
        {
            this.option = option;
        }

        private void Update()
        {
            float time = timeCounter.Time;

            //if()
        }

        public void StartNotes(GroundTouchNoteData data, float currentTime, INoteStarter starter)
        {
            // スタートポジションにずれを足す
            //data.SpawnPosition = startPoint.position + Vector3.back * (data.JudgeTime - currentTime) * option.NoteSpeed;
            starter.SetPosition(data.SpawnPosition);
            starter.SetActive(true);
            starter.StartMovement(Vector3.back * option.NoteSpeed);
        }
    }

    public class ChartData
    {
        public List<GroundTouchNoteData> groundTouchNoteList;

    }

}
