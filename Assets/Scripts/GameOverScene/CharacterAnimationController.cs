using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInGameOverScene
{
    public class CharacterAnimationController : MonoBehaviour
    {
        [SerializeField] Animator anim;

        const string CONTINUE_TAG = "Continue";
        const string GAMEOVER_TAG = "Finish";

        public void OnContinueSelected()
        {
            anim?.SetTrigger(CONTINUE_TAG);
        }

        public void OnFinishSelected()
        {
            anim?.SetTrigger(GAMEOVER_TAG);
        }
    }

}
