using System.Collections;
using TMPro;
using UnityEngine;

public class CreditTextView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI creditText;
    [SerializeField] private string[] creditTexts;
    [SerializeField, Min(0.01f)] private float intervalSeconds = 2f;
    [SerializeField, Min(0.01f)] private float fadeSeconds = 0.35f;

    private int currentIndex = -1;
    private Coroutine playCoroutine;
    private Color baseColor;

    private void Awake()
    {
        if (creditText == null)
        {
            creditText = GetComponent<TextMeshProUGUI>();
        }

        if (creditText != null)
        {
            baseColor = creditText.color;
        }
    }

    private void Start()
    {
        if (creditTexts == null || creditTexts.Length == 0)
        {
            if (creditText != null)
            {
                creditText.text = string.Empty;
            }
            return;
        }

        currentIndex = Random.Range(0, creditTexts.Length);
        ApplyCurrentText();
        SetTextAlpha(1f);
        playCoroutine = StartCoroutine(PlayCoroutine());
    }

    private void OnDisable()
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }
    }

    private IEnumerator PlayCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(intervalSeconds);
            yield return FadeTextAlpha(1f, 0f, fadeSeconds);
            MoveNext();
            ApplyCurrentText();
            yield return FadeTextAlpha(0f, 1f, fadeSeconds);
        }
    }

    private void MoveNext()
    {
        if (creditTexts == null || creditTexts.Length == 0)
        {
            currentIndex = -1;
            return;
        }

        if (currentIndex < 0)
        {
            currentIndex = Random.Range(0, creditTexts.Length);
            return;
        }

        currentIndex = (currentIndex + 1) % creditTexts.Length;
    }

    private void ApplyCurrentText()
    {
        if (creditText == null)
        {
            return;
        }

        if (creditTexts == null || creditTexts.Length == 0 || currentIndex < 0 || currentIndex >= creditTexts.Length)
        {
            creditText.text = string.Empty;
            return;
        }

        creditText.text = creditTexts[currentIndex];
    }

    private IEnumerator FadeTextAlpha(float from, float to, float duration)
    {
        if (creditText == null)
        {
            yield break;
        }

        if (duration <= 0f)
        {
            SetTextAlpha(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetTextAlpha(Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetTextAlpha(to);
    }

    private void SetTextAlpha(float alpha)
    {
        if (creditText == null)
        {
            return;
        }

        var color = baseColor;
        color.a = alpha;
        creditText.color = color;
    }
}
