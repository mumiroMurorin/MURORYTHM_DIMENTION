using UnityEngine;

public class AudioSpectrumVisualizer : MonoBehaviour
{
    const int SAMPLE_COUNT = 512;

    [SerializeField] AudioSource[] sources;
    [SerializeField] Transform[] bars;
    [SerializeField] float heightMultiplier = 50f;
    [SerializeField] FFTWindow fftWindow = FFTWindow.BlackmanHarris;

    float[] spectrumData;

    void Start()
    {
        spectrumData = new float[SAMPLE_COUNT];

        if (sources == null || sources.Length == 0)
        {
            sources = SoundManager.Instance.BGMSources;
        }
    }

    void Update()
    {
        if (sources == null) { return; }

        foreach(var source in sources)
        {
            if (!source.isPlaying) { continue; }

            // 周波数データを取得
            source.GetSpectrumData(spectrumData, 0, fftWindow);

            // 各バーに対して高さを反映
            for (int i = 0; i < bars.Length; i++)
            {
                //int index = Mathf.FloorToInt(i * (spectrumData.Length / (float)bars.Length));
                int index = (int)(Mathf.Pow(i / (float)bars.Length, 2f) * spectrumData.Length);
                float intensity = spectrumData[index] * heightMultiplier;

                Vector3 scale = bars[i].localScale;
                scale.y = Mathf.Lerp(scale.y, Mathf.Clamp(intensity, 0.05f, 10f), 0.5f); // 平滑化
                bars[i].localScale = scale;
            }
        }
    }
}
