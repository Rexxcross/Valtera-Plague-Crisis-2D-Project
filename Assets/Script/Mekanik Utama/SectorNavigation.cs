using UnityEngine;
using UnityEngine.SceneManagement;

public class SectorNavigation : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sectorSelectionSceneName =
        "SectorSelection";

    // Tombol "Lanjut ke Sektor Berikutnya" pada panel Victory.
    // Jika sektor berikutnya belum ada, kembali ke Sector Selection.
    public void NextSector()
    {
        Time.timeScale = 1f;

        string next = GameProgress.NextSectorScene(
            SceneManager.GetActiveScene().name);

        SceneManager.LoadScene(
            string.IsNullOrEmpty(next)
                ? sectorSelectionSceneName
                : next);
    }

    public void BackToSectorSelection()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(sectorSelectionSceneName);
    }
}
