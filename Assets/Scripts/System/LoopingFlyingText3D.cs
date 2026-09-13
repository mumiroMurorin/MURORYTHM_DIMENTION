using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityFx.Outline;

public sealed class LoopingFlyingText3D : MonoBehaviour
{
    struct FadeProperty
    {
        public int PropertyId;
        public bool IsColor;
        public Color BaseColor;
        public float BaseAlpha;
    }

    sealed class Glyph
    {
        public GameObject Object;
        public Renderer[] Renderers;
        public Material[][] Materials;
        public FadeProperty[][] FadeProperties;
        public OutlineBehaviour Outline;
        public Color OutlineColor;
        public float VisualMinX;
        public float Width;
    }

    readonly List<Glyph> glyphs = new List<Glyph>();

    CharacterSpawner characterSpawner;
    FlyingTextSettings textSettings;
    int visibleCharacterCount;
    float additionalCharacterSpacing;
    float loopInterval;
    float transitionDuration;
    float transitionYOffset;
    OutlineSettings outlineSettings;
    Color? outlineColor;
    int windowStart;
    Coroutine loopCoroutine;

    public void Configure(
        CharacterSpawner spawner,
        FlyingTextSettings settings,
        int visibleCount,
        float characterSpacing,
        float interval,
        float duration,
        float yOffset,
        OutlineSettings outline,
        Color? color)
    {
        characterSpawner = spawner;
        textSettings = settings;
        visibleCharacterCount = Mathf.Max(1, visibleCount);
        additionalCharacterSpacing = Mathf.Max(0f, characterSpacing);
        loopInterval = Mathf.Max(0f, interval);
        transitionDuration = Mathf.Max(0.01f, duration);
        transitionYOffset = Mathf.Abs(yOffset);
        outlineSettings = outline;
        outlineColor = color;
    }

    public void SetText(string text)
    {
        StopLoop();
        ClearGlyphs();

        if (characterSpawner == null)
        {
            Debug.LogWarning($"[{nameof(LoopingFlyingText3D)}] CharacterSpawner is not set.", this);
            return;
        }

        foreach (string element in EnumerateTextElements(text ?? string.Empty))
        {
            glyphs.Add(CreateGlyph(element));
        }

        windowStart = 0;
        ApplyWindow();
        if (glyphs.Count > visibleCharacterCount && isActiveAndEnabled)
        {
            loopCoroutine = StartCoroutine(Loop());
        }
    }

    void OnDisable()
    {
        StopLoop();
    }

    void OnEnable()
    {
        if (glyphs.Count > visibleCharacterCount && loopCoroutine == null)
        {
            ApplyWindow();
            loopCoroutine = StartCoroutine(Loop());
        }
    }

    void OnDestroy()
    {
        ClearGlyphs();
    }

    IEnumerator Loop()
    {
        while (true)
        {
            if (loopInterval > 0f)
            {
                yield return new WaitForSeconds(loopInterval);
            }

            yield return AnimateNextCharacter();
        }
    }

    IEnumerator AnimateNextCharacter()
    {
        int outgoingIndex = windowStart % glyphs.Count;
        int incomingIndex = (windowStart + visibleCharacterCount) % glyphs.Count;
        Glyph outgoing = glyphs[outgoingIndex];
        Glyph incoming = glyphs[incomingIndex];
        Vector3 outgoingStart = outgoing.Object.transform.localPosition;
        float[] nextPositions = GetWindowPositions((windowStart + 1) % glyphs.Count, visibleCharacterCount);

        incoming.Object.SetActive(true);
        incoming.Object.transform.localPosition = new Vector3(
            nextPositions[visibleCharacterCount - 1],
            -transitionYOffset,
            0f);
        SetAlpha(incoming, 0f);

        Vector3[] movingStarts = new Vector3[visibleCharacterCount - 1];
        for (int slot = 1; slot < visibleCharacterCount; slot++)
        {
            movingStarts[slot - 1] = glyphs[(windowStart + slot) % glyphs.Count].Object.transform.localPosition;
        }

        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / transitionDuration);
            float eased = Mathf.SmoothStep(0f, 1f, t);

            for (int slot = 1; slot < visibleCharacterCount; slot++)
            {
                Glyph glyph = glyphs[(windowStart + slot) % glyphs.Count];
                float x = Mathf.Lerp(movingStarts[slot - 1].x, nextPositions[slot - 1], eased);
                glyph.Object.transform.localPosition = new Vector3(x, 0f, 0f);
            }

            outgoing.Object.transform.localPosition = new Vector3(
                Mathf.Lerp(outgoingStart.x, outgoingStart.x - GetAdvance(outgoing), eased),
                transitionYOffset * eased,
                0f);
            SetAlpha(outgoing, 1f - eased);

            incoming.Object.transform.localPosition = new Vector3(
                nextPositions[visibleCharacterCount - 1],
                Mathf.Lerp(-transitionYOffset, 0f, eased),
                0f);
            SetAlpha(incoming, eased);
            yield return null;
        }

        windowStart = (windowStart + 1) % glyphs.Count;
        ApplyWindow();
    }

    Glyph CreateGlyph(string element)
    {
        GameObject glyphObject = characterSpawner.SpawnCharacter(element, textSettings);
        glyphObject.name = $"Glyph_{glyphs.Count}_{element}";
        glyphObject.transform.SetParent(transform, false);

        Renderer[] renderers = glyphObject.GetComponentsInChildren<Renderer>(true);
        Material[][] rendererMaterials = new Material[renderers.Length][];
        FadeProperty[][] fadeProperties = new FadeProperty[renderers.Length][];
        for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
        {
            Material[] materials = renderers[rendererIndex].materials;
            rendererMaterials[rendererIndex] = materials;
            fadeProperties[rendererIndex] = new FadeProperty[materials.Length];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                fadeProperties[rendererIndex][materialIndex] = FindFadeProperty(materials[materialIndex]);
            }
        }

        GetVisualXBounds(glyphObject, renderers, out float visualMinX, out float width);

        Color appliedOutlineColor = outlineColor
            ?? (outlineSettings != null ? outlineSettings.Color : Color.clear);
        OutlineBehaviour outline = outlineSettings != null
            ? outlineSettings.ApplyOutline(glyphObject, appliedOutlineColor)
            : null;

        return new Glyph
        {
            Object = glyphObject,
            Renderers = renderers,
            Materials = rendererMaterials,
            FadeProperties = fadeProperties,
            Outline = outline,
            OutlineColor = appliedOutlineColor,
            VisualMinX = visualMinX,
            Width = width
        };
    }

    void ApplyWindow()
    {
        for (int i = 0; i < glyphs.Count; i++)
        {
            glyphs[i].Object.SetActive(false);
        }

        int count = Mathf.Min(visibleCharacterCount, glyphs.Count);
        float[] positions = GetWindowPositions(windowStart, count);
        for (int slot = 0; slot < count; slot++)
        {
            Glyph glyph = glyphs[(windowStart + slot) % glyphs.Count];
            glyph.Object.SetActive(true);
            glyph.Object.transform.localPosition = new Vector3(positions[slot], 0f, 0f);
            SetAlpha(glyph, 1f);
        }
    }

    float[] GetWindowPositions(int start, int count)
    {
        float[] positions = new float[count];
        float totalWidth = 0f;
        for (int slot = 0; slot < count; slot++)
        {
            totalWidth += glyphs[(start + slot) % glyphs.Count].Width;
        }
        totalWidth += additionalCharacterSpacing * Mathf.Max(0, count - 1);

        float cursor = GetAlignedLeftEdge(totalWidth);
        for (int slot = 0; slot < count; slot++)
        {
            Glyph glyph = glyphs[(start + slot) % glyphs.Count];
            positions[slot] = cursor - glyph.VisualMinX;
            cursor += GetAdvance(glyph);
        }

        return positions;
    }

    float GetAdvance(Glyph glyph)
    {
        return Mathf.Max(0.01f, glyph.Width + additionalCharacterSpacing);
    }

    float GetAlignedLeftEdge(float totalWidth)
    {
        if (textSettings == null) { return 0f; }

        switch (textSettings.TextAnchor)
        {
            case TextAnchor.UpperCenter:
            case TextAnchor.MiddleCenter:
            case TextAnchor.LowerCenter:
                return -totalWidth * 0.5f;
            case TextAnchor.UpperRight:
            case TextAnchor.MiddleRight:
            case TextAnchor.LowerRight:
                return -totalWidth;
            default:
                return 0f;
        }
    }

    void GetVisualXBounds(GameObject root, Renderer[] renderers, out float minX, out float width)
    {
        minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;

        foreach (Renderer renderer in renderers)
        {
            Bounds bounds = renderer.localBounds;
            Vector3 min = bounds.min;
            Vector3 max = bounds.max;
            for (int x = 0; x < 2; x++)
            {
                for (int y = 0; y < 2; y++)
                {
                    for (int z = 0; z < 2; z++)
                    {
                        Vector3 corner = new Vector3(
                            x == 0 ? min.x : max.x,
                            y == 0 ? min.y : max.y,
                            z == 0 ? min.z : max.z);
                        float localX = root.transform.InverseTransformPoint(
                            renderer.transform.TransformPoint(corner)).x;
                        minX = Mathf.Min(minX, localX);
                        maxX = Mathf.Max(maxX, localX);
                    }
                }
            }
        }

        if (float.IsInfinity(minX) || maxX - minX <= Mathf.Epsilon)
        {
            minX = 0f;
            width = textSettings != null ? Mathf.Max(0.01f, textSettings.DefaultSize * 0.5f) : 1f;
            return;
        }

        width = maxX - minX;
    }

    static IEnumerable<string> EnumerateTextElements(string text)
    {
        TextElementEnumerator enumerator = StringInfo.GetTextElementEnumerator(text);
        while (enumerator.MoveNext())
        {
            yield return enumerator.GetTextElement();
        }
    }

    static FadeProperty FindFadeProperty(Material material)
    {
        if (material == null)
        {
            return new FadeProperty { PropertyId = -1 };
        }

        int alphaId = FindProperty(material, "_Alpha", "_alpha");
        if (alphaId >= 0)
        {
            return new FadeProperty
            {
                PropertyId = alphaId,
                BaseAlpha = material.GetFloat(alphaId)
            };
        }

        int colorId = FindProperty(material, "_Color", "_color");
        return colorId >= 0
            ? new FadeProperty
            {
                PropertyId = colorId,
                IsColor = true,
                BaseColor = material.GetColor(colorId)
            }
            : new FadeProperty { PropertyId = -1 };
    }

    static int FindProperty(Material material, params string[] propertyNames)
    {
        foreach (string propertyName in propertyNames)
        {
            int propertyId = Shader.PropertyToID(propertyName);
            if (material.HasProperty(propertyId)) { return propertyId; }
        }

        return -1;
    }

    static void SetAlpha(Glyph glyph, float alpha)
    {
        if (glyph.Outline != null)
        {
            Color color = glyph.OutlineColor;
            color.a *= alpha;
            glyph.Outline.OutlineColor = color;
        }

        for (int rendererIndex = 0; rendererIndex < glyph.Renderers.Length; rendererIndex++)
        {
            Material[] materials = glyph.Materials[rendererIndex];
            for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
            {
                Material material = materials[materialIndex];
                FadeProperty property = glyph.FadeProperties[rendererIndex][materialIndex];
                if (material == null || property.PropertyId < 0) { continue; }

                if (property.IsColor)
                {
                    Color color = property.BaseColor;
                    color.a *= alpha;
                    material.SetColor(property.PropertyId, color);
                }
                else
                {
                    material.SetFloat(property.PropertyId, property.BaseAlpha * alpha);
                }
            }
        }

    }

    void StopLoop()
    {
        if (loopCoroutine == null) { return; }
        StopCoroutine(loopCoroutine);
        loopCoroutine = null;
    }

    void ClearGlyphs()
    {
        foreach (Glyph glyph in glyphs)
        {
            if (glyph.Materials != null)
            {
                foreach (Material[] materials in glyph.Materials)
                {
                    foreach (Material material in materials)
                    {
                        if (material != null) { Destroy(material); }
                    }
                }
            }

            if (glyph.Object != null) { Destroy(glyph.Object); }
        }
        glyphs.Clear();
    }
}
