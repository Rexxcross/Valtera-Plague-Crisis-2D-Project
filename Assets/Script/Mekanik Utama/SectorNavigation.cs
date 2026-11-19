
using UnityEngine;
using UnityEngine.SceneManagement;

public class SectorNavigation : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string sectorSelectionSceneName =
        "SectorSelection";

    public void NextSector()
    {
        Debug.Log("Menuju Sector Selection.");

        // Aktifkan kembali waktu permainan.
        Time.timeScale = 1f;

        // Kembali ke pemilihan sektor.
        SceneManager.LoadScene(sectorSelectionSceneName);
    }
}
