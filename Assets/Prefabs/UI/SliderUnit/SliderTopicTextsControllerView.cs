using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SliderTopicTextsControllerView : MonoBehaviour
{
    [SerializeField] GameObject sliderTopicPrefab;
    [SerializeField] Transform parent;

    List<SliderTopicTextController> sliderTopics = new List<SliderTopicTextController>();

    /// <summary>
    /// 操作が追加された時の処理
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
    /// 操作が一新された時の処理
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
