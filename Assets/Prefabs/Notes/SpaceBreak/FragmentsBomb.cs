using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using System.Threading;

public class FragmentsBomb : MonoBehaviour
{
    [Header("îöî≠ä÷åW")]
    [SerializeField] float startSize = 0.75f;
    [SerializeField] float minForce;
    [SerializeField] float maxForce;
    [SerializeField] Vector3 center;
    [SerializeField] float radius;
    [SerializeField] float upwards;
    [SerializeField] float extraGravityMultiplier = 8f;
    [SerializeField] bool applyExtraGravity = true;

    [Header("çÌèúÇ‹Ç≈ÇÃéûä‘")]
    [SerializeField] float lifeTime;

    List<Rigidbody> childrenRb = new List<Rigidbody>();
    int remainingChildren;
    bool isParentDestroyed;

    public void Initialize()
    {
        childrenRb.Clear();

        foreach (Transform child in this.gameObject.transform)
        {
            if (!child.TryGetComponent(out Rigidbody rb))
            {
                rb = child.gameObject.AddComponent<Rigidbody>();
            }

            child.localScale = Vector3.one * startSize;
            childrenRb.Add(rb);
        }

        remainingChildren = childrenRb.Count;
    }

    public void Explosion()
    {
        this.gameObject.SetActive(true);

        foreach (var rb in childrenRb)
        {
            if (rb == null) { continue; }

            var force = Random.Range(minForce, maxForce);
            rb.AddExplosionForce(force, center, radius, upwards, ForceMode.Impulse);
            rb.useGravity = true;

            if (applyExtraGravity)
            {
                ApplyExtraGravity(rb, this.GetCancellationTokenOnDestroy()).Forget();
            }

            var target = rb.gameObject;
            rb.transform.DOScale(Vector3.zero, lifeTime)
                .SetEase(Ease.InExpo)
                .OnComplete(() =>
                {
                    if (target != null)
                    {
                        Destroy(target);
                    }

                    remainingChildren--;
                    TryDestroyParent();
                });
        }
    }

    public void Explosion(Vector3 center)
    {
        this.center = center;
        Explosion();
    }

    private async UniTaskVoid ApplyExtraGravity(Rigidbody rb, CancellationToken token)
    {
        while (rb != null && !isParentDestroyed && !token.IsCancellationRequested)
        {
            rb.AddForce(Physics.gravity * extraGravityMultiplier, ForceMode.Acceleration);
            await UniTask.WaitForFixedUpdate(token);
        }
    }

    private void TryDestroyParent()
    {
        if (isParentDestroyed) { return; }
        if (remainingChildren > 0) { return; }

        isParentDestroyed = true;
        Destroy(gameObject);
    }
}