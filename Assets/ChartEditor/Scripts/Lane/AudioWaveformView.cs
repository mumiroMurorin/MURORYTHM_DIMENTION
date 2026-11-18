using System;
using UniRx;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

namespace ChartEditor
{
    /// <summary>
    /// 楽曲の音声波形を、グラウンド (レーンの面) の上、線やノーツの下に、緑で出す (録音 20261118_221639)
    ///
    /// ・時刻の向きは、レーンの奥行き (秒 × 表示の倍率)。オフセットのぶんだけずれる (オフセットを変えると、波形も動く)
    /// ・グラウンドがある所 (譜面の始まりから終わりまで、レーンの幅の中) だけに出す。曲が譜面より長くても、はみ出さない
    /// ・波形の幅は、その時刻の音の大きさ (10 ms ごとの RMS。曲の中でいちばん大きい所を、レーンの幅いっぱいにする)。左右対称
    /// ・出すかどうかは、メニューの「表示」→「音声波形」 (EditorPanel.AudioWaveform)。はじめは出さない
    /// 描く板 (メッシュ) は、シーンの LaneParent の下に置いてある (AudioWaveform)。このコンポーネントは、注入を受ける LaneControllers に置く。
    /// </summary>
    public class AudioWaveformView : MonoBehaviour
    {
        [Tooltip("波形を描く板 (LaneParent の下。グラウンドの面より少し上)")]
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;
        [Tooltip("波形の色")]
        [SerializeField] Color color = new Color(0.25f, 0.95f, 0.4f, 0.45f);
        [Tooltip("いちばん大きい音のときの、波形の幅の半分 (グラウンドの幅の半分: 16 / 2)")]
        [SerializeField] float halfWidth = 7.8f;

        // 1 秒をいくつに分けて、音の大きさを調べるか
        const int BUCKETS_PER_SECOND = 100;
        // 音を読むときに、一度に読む長さ (秒。曲全体を一度に読むと、メモリをたくさん使う)
        const int READ_CHUNK_SECONDS = 2;

        IChartEditorDataGetter dataGetter;
        IChartEditorOptionGetter optionGetter;

        Mesh mesh;
        Material material;
        AudioClip envelopeClip;
        float[] envelope;
        bool isVisible;
        bool isDirty = true;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.dataGetter = dataGetter;
            this.optionGetter = optionGetter;
        }

        void Start()
        {
            if (meshFilter == null || meshRenderer == null || dataGetter == null || optionGetter == null)
            {
                Debug.LogWarning("【音声波形】描く板か、譜面の情報が見つからないので、音声波形は出しません");
                enabled = false;
                return;
            }

            mesh = new Mesh { name = "AudioWaveform", indexFormat = IndexFormat.UInt32 };
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;

            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("UI/Default");
            if (shader != null)
            {
                material = new Material(shader) { name = "AudioWaveform", color = Color.white };
                meshRenderer.sharedMaterial = material;
            }
            meshRenderer.enabled = false;

            dataGetter.Music.Subscribe(_ => isDirty = true).AddTo(this);
            dataGetter.Offset.Subscribe(_ => isDirty = true).AddTo(this);
            dataGetter.ChartSeconds.Subscribe(_ => isDirty = true).AddTo(this);
            optionGetter.ChartViewScale.Subscribe(_ => isDirty = true).AddTo(this);
            optionGetter.GetPanelVisibility(EditorPanel.AudioWaveform)
                .Subscribe(visible =>
                {
                    isVisible = visible;
                    isDirty = true;
                })
                .AddTo(this);
        }

        void LateUpdate()
        {
            if (!isDirty) { return; }
            isDirty = false;

            AudioClip clip = dataGetter.Music.Value;
            bool isShown = isVisible && clip != null && TryGetEnvelope(clip);
            meshRenderer.enabled = isShown;
            if (isShown) { RebuildMesh(); }
        }

        void OnDestroy()
        {
            if (mesh != null) { Destroy(mesh); }
            if (material != null) { Destroy(material); }
        }

        /// <summary>
        /// 曲の、時間ごとの音の大きさ (0〜1。曲の中でいちばん大きい音が 1) を求める (曲が替わったときだけ)
        /// </summary>
        private bool TryGetEnvelope(AudioClip clip)
        {
            if (ReferenceEquals(clip, envelopeClip)) { return envelope != null; }

            envelopeClip = clip;
            envelope = null;

            int channels = Mathf.Max(1, clip.channels);
            int frequency = Mathf.Max(1, clip.frequency);
            int totalFrames = clip.samples;
            if (totalFrames <= 0) { return false; }
            if (clip.loadState != AudioDataLoadState.Loaded && !clip.LoadAudioData()) { return false; }

            int framesPerBucket = Mathf.Max(1, frequency / BUCKETS_PER_SECOND);
            int bucketCount = (totalFrames + framesPerBucket - 1) / framesPerBucket;
            var peaks = new float[bucketCount];

            int chunkFrames = frequency * READ_CHUNK_SECONDS;
            var buffer = new float[chunkFrames * channels];
            // 区間ごとの、音の大きさ (二乗平均の平方根: RMS)。いちばん大きな値 (ピーク) を取ると、音圧の高い曲では
            // ほとんどの区間で最大になって、波に見えなかった (録音 20261118_225907)
            var sums = new double[bucketCount];
            var counts = new int[bucketCount];

            for (int startFrame = 0; startFrame < totalFrames; startFrame += chunkFrames)
            {
                int frames = Mathf.Min(chunkFrames, totalFrames - startFrame);
                // (最後の1回は、短い配列で読む: GetData は配列の長さぶん読む)
                float[] data = frames == chunkFrames ? buffer : new float[frames * channels];
                if (!clip.GetData(data, startFrame))
                {
                    Debug.LogWarning("【音声波形】この曲は、音のデータを読めないので、音声波形を出せません");
                    return false;
                }

                for (int frame = 0; frame < frames; frame++)
                {
                    int baseIndex = frame * channels;
                    float square = 0f;
                    for (int channel = 0; channel < channels; channel++)
                    {
                        float value = data[baseIndex + channel];
                        square += value * value;
                    }

                    int bucket = (startFrame + frame) / framesPerBucket;
                    sums[bucket] += square / channels;
                    counts[bucket]++;
                }
            }

            float maxRms = 0f;
            for (int i = 0; i < bucketCount; i++)
            {
                peaks[i] = counts[i] > 0 ? (float)System.Math.Sqrt(sums[i] / counts[i]) : 0f;
                if (peaks[i] > maxRms) { maxRms = peaks[i]; }
            }
            if (maxRms <= 0f) { return false; }
            for (int i = 0; i < peaks.Length; i++) { peaks[i] /= maxRms; }
            envelope = peaks;
            return true;
        }

        /// <summary>
        /// 波形の板を作り直す (グラウンドがある所だけ: 奥行き 0 から 譜面の長さ × 倍率 まで)
        /// </summary>
        private void RebuildMesh()
        {
            mesh.Clear();
            if (envelope == null || envelope.Length < 2) { return; }

            float scale = optionGetter.ChartViewScale.Value;
            float offsetSeconds = dataGetter.Offset.Value / 1000f;
            float groundLength = dataGetter.ChartSeconds.Value * scale;
            if (scale <= 0f || groundLength <= 0f) { return; }

            // 時刻 t の音は、奥行き (t + オフセット) × 倍率 の所 (再生の線の下で鳴るのと同じ所: CameraController)
            float bucketSeconds = 1f / BUCKETS_PER_SECOND;
            int first = Mathf.Max(0, Mathf.FloorToInt(-offsetSeconds / bucketSeconds));
            int last = Mathf.Min(envelope.Length - 1, Mathf.CeilToInt((groundLength / scale - offsetSeconds) / bucketSeconds));
            if (last - first < 1) { return; }

            int count = last - first + 1;
            var vertices = new Vector3[count * 2];
            var colors = new Color[count * 2];
            var triangles = new int[(count - 1) * 6];

            for (int i = 0; i < count; i++)
            {
                int bucket = first + i;
                float z = Mathf.Clamp((bucket * bucketSeconds + offsetSeconds) * scale, 0f, groundLength);
                float width = envelope[bucket] * halfWidth;
                vertices[i * 2] = new Vector3(-width, 0f, z);
                vertices[i * 2 + 1] = new Vector3(width, 0f, z);
                colors[i * 2] = color;
                colors[i * 2 + 1] = color;
            }
            for (int i = 0; i < count - 1; i++)
            {
                int v = i * 2;
                int t = i * 6;
                triangles[t] = v;
                triangles[t + 1] = v + 2;
                triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1;
                triangles[t + 4] = v + 2;
                triangles[t + 5] = v + 3;
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
        }
    }
}
