using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class NoteSoundCollider : MonoBehaviour
    {
        [SerializeField] AudioClip se;

        public void PlaySE()
        {
            SoundManager.Instance.PlaySE(se);
        }
    }
}
