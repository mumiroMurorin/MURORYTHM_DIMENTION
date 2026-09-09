using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

public class WorldTextParticleSpawner : MonoBehaviour
{
    [System.Serializable]
    public class WeightedSprite
    {
        public Sprite sprite;
        [Min(0f)]
        public float weight = 1f;
    }

    [System.Serializable]
    public class WeightedText
    {
        [TextArea(2, 5)]
        public string text;
        [Min(0f)]
        public float weight = 1f;
    }

    struct SpawnContent
    {
        public Sprite sprite;
        public string text;
        public bool useText;
    }

    [Header("Image")]
    [SerializeField] WorldTextParticleItem itemPrefab;
    [SerializeField] WeightedSprite[] sprites;
    [SerializeField] Color spriteColor = Color.white;
    [FormerlySerializedAs("scaleRange")]
    [SerializeField] Vector2 textureScaleRange = new Vector2(1f, 1f);
    [SerializeField] bool useSolidSpriteColor;
    [SerializeField] Material solidSpriteColorMaterial;

    [Header("Text")]
    [SerializeField] WeightedText[] texts;
    [SerializeField] TMP_FontAsset textFont;
    [SerializeField] Color textColor = Color.white;
    [SerializeField] Vector2 fontSizeRange = new Vector2(24f, 36f);
    [SerializeField] Vector2 textScaleRange = new Vector2(1f, 1f);

    [Header("Render")]
    [SerializeField] int renderQueue = -1;
    [SerializeField] string sortingLayerName;
    [SerializeField] int sortingOrder;

    [Header("Spawn")]
    [SerializeField] Transform spawnParent;
    [SerializeField] Vector3 spawnCenterLocal;
    [SerializeField] Vector3 spawnRange = new Vector3(100f, 100f, 0f);
    [SerializeField] bool useExcludeRect;
    [SerializeField] Vector2 excludeRectPointA;
    [SerializeField] Vector2 excludeRectPointB;
    [SerializeField] int maxSpawnPositionRetryCount = 20;
    [SerializeField] Vector2 spawnIntervalRange = new Vector2(0.1f, 0.3f);
    [SerializeField] int initialPoolSize = 32;
    [SerializeField] int maxActiveCount = 64;
    [SerializeField] bool playOnStart = true;

    [Header("Move")]
    [SerializeField] Vector3 moveDirectionLocal = Vector3.up;
    [SerializeField] Vector2 moveSpeedRange = new Vector2(40f, 80f);
    [SerializeField] Vector2 lifeTimeRange = new Vector2(1.5f, 3f);
    [SerializeField] bool useUnscaledTime;

    [Header("Fade")]
    [SerializeField] float fadeInDuration = 0.2f;
    [SerializeField] float fadeOutDuration = 0.5f;

    readonly Stack<WorldTextParticleItem> pool = new Stack<WorldTextParticleItem>();
    readonly List<WorldTextParticleItem> activeItems = new List<WorldTextParticleItem>();

    Material runtimeSolidSpriteColorMaterial;
    float spawnTimer;
    bool isPlaying;

    private void Awake()
    {
        if (spawnParent == null)
        {
            spawnParent = transform;
        }

        InitializePool();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!isPlaying) { return; }
        if (!HasSpawnContent()) { return; }
        if (spawnParent == null) { return; }

        spawnTimer -= useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        if (spawnTimer > 0f) { return; }

        Spawn();
        ResetSpawnTimer();
    }

    public void Play()
    {
        isPlaying = true;
        ResetSpawnTimer();
    }

    public void Stop()
    {
        isPlaying = false;
    }

    public void Clear()
    {
        for (int i = activeItems.Count - 1; i >= 0; i--)
        {
            if (activeItems[i] == null) { continue; }

            activeItems[i].StopForPool();
            ReturnToPool(activeItems[i]);
        }

        activeItems.Clear();
    }

    void InitializePool()
    {
        int count = Mathf.Max(0, initialPoolSize);
        for (int i = 0; i < count; i++)
        {
            pool.Push(CreateItem());
        }
    }

    void Spawn()
    {
        if (activeItems.Count >= maxActiveCount) { return; }

        SpawnContent content = GetRandomContent();
        if (!content.useText && content.sprite == null) { return; }
        if (content.useText && string.IsNullOrEmpty(content.text)) { return; }

        WorldTextParticleItem item = GetItem();
        if (item == null) { return; }

        if (!TryGetSpawnLocalPosition(out Vector3 localPosition)) { return; }

        float moveSpeed = Random.Range(moveSpeedRange.x, moveSpeedRange.y);
        float lifeTime = Random.Range(lifeTimeRange.x, lifeTimeRange.y);
        float localScale = content.useText
            ? Random.Range(textScaleRange.x, textScaleRange.y)
            : Random.Range(textureScaleRange.x, textureScaleRange.y);
        float fontSize = Random.Range(fontSizeRange.x, fontSizeRange.y);

        activeItems.Add(item);
        item.Initialize(
            content.sprite,
            content.text,
            content.useText,
            localPosition,
            moveDirectionLocal,
            moveSpeed,
            lifeTime,
            fadeInDuration,
            fadeOutDuration,
            content.useText ? textColor : spriteColor,
            content.useText ? textFont : null,
            fontSize,
            localScale,
            content.useText ? null : GetSpriteMaterial(),
            renderQueue,
            sortingLayerName,
            sortingOrder,
            useUnscaledTime,
            ReturnToPool);
    }

    bool TryGetSpawnLocalPosition(out Vector3 localPosition)
    {
        int retryCount = Mathf.Max(1, maxSpawnPositionRetryCount);
        for (int i = 0; i < retryCount; i++)
        {
            localPosition = spawnCenterLocal + new Vector3(
                Random.Range(-spawnRange.x * 0.5f, spawnRange.x * 0.5f),
                Random.Range(-spawnRange.y * 0.5f, spawnRange.y * 0.5f),
                Random.Range(-spawnRange.z * 0.5f, spawnRange.z * 0.5f));

            if (!IsInExcludeRect(localPosition)) { return true; }
        }

        localPosition = default;
        return false;
    }

    bool IsInExcludeRect(Vector3 localPosition)
    {
        if (!useExcludeRect) { return false; }

        float minX = Mathf.Min(excludeRectPointA.x, excludeRectPointB.x);
        float maxX = Mathf.Max(excludeRectPointA.x, excludeRectPointB.x);
        float minY = Mathf.Min(excludeRectPointA.y, excludeRectPointB.y);
        float maxY = Mathf.Max(excludeRectPointA.y, excludeRectPointB.y);

        return localPosition.x >= minX &&
               localPosition.x <= maxX &&
               localPosition.y >= minY &&
               localPosition.y <= maxY;
    }

    Material GetSpriteMaterial()
    {
        if (!useSolidSpriteColor) { return null; }
        if (solidSpriteColorMaterial != null) { return solidSpriteColorMaterial; }

        if (runtimeSolidSpriteColorMaterial == null)
        {
            Shader shader = Shader.Find("Custom/SpriteSolidColorTransparent");
            if (shader == null) { return null; }

            runtimeSolidSpriteColorMaterial = new Material(shader);
        }

        return runtimeSolidSpriteColorMaterial;
    }

    bool HasSpawnContent()
    {
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null && sprites[i].sprite != null && sprites[i].weight > 0f) { return true; }
            }
        }

        if (texts != null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && !string.IsNullOrEmpty(texts[i].text) && texts[i].weight > 0f) { return true; }
            }
        }

        return false;
    }

    SpawnContent GetRandomContent()
    {
        float totalWeight = 0f;
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null || sprites[i].sprite == null) { continue; }

                totalWeight += Mathf.Max(0f, sprites[i].weight);
            }
        }

        if (texts != null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null || string.IsNullOrEmpty(texts[i].text)) { continue; }

                totalWeight += Mathf.Max(0f, texts[i].weight);
            }
        }

        if (totalWeight <= 0f) { return default; }

        float value = Random.Range(0f, totalWeight);
        if (sprites != null)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null || sprites[i].sprite == null) { continue; }

                value -= Mathf.Max(0f, sprites[i].weight);
                if (value <= 0f)
                {
                    return new SpawnContent { sprite = sprites[i].sprite, useText = false };
                }
            }
        }

        if (texts != null)
        {
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] == null || string.IsNullOrEmpty(texts[i].text)) { continue; }

                value -= Mathf.Max(0f, texts[i].weight);
                if (value <= 0f)
                {
                    return new SpawnContent { text = texts[i].text, useText = true };
                }
            }
        }

        return default;
    }

    WorldTextParticleItem GetItem()
    {
        while (pool.Count > 0)
        {
            WorldTextParticleItem item = pool.Pop();
            if (item != null) { return item; }
        }

        return activeItems.Count < maxActiveCount ? CreateItem() : null;
    }

    WorldTextParticleItem CreateItem()
    {
        WorldTextParticleItem item = itemPrefab != null ? Instantiate(itemPrefab, spawnParent) : CreateDefaultItem();
        item.EnsureComponents();
        item.gameObject.SetActive(false);

        return item;
    }

    WorldTextParticleItem CreateDefaultItem()
    {
        GameObject obj = new GameObject("WorldParticle");
        obj.transform.SetParent(spawnParent, false);

        return obj.AddComponent<WorldTextParticleItem>();
    }

    void ReturnToPool(WorldTextParticleItem item)
    {
        if (item == null) { return; }

        activeItems.Remove(item);
        item.transform.SetParent(spawnParent, false);
        pool.Push(item);
    }

    void ResetSpawnTimer()
    {
        float min = Mathf.Min(spawnIntervalRange.x, spawnIntervalRange.y);
        float max = Mathf.Max(spawnIntervalRange.x, spawnIntervalRange.y);
        spawnTimer = Random.Range(Mathf.Max(0.01f, min), Mathf.Max(0.01f, max));
    }

    private void OnDestroy()
    {
        if (runtimeSolidSpriteColorMaterial != null)
        {
            Destroy(runtimeSolidSpriteColorMaterial);
        }

        pool.Clear();
        activeItems.Clear();
    }
}
