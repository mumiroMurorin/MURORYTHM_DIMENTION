using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class EditorWaveformRenderer : MonoBehaviour
{
    [SerializeField]
    AudioSource audioSource;
    [SerializeField]
    RawImage image;
    [SerializeField]
    int imageWidth;

    Texture2D texture;
    float[] samples = new float[500000];

    void Start()
    {
        texture = new Texture2D(imageWidth, 1);
        texture.SetPixels(Enumerable.Range(0, imageWidth).Select(_ => Color.clear).ToArray());
        texture.Apply();
        image.texture = texture;
    }

    void Update()
    {
        audioSource.clip.GetData(samples, audioSource.timeSamples);

        int textureX = 0;
        int skipSamples = 200;
        float maxSample = 0;

        for (int i = 0, l = samples.Length; i < l && textureX < imageWidth; i++)
        {
            maxSample = Mathf.Max(maxSample, samples[i]);

            if (i % skipSamples == 0)
            {
                texture.SetPixel(textureX, 0, new Color(maxSample, 0, 0));
                maxSample = 0;
                textureX++;
            }
        }

        texture.Apply();
    }
}