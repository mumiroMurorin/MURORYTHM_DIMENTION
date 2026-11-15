using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using JsonUtil;
using UnityEngine;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 形のプリセット (スペース系のノーツの頂点の並びを、名前を付けて取っておいたもの) の登録・上書き・削除・適用
    ///
    /// プリセットは、1つのファイルに保存する (chartEditorVerticesShapePresets.json)。ファイルの形は VerticesShapePresetFile。
    /// </summary>
    public class VerticesShapePresetApplier : MonoBehaviour
    {
        const string FILE_NAME = "chartEditorVerticesShapePresets.json";

        [SerializeField] MultiVertexSelector vertexSelector;

        INotesDataGetter notesGetter;
        IChartEditorDataGetter dataGetter;

        readonly List<VerticesShapePreset> presets = new();

        readonly EditMode[] ignoreEditModes = new EditMode[]
        {
            EditMode.VertexMoving,
            EditMode.VerticesRotating,
            EditMode.VerticesScaling,
            EditMode.Preview,
        };

        public event Action<IReadOnlyList<VerticesShapePreset>> OnPresetsChanged;

        public IReadOnlyList<VerticesShapePreset> Presets => presets;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        void Awake()
        {
            LoadPresets();
        }

        public bool RegisterCurrentVerticesAsPreset()
        {
            if (!TryGetCurrentVertices(out var vertices)) { return false; }

            var preset = new VerticesShapePreset(
                Guid.NewGuid().ToString("N"),
                $"Preset {GetNextPresetNumber()}",
                vertices);

            presets.Add(preset);

            if (!SavePresets())
            {
                presets.Remove(preset);
                return false;
            }

            NotifyPresetsChanged();
            return true;
        }

        public bool RemovePreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId)) { return false; }

            int removedCount = presets.RemoveAll(x => x != null && x.Id == presetId);
            if (removedCount <= 0) { return false; }

            if (!SavePresets())
            {
                LoadPresets();
                return false;
            }

            NotifyPresetsChanged();
            return true;
        }

        public bool OverwritePreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId)) { return false; }
            if (!TryGetCurrentVertices(out var vertices)) { return false; }

            var preset = presets.FirstOrDefault(x => x != null && x.Id == presetId);
            if (preset == null) { return false; }

            var previousVertices = preset.Vertices;
            preset.SetVertices(vertices);

            if (!SavePresets())
            {
                preset.SetVertices(previousVertices);
                return false;
            }

            NotifyPresetsChanged();
            return true;
        }

        public bool ApplyPreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId)) { return false; }

            var preset = presets.FirstOrDefault(x => x.Id == presetId);
            return preset != null && ApplyPreset(preset);
        }

        public bool ApplyPreset(VerticesShapePreset preset)
        {
            if (preset == null) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Vertices) { return false; }
            if (dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes)) { return false; }
            if (notesGetter.EditingVertices.Value == null) { return false; }
            if (preset.Vertices == null || preset.Vertices.Length < 3) { return false; }

            var target = notesGetter.EditingVertices.Value.SpaceVertices;
            var previousVertices = target.Vertices.Select(v => new VertexData(v)).ToList();
            var nextVertices = preset.Vertices.Select(v => new VertexData(v)).ToList();

            Record(() =>
            {
                ReplaceVertices(target, nextVertices);
            }, () =>
            {
                ReplaceVertices(target, previousVertices);
            });

            return true;
        }

        bool TryGetCurrentVertices(out Vector2[] vertices)
        {
            vertices = Array.Empty<Vector2>();

            var editingVertices = notesGetter?.EditingVertices?.Value;
            if (editingVertices == null) { return false; }

            var sourceVertices = editingVertices.SpaceVertices?.Vertices;
            if (sourceVertices == null || sourceVertices.Count < 3) { return false; }

            vertices = sourceVertices
                .Select(x => x.Position.Value)
                .ToArray();
            return true;
        }

        void ReplaceVertices(SpaceVertices target, List<VertexData> vertices)
        {
            vertexSelector?.DeselectAll();
            target.SetVertices(vertices.Select(v => v.Position.Value).ToArray());
        }

        void LoadPresets()
        {
            presets.Clear();

            string filePath = GetFilePath();
            if (!File.Exists(filePath))
            {
                NotifyPresetsChanged();
                return;
            }

            if (!JsonLoader.TryLoadFromJsonFile(filePath, out VerticesShapePresetFile.Collection collection) ||
                collection?.presets == null)
            {
                Debug.LogWarning($"[VerticesPreset] Failed to load presets: {filePath}");
                NotifyPresetsChanged();
                return;
            }

            presets.AddRange(VerticesShapePresetFile.ToPresets(collection).Where(IsValidPreset));
            NotifyPresetsChanged();

            RewriteOldFormatFile(filePath);
        }

        /// <summary>
        /// 以前の形のファイル (頂点1つごとに、normalized や magnitude まで書かれていた) なら、x と y だけの形で書き直す
        /// (元のファイルは「ファイル名.bak」として残す。書き直すのは1回だけで、次からは新しい形なので何もしない)
        /// </summary>
        void RewriteOldFormatFile(string filePath)
        {
            try
            {
                if (presets.Count == 0) { return; }
                if (!VerticesShapePresetFile.IsOldFormat(File.ReadAllText(filePath))) { return; }

                if (JsonWriter.TrySaveToJsonPath(VerticesShapePresetFile.ToCollection(presets.Where(IsValidPreset)), filePath, true))
                {
                    Debug.Log($"[VerticesPreset] プリセットのファイルを、x と y だけの形に書き直しました (元のファイルは .bak に残しています)\n{filePath}");
                }
            }
            catch (Exception ex)
            {
                // 書き直せなくても、読み込んだプリセットはそのまま使える
                Debug.LogWarning($"[VerticesPreset] Failed to rewrite the preset file: {ex.Message}");
            }
        }

        bool SavePresets()
        {
            var collection = VerticesShapePresetFile.ToCollection(presets.Where(IsValidPreset));

            return JsonWriter.TrySaveToJsonPath(collection, GetFilePath());
        }

        void NotifyPresetsChanged()
        {
            OnPresetsChanged?.Invoke(presets);
        }

        int GetNextPresetNumber()
        {
            int maxNumber = 0;

            foreach (var preset in presets)
            {
                if (string.IsNullOrWhiteSpace(preset?.DisplayName)) { continue; }
                if (!preset.DisplayName.StartsWith("Preset ")) { continue; }

                string numberText = preset.DisplayName.Substring("Preset ".Length);
                if (int.TryParse(numberText, out int number))
                {
                    maxNumber = Mathf.Max(maxNumber, number);
                }
            }

            return maxNumber + 1;
        }

        bool IsValidPreset(VerticesShapePreset preset)
        {
            return preset != null &&
                   !string.IsNullOrWhiteSpace(preset.Id) &&
                   preset.Vertices != null &&
                   preset.Vertices.Length >= 3;
        }

        string GetFilePath()
        {
            return Path.Combine(Application.persistentDataPath, FILE_NAME);
        }
    }

    [Serializable]
    public class VerticesShapePreset
    {
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] Vector2[] vertices;

        public string Id => id;
        public string DisplayName => displayName;
        public Vector2[] Vertices => vertices;

        public VerticesShapePreset(string id, string displayName, Vector2[] vertices)
        {
            this.id = id;
            this.displayName = displayName;
            this.vertices = vertices ?? Array.Empty<Vector2>();
        }

        public VerticesShapePreset Clone()
        {
            var copiedVertices = new Vector2[vertices?.Length ?? 0];
            if (vertices != null)
            {
                Array.Copy(vertices, copiedVertices, copiedVertices.Length);
            }

            return new VerticesShapePreset(id, displayName, copiedVertices);
        }

        public void SetVertices(Vector2[] nextVertices)
        {
            vertices = nextVertices ?? Array.Empty<Vector2>();
        }
    }

    /// <summary>
    /// 形のプリセットを保存するファイルの形 (chartEditorVerticesShapePresets.json)
    ///
    /// 頂点は、x と y だけを持つ SimpleVector2 で保存する (譜面ファイルの頂点と同じ形)。
    /// Unity の Vector2 のまま保存すると、x と y のほかに normalized・magnitude・sqrMagnitude まで書き出され、
    /// normalized の中にまた同じものが入るので、ファイルが4倍ほどの大きさになる。以前はそうなっていた。
    /// 以前の形のファイルも、そのまま読める (x と y 以外の項目は、読み飛ばされる)。
    /// 編集中に使う VerticesShapePreset (頂点は Vector2) とは、ここで相互に変換する。
    /// </summary>
    public static class VerticesShapePresetFile
    {
        /// <summary>ファイル全体</summary>
        [Serializable]
        public class Collection
        {
            public List<Preset> presets = new();
        }

        /// <summary>
        /// プリセット1つぶん
        /// (項目の名前は、以前のファイルと同じにしてある。変えると、今あるファイルを読めなくなる)
        /// </summary>
        [Serializable]
        public class Preset
        {
            public string Id;
            public string DisplayName;
            public SimpleVector2[] Vertices;
        }

        /// <summary>
        /// 編集中のプリセットを、保存する形にする
        /// </summary>
        public static Collection ToCollection(IEnumerable<VerticesShapePreset> presets)
        {
            var collection = new Collection();
            if (presets == null) { return collection; }

            foreach (var preset in presets)
            {
                if (preset == null) { continue; }

                var source = preset.Vertices ?? Array.Empty<Vector2>();
                var vertices = new SimpleVector2[source.Length];
                for (int i = 0; i < source.Length; i++) { vertices[i] = new SimpleVector2(source[i]); }

                collection.presets.Add(new Preset { Id = preset.Id, DisplayName = preset.DisplayName, Vertices = vertices });
            }

            return collection;
        }

        /// <summary>
        /// 読み込んだファイルの中身を、編集中に使う形にする (中身の無い項目は飛ばす)
        /// </summary>
        public static List<VerticesShapePreset> ToPresets(Collection collection)
        {
            var presets = new List<VerticesShapePreset>();
            if (collection?.presets == null) { return presets; }

            foreach (var preset in collection.presets)
            {
                if (preset == null) { continue; }

                var vertices = new List<Vector2>();
                if (preset.Vertices != null)
                {
                    foreach (var vertex in preset.Vertices)
                    {
                        if (vertex != null) { vertices.Add(vertex.ToVector2()); }
                    }
                }

                presets.Add(new VerticesShapePreset(preset.Id, preset.DisplayName, vertices.ToArray()));
            }

            return presets;
        }

        /// <summary>
        /// ファイルの中身が、以前の形か (頂点に、x と y 以外の項目が書かれている)
        /// </summary>
        public static bool IsOldFormat(string json)
        {
            return !string.IsNullOrEmpty(json) && json.Contains("\"normalized\"");
        }
    }
}
