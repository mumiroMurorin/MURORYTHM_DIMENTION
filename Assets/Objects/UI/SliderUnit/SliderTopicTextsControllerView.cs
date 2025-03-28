using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class SliderTopicTextsControllerView : MonoBehaviour
    {
        [SerializeField] GameObject sliderTopicPrefab;
        [SerializeField] Transform parent;

        List<SliderTopicTextController> sliderTopics = new List<SliderTopicTextController>();

        /// <summary>
        /// ‘€ì‚ª’Ç‰Á‚³‚ê‚½‚Ìˆ—
        /// </summary>
        /// <param name="sliderTouchData"></param>
        public void OnChangeSliderData(SliderTouchData sliderTouchData)
        {
            SliderTopicTextController sliderTopic = Instantiate(sliderTopicPrefab, parent).GetComponent<SliderTopicTextController>();
            sliderTopic.SetSliderTouchData(sliderTouchData);
            sliderTopic.transform.localPosition = Vector3.zero;
            sliderTopics.Add(sliderTopic);
        }

        /// <summary>
        /// ‘€ì‚ªˆêV‚³‚ê‚½‚Ìˆ—
        /// </summary>
        public void OnClearSliderData()
        {
            if (sliderTopics == null) { return; }

            foreach (var topic in sliderTopics)
            {
                Destroy(topic.transform.gameObject);
            }

            sliderTopics.Clear();
        }
    }

}
