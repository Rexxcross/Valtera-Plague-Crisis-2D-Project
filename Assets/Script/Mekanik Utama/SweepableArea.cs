using System.Collections.Generic;
using UnityEngine;

public class SweepableArea : MonoBehaviour
{
    // Daftar semua area kotor di scene (termasuk yang sudah bersih / nonaktif).
    public static readonly List<SweepableArea> All = new List<SweepableArea>();

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatic()
    {
        All.Clear();
    }

    public static int TotalCount => All.Count;

    public static int CleanedCount
    {
        get
        {
            int count = 0;

            foreach (SweepableArea area in All)
            {
                if (area != null && area.IsCleaned)
                    count++;
            }

            return count;
        }
    }

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
        All.Add(this);

        renderers = GetComponentsInChildren<SpriteRenderer>();
        baseColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
            baseColors[i] = renderers[i].color;
    }

    private void OnDestroy()
    {
        All.Remove(this);
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

    // Dipanggil tikus / gelombang wabah: area yang sudah bersih kotor lagi.
    public bool Recontaminate()
    {
        if (!isCleaned)
            return false;

        isCleaned = false;
        sweepTime = 0f;

        ApplyVisual(0f);

        gameObject.SetActive(true);

        return true;
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