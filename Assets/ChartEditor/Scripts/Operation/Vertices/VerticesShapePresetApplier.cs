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

            if (!JsonLoader.TryLoadFromJsonFile(filePath, out VerticesShapePresetCollection collection) ||
                collection?.presets == null)
            {
                Debug.LogWarning($"[VerticesPreset] Failed to load presets: {filePath}");
                NotifyPresetsChanged();
                return;
            }

            presets.AddRange(collection.presets.Where(IsValidPreset));
            NotifyPresetsChanged();
        }

        bool SavePresets()
        {
            var collection = new VerticesShapePresetCollection
            {
                presets = presets
                    .Where(IsValidPreset)
                    .Select(x => x.Clone())
                    .ToList()
            };

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

    [Serializable]
    class VerticesShapePresetCollection
    {
        public List<VerticesShapePreset> presets = new();
    }
}
