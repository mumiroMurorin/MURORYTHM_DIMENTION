using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class FragmentsBomb : MonoBehaviour
{
    [Header("îöî≠ä÷åW")]
    [SerializeField] float minForce;
    [SerializeField] float maxForce;
    [SerializeField] Vector3 center;
    [SerializeField] float radius;
    [SerializeField] float upwards;

    [Header("çÌèúÇ‹Ç≈ÇÃéûä‘")]
    [SerializeField] float lifeTime;

    List<Rigidbody> childrenRb = new List<Rigidbody>();

    public void Initialize()
    {
        foreach(Transform child in this.gameObject.transform)
        {
            if(!child.TryGetComponent(out Rigidbody rb)) 
            {
                rb = child.gameObject.AddComponent<Rigidbody>();
            }

            childrenRb.Add(rb);
        }
    }

    public void Explosion()
    {
        this.gameObject.SetActive(true);

        foreach (var rb in childrenRb)
        {
            var force = Random.Range(minForce, maxForce);
            rb.AddExplosionForce(force, center, radius, upwards, ForceMode.Impulse);

            rb.transform.DOScale(Vector3.zero, lifeTime)
                .SetEase(Ease.InExpo)
                .OnComplete(() =>
                {
                    Destroy(gameObject);
                });
        }
    }

    public void Explosion(Vector3 center)
    {
        this.center = center;
        Explosion();
    }
}
