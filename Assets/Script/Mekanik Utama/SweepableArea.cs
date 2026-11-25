using UnityEngine;

public class SweepableArea : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float secondsToClean = 2f;

    [Header("Efek Visual")]
    [Tooltip("Transparansi terendah saat hampir bersih (1 = tidak memudar)")]
    [SerializeField, Range(0f, 1f)]
    private float minAlpha = 0.3f;

    private float sweepTime = 0f;
    private bool isCleaned = false;

    private SpriteRenderer[] renderers;
    private Color[] baseColors;

    public bool IsCleaned => isCleaned;

    public float Progress01 =>
        isCleaned ? 1f :
        Mathf.Clamp01(sweepTime / secondsToClean);

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;
    }

    public bool Sweep(float deltaTime)
    {
        if (isCleaned)
            return false;

        sweepTime += deltaTime;

        ApplyVisual(Progress01);

        if (sweepTime >= secondsToClean)
        {
            isCleaned = true;

            // Hilangkan debu ketika sudah 100%.
            gameObject.SetActive(false);

            return true;
        }

        return false;
    }

    public void CancelSweep()
    {
        if (!isCleaned)
        {
            // Reset jika tombol E dilepas.
            sweepTime = 0f;
            ApplyVisual(0f);
        }
    }

    // Debu memudar saat disapu, kembali normal jika dibatalkan.
    private void ApplyVisual(float progress)
    {
        if (renderers == null)
            return;

        float alpha = Mathf.Lerp(1f, minAlpha, progress);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;

            Color c = baseColors[i];
            c.a = baseColors[i].a * alpha;
            renderers[i].color = c;
        }
    }
}
