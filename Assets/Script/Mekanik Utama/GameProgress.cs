using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menyimpan progres sektor: sektor mana yang sudah terbuka dan dari mana permainan dimulai.
/// Class static, tidak perlu dipasang di objek mana pun.
/// </summary>
public static class GameProgress
{
    // =====================================
    // DAFTAR SEKTOR (URUTAN = URUTAN UNLOCK)
    // Ubah nama scene di sini jika berbeda.
    // =====================================

    public static readonly string[] SectorScenes =
    {
        "Scn_Sector1",
        "Scn_Sector2",
        "Scn_Sector3",
        "Scn_Sector4"
    };

    public const string SectorSelectionScene = "SectorSelection";

    private const string UnlockKey = "valtera.unlockedSectors";
    private const string StarsKeyPrefix = "valtera.stars.";


    // =====================================
    // SEKTOR AWAL PERMAINAN (UNTUK "ULANG DARI AWAL")
    // =====================================

    public static string RunStartScene { get; private set; } = SectorScenes[0];

    private static bool runStartChosen = false;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeData()
    {
        RunStartScene = SectorScenes[0];
        runStartChosen = false;
    }

    /// <summary>Dipanggil tombol Start di Main Menu.</summary>
    public static void BeginNewRun()
    {
        RunStartScene = SectorScenes[0];
        runStartChosen = false;
    }

    /// <summary>Dipanggil Sector Selection. Sektor pertama yang dipilih = awal permainan.</summary>
    public static void RegisterSectorSelected(string sceneName)
    {
        if (runStartChosen)
            return;

        RunStartScene = sceneName;
        runStartChosen = true;
    }


    // =====================================
    // UNLOCK SEKTOR
    // =====================================

    public static int UnlockedCount =>
        Mathf.Max(1, PlayerPrefs.GetInt(UnlockKey, 1));

    public static bool IsUnlocked(int sectorIndex) =>
        sectorIndex < UnlockedCount;

    public static int IndexOf(string sceneName) =>
        System.Array.IndexOf(SectorScenes, sceneName);

    /// <summary>Sektor selesai: buka sektor setelahnya.</summary>
    public static void CompleteSector(string sceneName)
    {
        int index = IndexOf(sceneName);

        if (index < 0)
            return;

        int newCount = Mathf.Clamp(index + 2, 1, SectorScenes.Length);

        if (newCount > UnlockedCount)
        {
            PlayerPrefs.SetInt(UnlockKey, newCount);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Hapus semua progres unlock (untuk tombol Game Baru / testing).</summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockKey);

        foreach (string scene in SectorScenes)
            PlayerPrefs.DeleteKey(StarsKeyPrefix + scene);

        PlayerPrefs.Save();
        BeginNewRun();
    }


    // =====================================
    // SEKTOR BERIKUTNYA
    // =====================================

    // =====================================
    // BINTANG PER SEKTOR (REKOR TERBAIK)
    // =====================================

    public static int GetStars(string sceneName) =>
        PlayerPrefs.GetInt(StarsKeyPrefix + sceneName, 0);

    public static void SaveStars(string sceneName, int stars)
    {
        if (stars > GetStars(sceneName))
        {
            PlayerPrefs.SetInt(StarsKeyPrefix + sceneName, stars);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Nama scene sektor berikutnya, atau null jika belum ada / belum masuk Build Settings.</summary>
    public static string NextSectorScene(string currentScene)
    {
        int index = IndexOf(currentScene);

        if (index < 0 || index + 1 >= SectorScenes.Length)
            return null;

        string next = SectorScenes[index + 1];

        return Application.CanStreamedLevelBeLoaded(next) ? next : null;
    }

    public static void LoadNextSectorOrSelection()
    {
        Time.timeScale = 1f;

        string next = NextSectorScene(SceneManager.GetActiveScene().name);

        SceneManager.LoadScene(next != null ? next : SectorSelectionScene);
    }
}