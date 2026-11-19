using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    [Header("Main Menu")]
    [SerializeField] private string mainMenuSceneName = "Scn_MainMenu";


    // =====================================
    // RETRY SEKTOR SAAT INI
    // =====================================

    public void RetryCurrentSector()
    {
        string currentScene =
            SceneManager.GetActiveScene().name;

        Debug.Log("Mengulang sektor: " + currentScene);

        // Kembalikan waktu normal.
        Time.timeScale = 1f;

        // Muat ulang scene yang sedang dimainkan.
        SceneManager.LoadScene(currentScene);
    }


    // =====================================
    // KEMBALI KE MAIN MENU
    // =====================================

    public void BackToMainMenu()
    {
        Debug.Log("Kembali ke Main Menu.");

        Time.timeScale = 1f;

        SceneManager.LoadScene(mainMenuSceneName);
    }


    // =====================================
    // EXIT GAME
    // =====================================

    public void ExitGame()
    {
        Debug.Log("Keluar dari permainan.");

        Time.timeScale = 1f;

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
