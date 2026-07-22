using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpaceHoldBulletInteractEffectController : MonoBehaviour, IInteractNoteEffectController
{
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField] float bulletLifeTime = 1.5f;
    [SerializeField] Vector3 fireDirectionLocal = Vector3.forward;
    [SerializeField] bool detachOnFire = true;
    [SerializeField] Animator animator;
    [SerializeField] ParticleSystem chargeParticle;
    [SerializeField] string spawnTriggerName = "Spawn";
    [SerializeField] string chargeStartTriggerName = "ChargeStart";
    [SerializeField] string chargeEndTriggerName = "ChargeEnd";
    [SerializeField] string fireTriggerName = "Fire";
    [SerializeField] Transform widthScaleTarget;
    [SerializeField] float widthScaleMultiplier = 1f;
    [SerializeField] float minWidthScale = 0f;
    [SerializeField] float maxWidthScale = 100f;

    static readonly Dictionary<int, BulletGroup> holdNumberToGroup = new Dictionary<int, BulletGroup>();

    Action<IInteractNoteEffectController> returnToPool;
    Transform poolParent;
    int holdNumber;
    bool isWaitingFire;
    bool isReturnedToPool;
    bool hasChargeTiming;
    bool isChargeStarted;
    bool isChargeEnded;
    float chargeStartTiming;
    float chargeEndTiming;

    private void Awake()
    {
        poolParent = transform.parent;
        gameObject.SetActive(false);
    }

    public void SetEffect(INoteData noteData, Judgement judgement, Action<IInteractNoteEffectController> returnToPool)
    {
        this.returnToPool = returnToPool;
        if (transform.parent != null)
        {
            poolParent = transform.parent;
        }

        isWaitingFire = false;
        isReturnedToPool = false;
        ReturnToPool();
    }

    public void SetTransform(Vector3 pos, Quaternion rotation)
    {
    }

    public void Play()
    {
        if (!isWaitingFire) { return; }

        gameObject.SetActive(true);
        SetAnimatorTrigger(spawnTriggerName);
    }

    public bool InitializeWaitingBullet(int holdNumber, Vector3 localPosition, Action<IInteractNoteEffectController> returnToPool = null)
    {
        this.returnToPool = returnToPool;
        if (transform.parent != null)
        {
            poolParent = transform.parent;
        }

        this.holdNumber = holdNumber;
        transform.localPosition = localPosition;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        BulletGroup group = GetOrCreateGroup(holdNumber);
        group.Bullets.Add(this);
        isWaitingFire = true;
        isReturnedToPool = false;
        return true;
    }

    public void SetChargeTiming(float startTiming, float endTiming)
    {
        hasChargeTiming = endTiming >= startTiming;
        chargeStartTiming = startTiming;
        chargeEndTiming = endTiming;
        isChargeStarted = false;
        isChargeEnded = false;
    }

    public void SetChargeParticleDuration(float duration)
    {
        if (chargeParticle == null) { return; }

        ParticleSystem.MainModule main = chargeParticle.main;
        main.duration = Mathf.Max(0.01f, duration);
    }

    public void UpdateCharge(float currentTime)
    {
        if (!hasChargeTiming) { return; }

        if (!isChargeStarted && currentTime >= chargeStartTiming)
        {
            SetAnimatorTrigger(chargeStartTriggerName);
            isChargeStarted = true;
        }

        if (!isChargeEnded && currentTime >= chargeEndTiming)
        {
            SetAnimatorTrigger(chargeEndTriggerName);
            isChargeEnded = true;
        }
    }

    public void SetWidth(float width)
    {
        float scaledWidth = ClampWidth(width * widthScaleMultiplier, minWidthScale, maxWidthScale);

        if (widthScaleTarget != null)
        {
            Vector3 scale = widthScaleTarget.localScale;
            scale.x = scaledWidth;
            widthScaleTarget.localScale = scale;
        }

    }

    private float ClampWidth(float width, float min, float max)
    {
        if (max < min) { max = min; }

        return Mathf.Clamp(width, min, max);
    }

    public static void ClearWaitingBullets(int holdNumber)
    {
        if (!holdNumberToGroup.TryGetValue(holdNumber, out BulletGroup group)) { return; }

        List<SpaceHoldBulletInteractEffectController> bullets = new List<SpaceHoldBulletInteractEffectController>(group.Bullets);
        holdNumberToGroup.Remove(holdNumber);

        foreach (SpaceHoldBulletInteractEffectController bullet in bullets)
        {
            if (bullet == null) { continue; }

            bullet.ReturnToPool();
        }
    }

    public static void FireBullets(int holdNumber, float fireDelayInterval)
    {
        if (!holdNumberToGroup.TryGetValue(holdNumber, out BulletGroup group)) { return; }

        List<SpaceHoldBulletInteractEffectController> bullets = new List<SpaceHoldBulletInteractEffectController>(group.Bullets);
        holdNumberToGroup.Remove(holdNumber);

        for (int i = 0; i < bullets.Count; i++)
        {
            SpaceHoldBulletInteractEffectController bullet = bullets[i];
            if (bullet == null) { continue; }

            bullet.Fire(i * fireDelayInterval);
        }
    }

    private void Fire(float delay)
    {
        if (!isWaitingFire) { return; }

        isWaitingFire = false;
        gameObject.SetActive(true);
        StartCoroutine(FireCoroutine(delay));
    }

    private IEnumerator FireCoroutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        Vector3 direction = GetFireDirection();
        SetAnimatorTrigger(fireTriggerName);

        if (detachOnFire)
        {
            transform.SetParent(null, true);
        }

        float elapsed = 0f;
        while (elapsed < bulletLifeTime)
        {
            float deltaTime = Time.deltaTime;
            transform.position += direction * bulletSpeed * deltaTime;
            elapsed += deltaTime;
            yield return null;
        }

        ReturnToPool();
    }

    private Vector3 GetFireDirection()
    {
        Vector3 localDirection = fireDirectionLocal.sqrMagnitude <= 0f ? Vector3.forward : fireDirectionLocal.normalized;
        Transform parent = transform.parent;

        return parent != null ? parent.TransformDirection(localDirection).normalized : localDirection;
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (string.IsNullOrEmpty(triggerName)) { return; }

        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }

        animator?.SetTrigger(triggerName);
    }

    private static BulletGroup GetOrCreateGroup(int holdNumber)
    {
        if (!holdNumberToGroup.TryGetValue(holdNumber, out BulletGroup group))
        {
            group = new BulletGroup();
            holdNumberToGroup.Add(holdNumber, group);
        }

        return group;
    }

    private void ReturnToPool()
    {
        if (isReturnedToPool) { return; }

        isReturnedToPool = true;
        isWaitingFire = false;
        RemoveFromGroup();
        gameObject.SetActive(false);

        if (poolParent != null)
        {
            transform.SetParent(poolParent, false);
        }

        returnToPool?.Invoke(this);
    }

    private void RemoveFromGroup()
    {
        if (!holdNumberToGroup.TryGetValue(holdNumber, out BulletGroup group)) { return; }

        group.Bullets.Remove(this);
        if (group.Bullets.Count <= 0)
        {
            holdNumberToGroup.Remove(holdNumber);
        }
    }

    class BulletGroup
    {
        public readonly List<SpaceHoldBulletInteractEffectController> Bullets = new List<SpaceHoldBulletInteractEffectController>();
    }

    public void PlayChargeParticle()
    {
        chargeParticle?.Play();
    }

    public void StopChargeParticle()
    {
        chargeParticle?.Stop();
    }
}
