using System;
using UnityEngine;
using Mediapipe.Unity;

/// <summary>
/// Applies the game's tracking settings to MediaPipe's sample WebCamSource.
/// </summary>
public class ConfigurableWebCamSource : WebCamSource
{
    private bool manualHorizontallyFlipped;
    private bool manualVerticallyFlipped;

    public override bool isHorizontallyFlipped
    {
        get => manualHorizontallyFlipped;
        set => manualHorizontallyFlipped = value;
    }

    public override bool isVerticallyFlipped => base.isVerticallyFlipped ^ manualVerticallyFlipped;

    public void ApplySettings(BodyTrackingSettings settings)
    {
        if (settings == null) { return; }

        manualHorizontallyFlipped = settings.IsHorizontallyFlipped.Value;
        manualVerticallyFlipped = settings.IsVerticallyFlipped.Value;

        ApplyCameraIndex(settings.CameraIndex);
        ApplyResolution(settings.CameraWidth.Value, settings.CameraHeight.Value);
    }

    private void ApplyCameraIndex(int cameraIndex)
    {
        var candidates = sourceCandidateNames;
        if (candidates == null || candidates.Length == 0) { return; }

        SelectSource(Mathf.Clamp(cameraIndex, 0, candidates.Length - 1));
    }

    private void ApplyResolution(int width, int height)
    {
        var resolutions = availableResolutions;
        if (resolutions == null || resolutions.Length == 0) { return; }

        var bestIndex = 0;
        var bestScore = int.MaxValue;

        for (var i = 0; i < resolutions.Length; i++)
        {
            var resolution = resolutions[i];
            var score = Math.Abs(resolution.width - width) + Math.Abs(resolution.height - height);
            if (score >= bestScore) { continue; }

            bestScore = score;
            bestIndex = i;
        }

        SelectResolution(bestIndex);
    }
}
