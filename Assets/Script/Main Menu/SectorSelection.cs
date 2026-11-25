using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SectorSelection : MonoBehaviour
{
    [System.Serializable]
    public class SectorEntry
    {
        [Tooltip("Nama scene sektor (harus ada di Build Settings)")]
        public string sceneName = "Scn_Sector1";

        public Button button;

        [Tooltip("Opsional: gambar gembok, disembunyikan jika sektor sudah terbuka")]
        public GameObject lockIcon;
    }

    [SerializeField] private string mainMenuSceneName = "Scn_MainMenu";

    [Header("Daftar Sektor (opsional, untuk gembok otomatis)")]
    [SerializeField] private SectorEntry[] sectors;


    // =====================================
    // START
    // =====================================

    private void Start()
    {
        Time.timeScale = 1f;

        if (sectors == null)
            return;

        foreach (SectorEntry entry in sectors)
        {
            if (entry == null)
                continue;

            int index = GameProgress.IndexOf(entry.sceneName);

            bool unlocked =
                index < 0 || GameProgress.IsUnlocked(index);

            if (entry.button != null)
            {
                entry.button.interactable = unlocked;

                // Pasang klik otomatis hanya jika tombol belum punya OnClick
                // dari Inspector (agar tidak terpanggil dua kali).
                if (entry.button.onClick.GetPersistentEventCount() == 0)
                {
                    string scene = entry.sceneName;

                    entry.button.onClick.AddListener(
                        () => SelectSector(scene));
                }
            }

            if (entry.lockIcon != null)
                entry.lockIcon.SetActive(!unlocked);
        }
    }


    // =====================================
    // PILIH SEKTOR
    // (nama fungsi sama dengan script lama, jadi OnClick lama tetap jalan)
    // =====================================

    public void SelectSector(string sceneName)
    {
        int index = GameProgress.IndexOf(sceneName);

        if (index >= 0 && !GameProgress.IsUnlocked(index))
        {
            Debug.Log("Sektor masih terkunci: " + sceneName);
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning(
                "Scene '" + sceneName +
                "' belum ada di Build Settings."
            );
            return;
        }

        // Sektor pertama yang dipilih = awal permainan ("Ulang dari Awal").
        GameProgress.RegisterSectorSelected(sceneName);

        SceneManager.LoadScene(sceneName);
    }

    public void BackToMainMenu()
    {
        SceneManager.LoadScene(mainMenuSceneName);
    }
}
