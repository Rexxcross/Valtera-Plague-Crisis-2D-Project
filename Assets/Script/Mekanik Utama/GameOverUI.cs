using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private string mainMenuSceneName = "Scn_MainMenu";


    // =====================================
    // ULANG DARI AWAL (BAD ENDING & GAME OVER)
    // =====================================

    // Kembali ke sektor tempat permainan dimulai,
    // dengan Global Gauge direset.
    public void RestartFromBeginning()
    {
        Time.timeScale = 1f;

        ContaminationGaugeManager.ResetGlobalProgress();

        SceneManager.LoadScene(GameProgress.RunStartScene);
    }


    // =====================================
    // ULANG DARI SEKTOR (GAME OVER)
    // =====================================

    public void RetryCurrentSector()
    {
        string currentScene =
            SceneManager.GetActiveScene().name;

        Time.timeScale = 1f;

        // Kembalikan Global ke nilai saat sektor dimulai,
        // agar kegagalan tadi tidak ikut terbawa.
        ContaminationGaugeManager.RestoreGlobalToSectorStart();

        SceneManager.LoadScene(currentScene);
    }


    // =====================================
    // KEMBALI KE MAIN MENU
    // =====================================

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }


    // =====================================
    // EXIT GAME
    // =====================================

    public void ExitGame()
    {
        Time.timeScale = 1f;

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
