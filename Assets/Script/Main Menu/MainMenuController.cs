using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Settings Panel")]
    [SerializeField] private GameObject settingsPanel;

    // =====================================
    // START GAME
    // =====================================

    public void StartGame()
    {
        Time.timeScale = 1f;

        // Reset Global Gauge untuk permainan baru.
        ContaminationGaugeManager.ResetGlobalProgress();

        SceneManager.LoadScene("SectorSelection");
    }

    // =====================================
    // OPEN SETTINGS
    // =====================================

    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
        }
    }

    // =====================================
    // CLOSE SETTINGS
    // =====================================

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // =====================================
    // EXIT GAME
    // =====================================

    public void ExitGame()
    {
        Debug.Log("Keluar dari permainan.");

        #if UNITY_EDITOR
            // Hentikan Play Mode saat testing di Unity.
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            // Tutup aplikasi ketika sudah di-build.
            Application.Quit();
        #endif
    }
}