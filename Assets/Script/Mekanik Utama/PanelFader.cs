using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Menampilkan panel dengan fade + sedikit zoom. Memakai unscaled time,
/// jadi tetap jalan saat Time.timeScale = 0 (Game Over / Victory).
/// Pemakaian: StartCoroutine(PanelFader.FadeIn(panel));
/// </summary>
public static class PanelFader
{
    public static IEnumerator FadeIn(GameObject panel, float duration = 0.5f)
    {
        if (panel == null)
            yield break;

        CanvasGroup group = panel.GetComponent<CanvasGroup>();

        if (group == null)
            group = panel.AddComponent<CanvasGroup>();

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = true;

        panel.SetActive(true);

        Transform tr = panel.transform;

        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / duration);

            group.alpha = k;
            tr.localScale = Vector3.one * Mathf.Lerp(1.06f, 1f, k);

            yield return null;
        }

        group.alpha = 1f;
        group.interactable = true;
        tr.localScale = Vector3.one;

        // Pilih tombol pertama agar bisa dipakai dengan keyboard / gamepad.
        Button first = panel.GetComponentInChildren<Button>();

        if (first != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }
}
