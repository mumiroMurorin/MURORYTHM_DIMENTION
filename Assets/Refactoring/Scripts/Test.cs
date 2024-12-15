using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class Test : MonoBehaviour
    {
        [SerializeField] GroundTouchNoteGenerator generator;
        //[SerializeField] GroundTouchNoteSpawner spawner;
        [SerializeField] TimeCounter timer;

        const float START_GROUND_Z = 183.25f;

        ISliderInputGetter inputGetter;

        [Inject]
        public void Inject(ISliderInputGetter inputGetter)
        {
            this.inputGetter = inputGetter;
        }

        void Start()
        {
            GroundTouchNoteData data = new GroundTouchNoteData()
            {
                StartIndex = 4,
                EndIndex = 11,
                JudgeTime = 2f,
                SpawnPosition = new Vector3(0, 0, START_GROUND_Z)
            };

            
            timer.StartTimer();
            // note.StartMovement(new Vector3(0, 0, -60f));
        }

        void Update()
        {

        }
    }
}