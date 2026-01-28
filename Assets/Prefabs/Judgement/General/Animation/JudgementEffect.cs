using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class JudgementEffect : MonoBehaviour, IJudgementEffectController
{
    const string PLAY_ANIMATION_TAG = "Play";

    [SerializeField] JudgementToSprite[] sprites;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Animator anim;

    Camera mainCamera;
    Action<IJudgementEffectController> onFinishAnimation;

    private void Start()
    {
        mainCamera = Camera.main;
        this.gameObject.SetActive(false);
    }

    private void Update()
    {
        UpdateRotation();
    }

    private void UpdateRotation()
    {
        if (mainCamera == null) { return; }

        // ÉJÉÅÉâÇÃï˚å¸ÇåvéZ
        Vector3 directionToCamera = transform.position - mainCamera.transform.position;

        // Yé≤ÇÃâÒì]ÇÃÇ›Ççló∂
        directionToCamera.x = 0;

        Quaternion currentRotation = transform.rotation;

        // âÒì]ÇìKóp
        if (directionToCamera.sqrMagnitude > 0.01f) // í∑Ç≥Ç™ÇŸÇ⁄É[ÉçÇ≈Ç»Ç¢Ç©ämîF
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToCamera);
            transform.rotation = Quaternion.Euler(
                targetRotation.eulerAngles.x,
                targetRotation.eulerAngles.y,
                currentRotation.eulerAngles.z
            );
        }
    }


    public void SetEffect(Judgement judgement, Action<IJudgementEffectController> returnToPool, float error = 0f)
    {
        onFinishAnimation = returnToPool;

        ChangeSprite(judgement, error);
    }

    private void ChangeSprite(Judgement judgement, float error)
    {
        if (spriteRenderer == null) { return; }

        foreach (var s in sprites)
        {
            if (s.CheckCondition(judgement, error)) 
            {
                s.ApplySprite(spriteRenderer);
            }
        }
    }

    public void SetTransform(Vector3 pos, Quaternion rotation)
    {
        this.gameObject.transform.position = pos;
        this.gameObject.transform.rotation = rotation;
    }

    public void Play()
    {
        this.gameObject.SetActive(true);

        anim.SetTrigger(PLAY_ANIMATION_TAG);
    }

    public void OnFinishAnimation()
    {
        this.gameObject.SetActive(false);
        onFinishAnimation?.Invoke(this);
    }


    [System.Serializable]
    class JudgementToSprite
    {
        [SerializeField] Judgement judgement;
        [SerializeField] FastLate fastLate;
        [SerializeField] Sprite sprite;

        public bool CheckCondition(Judgement judgement, float error = 0f)
        {
            // FastLateï\é¶ñ≥ÇµÇÃÇ∆Ç´
            if (error == 0f && fastLate == FastLate.None) { return this.judgement == judgement; }
            else if (error == 0f) { return false; }

            if (error < 0f) { return this.judgement == judgement && fastLate == FastLate.Fast; }
            if (error > 0f) { return this.judgement == judgement && fastLate == FastLate.Late; }

            return false;
        }

        public void ApplySprite(SpriteRenderer spriteRenderer)
        {
            if (spriteRenderer == null) { return; }
            spriteRenderer.sprite = this.sprite;
        }

        enum FastLate
        {
            None,
            Fast,
            Late
        }
    }
}

