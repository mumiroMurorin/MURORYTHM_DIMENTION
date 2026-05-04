using UnityEngine;

public static class SpaceInputNormalizer
{
    /// <summary>
    /// XYのみを使って、BodyTrackingSettingsの基準点から-1～1に正規化します。
    /// </summary>
    public static Vector3 NormalizeUsedVector2(Vector3 pos, BodyTrackingSettings settings)
    {
        if (settings == null) { return Vector3.zero; }

        Vector3 controllerLeft = settings.ControllerLeftEdge.Value;
        controllerLeft = new Vector3(controllerLeft.x, controllerLeft.y);
        Vector3 controllerRight = settings.ControllerRightEdge.Value;
        controllerRight = new Vector3(controllerRight.x, controllerRight.y);

        Vector3 center = controllerLeft + (controllerRight - controllerLeft) / 2f;
        Vector3 controllerLowerCenter = settings.ControllerLowerCenter.Value;
        controllerLowerCenter = new Vector3(controllerLowerCenter.x, controllerLowerCenter.y);
        Vector3 controllerUpperCenter = controllerLowerCenter + (center - controllerLowerCenter) * 2f;

        Vector2 xy = new Vector2(
            NormalizeScalar(controllerLeft, controllerRight, pos),
            NormalizeScalar(controllerLowerCenter, controllerUpperCenter, pos)
        );
        xy = Vector2.ClampMagnitude(xy, 1f);

        return new Vector3(xy.x, xy.y, 0f);
    }

    /// <summary>
    /// XYZを使って、BodyTrackingSettingsの基準点から-1～1に正規化します。
    /// </summary>
    public static Vector3 NormalizeUsedVector3(Vector3 pos, BodyTrackingSettings settings)
    {
        if (settings == null) { return Vector3.zero; }

        Vector3 controllerLeft = settings.ControllerLeftEdge.Value;
        Vector3 controllerRight = settings.ControllerRightEdge.Value;

        Vector3 center = controllerLeft + (controllerRight - controllerLeft) / 2f;
        Vector3 controllerLowerCenter = settings.ControllerLowerCenter.Value;
        Vector3 controllerUpperCenter = controllerLowerCenter + (center - controllerLowerCenter) * 2f;

        Vector2 xy = new Vector2(
            NormalizeScalar(controllerLeft, controllerRight, pos),
            NormalizeScalar(controllerLowerCenter, controllerUpperCenter, pos)
        );
        xy = Vector2.ClampMagnitude(xy, 1f);

        return new Vector3(xy.x, xy.y, 0f);
    }

    public static float NormalizeScalar(Vector3 posA, Vector3 posB, Vector3 targetPos)
    {
        if ((posA - posB).sqrMagnitude == 0) { return 0; }

        Vector3 baseVec = posB - posA;
        Vector3 toTarget = targetPos - posA;

        float ratio = Vector3.Dot(toTarget, baseVec.normalized) / baseVec.magnitude;

        return Mathf.Lerp(-1, 1, ratio);
    }
}
