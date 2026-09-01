using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class NoteObject<T> : MonoBehaviour, INoteVisibilityTarget where T : INoteData
{
    public float StartChartDistance { get; private set; }

    public float EndChartDistance { get; private set; }

    public bool IsVisibilityLocked { get; private set; }

    virtual public void SetActive(bool isVisible)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(isVisible);
        }
    }

    public virtual bool ShouldLockVisibility(float currentDistance)
    {
        return false;
    }

    public void LockVisibility()
    {
        if (IsVisibilityLocked) { return; }

        IsVisibilityLocked = true;
        SetActive(false);
    }

    public void UnlockVisibility()
    {
        if (!IsVisibilityLocked) { return; }

        IsVisibilityLocked = false;
    }

    abstract public void Initialize(T data);

    /// <summary>
    /// エディタ中にノートスピードが変更された際など
    /// </summary>
    /// <param name="z"></param>
    public void SetPosition(float distance, float radius)
    {
        SetPosition(distance, distance, radius);
    }

    /// <summary>
    /// ホールド系ノーツの開始・終了距離を保持し、開始位置へ配置する
    /// </summary>
    public void SetPosition(float startDistance, float endDistance, float radius)
    {
        // 逆方向のソフランでも表示区間として扱えるよう、小さい方を開始距離とする
        StartChartDistance = Mathf.Min(startDistance, endDistance);
        EndChartDistance = Mathf.Max(startDistance, endDistance);

        // 生成時に円弧上の位置と接線方向へ合わせる
        NoteTrackCurve.SetPose(this.transform, startDistance, radius);
    }
}

