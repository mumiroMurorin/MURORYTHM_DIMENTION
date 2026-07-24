using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class InteractNoteEffectController_HoldEndGroundFly : MonoBehaviour, IInteractNoteEffectController
{
    [Header("Lane Objects")]
    [SerializeField] GameObject[] laneObjects = new GameObject[16];

    [Header("Move")]
    [SerializeField] float flySpeed = 80f;
    [SerializeField] float flyDuration = 1f;
    [SerializeField] float defaultCurveRadius = 2000f;
    [SerializeField] bool useNoteCurveRadius = true;
    [SerializeField] bool rotateAlongGround = true;

    [Header("Life Cycle")]
    [SerializeField] bool disableOnStart = true;

    Action<IInteractNoteEffectController> returnToPool;
    CancellationTokenSource moveCancellation;
    NoteData_HoldEnd holdEndData;
    Judgement judgement;
    Vector3 startLocalPosition;
    Quaternion startLocalRotation;
    float curveRadius;
    bool isPlaying;

    void Start()
    {
        if (disableOnStart)
        {
            gameObject.SetActive(false);
        }
    }

    void OnDisable()
    {
        CancelMove();
    }

    public void SetTransform(Vector3 pos, Quaternion rotation)
    {
        transform.position = pos;
        transform.rotation = rotation;
    }

    public void SetEffect(INoteData noteData, Judgement judgement, Action<IInteractNoteEffectController> returnToPool)
    {
        this.returnToPool = returnToPool;
        this.judgement = judgement;
        holdEndData = noteData as NoteData_HoldEnd;

        if (holdEndData == null || judgement == Judgement.Miss)
        {
            ReturnToPool();
            return;
        }

        InitializeRange(holdEndData.Range);
        curveRadius = GetCurveRadius(holdEndData);
    }

    public void InitializeRange(int[] range)
    {
        for (int i = 0; i < laneObjects.Length; i++)
        {
            if (laneObjects[i] == null) { continue; }
            laneObjects[i].SetActive(IsInRange(i, range));
        }
    }

    public void Play()
    {
        if (holdEndData == null || judgement == Judgement.Miss)
        {
            ReturnToPool();
            return;
        }

        gameObject.SetActive(true);

        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;

        CancelMove();
        moveCancellation = new CancellationTokenSource();
        FlyAsync(moveCancellation.Token).Forget();
    }

    async UniTaskVoid FlyAsync(CancellationToken token)
    {
        isPlaying = true;
        float elapsed = 0f;

        try
        {
            while (elapsed < flyDuration)
            {
                token.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                float distance = flySpeed * elapsed;

                // Keep the lane position and move forward along the same curved note track.
                Vector3 linearPosition = new Vector3(startLocalPosition.x, startLocalPosition.y, startLocalPosition.z + distance);
                transform.localPosition = NoteTrackCurve.BendVertex(linearPosition, curveRadius);

                if (rotateAlongGround)
                {
                    transform.localRotation = startLocalRotation * NoteTrackCurve.GetRotation(distance, curveRadius);
                }

                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            return;
        }
        finally
        {
            isPlaying = false;
        }

        ReturnToPool();
    }

    float GetCurveRadius(NoteData_HoldEnd noteData)
    {
        if (!useNoteCurveRadius) { return defaultCurveRadius; }
        if (noteData?.OptionGetter?.NoteCurveRadius == null) { return defaultCurveRadius; }

        return noteData.OptionGetter.NoteCurveRadius.Value;
    }

    bool IsInRange(int laneIndex, int[] range)
    {
        if (range == null) { return false; }

        for (int i = 0; i < range.Length; i++)
        {
            if (range[i] == laneIndex) { return true; }
        }

        return false;
    }

    void ReturnToPool()
    {
        CancelMove();
        gameObject.SetActive(false);
        returnToPool?.Invoke(this);
    }

    void CancelMove()
    {
        if (moveCancellation == null) { return; }

        if (!moveCancellation.IsCancellationRequested && isPlaying)
        {
            moveCancellation.Cancel();
        }

        moveCancellation.Dispose();
        moveCancellation = null;
    }
}
