using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ChartEditor
{
    public class MusicNameView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        public void OnChangeMusic(AudioClip clip)
        {
            if(clip == null) { return; }
            tmp.text = clip.name;
        }
    }

}