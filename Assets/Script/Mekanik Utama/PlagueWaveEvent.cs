using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Gelombang Wabah: tiap beberapa detik muncul peringatan, lalu beberapa area
/// yang sudah bersih dikotori lagi dan gauge naik. Pasang di objek kosong.
/// </summary>
public class PlagueWaveEvent : MonoBehaviour
{
    [Header("Waktu")]
    [SerializeField, Min(0f)] private float firstWaveDelay = 40f;
    [SerializeField] private Vector2 intervalRange = new Vector2(45f, 70f);
    [Tooltip("Detik peringatan sebelum gelombang menyerang")]
    [SerializeField, Min(0.5f)] private float warningTime = 4f;

    [Header("Dampak")]
    [SerializeField, Min(0)] private int areasPerWave = 2;
    [SerializeField, Min(0f)] private float localPenalty = 8f;
    [SerializeField, Min(0f)] private float globalPenalty = 2f;

    [Header("UI (diisi otomatis oleh Valtera > Bangun UI Sektor)")]
    [SerializeField] private CanvasGroup bannerGroup;
    [SerializeField] private TMP_Text bannerText;

    [Header("SFX (opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip waveSFX;

    private ContaminationGaugeManager manager;

    private void Start()
    {
        manager = FindAnyObjectByType<ContaminationGaugeManager>();

        if (bannerGroup != null)
            bannerGroup.alpha = 0f;

        StartCoroutine(WaveLoop());
    }

    private IEnumerator WaveLoop()
    {
        yield return new WaitForSeconds(firstWaveDelay);

        while (manager == null || !manager.IsGameOver)
        {
            // 1. Peringatan.
            yield return ShowBanner("GELOMBANG WABAH! BERSIAPLAH...", warningTime);

            // 2. Serangan.
            ApplyWave();

            yield return ShowBanner("WABAH MENYEBAR!", 2f);

            // 3. Jeda sampai gelombang berikutnya.
            yield return new WaitForSeconds(
                Random.Range(intervalRange.x, intervalRange.y));
        }
    }

    private void ApplyWave()
    {
        if (audioSource != null && waveSFX != null)
            audioSource.PlayOneShot(waveSFX);

        // Kumpulkan area yang sudah bersih, lalu acak urutannya.
        List<SweepableArea> cleaned = new List<SweepableArea>();

        foreach (SweepableArea area in SweepableArea.All)
        {
            if (area != null && area.IsCleaned)
                cleaned.Add(area);
        }

        for (int i = cleaned.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            SweepableArea temp = cleaned[i];
            cleaned[i] = cleaned[j];
            cleaned[j] = temp;
        }

        int count = Mathf.Min(areasPerWave, cleaned.Count);

        for (int i = 0; i < count; i++)
            cleaned[i].Recontaminate();

        if (manager != null)
            manager.AddContamination(localPenalty, globalPenalty);
    }

    private IEnumerator ShowBanner(string message, float duration)
    {
        if (bannerText != null)
            bannerText.text = message;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;

            if (bannerGroup != null)
            {
                float fade = Mathf.Clamp01(Mathf.Min(t, duration - t) * 3f);
                float pulse = 0.75f + 0.25f * Mathf.Sin(Time.time * 8f);

                bannerGroup.alpha = fade * pulse;
            }

            yield return null;
        }

        if (bannerGroup != null)
            bannerGroup.alpha = 0f;
    }
}
