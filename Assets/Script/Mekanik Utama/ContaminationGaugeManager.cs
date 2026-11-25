using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContaminationGaugeManager : MonoBehaviour
{
    // =====================================
    // DATA GLOBAL YANG TERSIMPAN ANTAR SCENE
    // =====================================

    private static float savedGlobalContamination = 0f;
    private static bool globalProgressInitialized = false;

    // Nilai Global saat sektor dimulai (untuk "Ulang dari Sektor").
    private static float globalAtSectorStart = 0f;

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeData()
    {
        savedGlobalContamination = 0f;
        globalProgressInitialized = false;
        globalAtSectorStart = 0f;
    }

    // Dipanggil ketika memulai permainan baru (Main Menu / Ulang dari Awal).
    public static void ResetGlobalProgress()
    {
        savedGlobalContamination = 0f;
        globalProgressInitialized = false;
        globalAtSectorStart = 0f;
    }

    // Dipanggil sebelum mengulang sektor, agar Global tidak
    // terbawa naik dari percobaan yang gagal.
    public static void RestoreGlobalToSectorStart()
    {
        savedGlobalContamination = globalAtSectorStart;
        globalProgressInitialized = true;
    }


    // =====================================
    // UI GAUGE GLOBAL
    // =====================================

    [Header("UI Gauge Global")]
    [SerializeField] private Image globalGaugeFill;
    [SerializeField] private TMP_Text globalGaugeText;


    // =====================================
    // UI GAUGE LOKAL
    // =====================================

    [Header("UI Gauge Lokal")]
    [SerializeField] private Image localGaugeFill;
    [SerializeField] private TMP_Text localGaugeText;


    // =====================================
    // TAMPILAN GAUGE
    // =====================================

    [Header("Warna Gauge (rendah - sedang - tinggi)")]
    [SerializeField] private Color lowColor =
        new Color(0.31f, 0.54f, 0.17f);

    [SerializeField] private Color midColor =
        new Color(0.79f, 0.76f, 0.23f);

    [SerializeField] private Color highColor =
        new Color(0.85f, 0.33f, 0.31f);

    [SerializeField, Min(1f)]
    private float fillSmoothSpeed = 60f;


    // =====================================
    // PERINGATAN KRITIS
    // =====================================

    [Header("Peringatan Kritis")]

    [SerializeField, Range(0f, 100f)]
    private float criticalThreshold = 75f;

    [Tooltip("Di atas ambang kritis, gauge naik lebih cepat. Isi 1 untuk mematikan.")]
    [SerializeField, Min(1f)]
    private float criticalRiseMultiplier = 1.5f;

    [SerializeField] private CanvasGroup warningChip;


    // =====================================
    // KOMBO
    // =====================================

    [Header("Kombo Membersihkan")]
    [SerializeField] private TMP_Text comboText;

    [SerializeField, Min(0f)]
    private float comboWindow = 8f;

    [SerializeField, Min(1)]
    private int maxCombo = 5;

    [Tooltip("Tambahan pengurangan gauge per level kombo (0.25 = +25%)")]
    [SerializeField, Min(0f)]
    private float comboBonusPerLevel = 0.25f;


    // =====================================
    // NILAI AWAL GAUGE
    // =====================================

    [Header("Nilai Awal Gauge (0-100)")]

    [SerializeField, Range(0f, 100f)]
    private float globalStartingContamination = 0f;

    [SerializeField, Range(0f, 100f)]
    private float localStartingContamination = 0f;

    private float globalContamination;
    private float localContamination;


    // =====================================
    // KENAIKAN NORMAL
    // =====================================

    [Header("Kenaikan Normal per Detik")]

    [SerializeField, Min(0f)]
    private float globalRisePerSecond = 0.4f;

    [SerializeField, Min(0f)]
    private float localRisePerSecond = 0.8f;


    // =====================================
    // DETEKSI PEMAIN DIAM
    // =====================================

    [Header("Deteksi Pemain Diam")]

    [SerializeField] private Transform player;

    [SerializeField, Min(0f)]
    private float idleGracePeriod = 8f;

    [SerializeField, Min(0f)]
    private float movementThreshold = 0.01f;

    [SerializeField, Min(0f)]
    private float idleGlobalBonusPerSecond = 1f;

    [SerializeField, Min(0f)]
    private float idleLocalBonusPerSecond = 2f;


    // =====================================
    // PENALTI GAGAL MENYAPU
    // =====================================

    [Header("Penalti Gagal Menyapu")]

    [SerializeField, Min(0f)]
    private float failedGlobalPenalty = 4f;

    [SerializeField, Min(0f)]
    private float failedLocalPenalty = 10f;

    [SerializeField, Min(0f)]
    private float failureBoostDuration = 5f;

    [SerializeField, Min(0f)]
    private float failureGlobalBonusPerSecond = 1.5f;

    [SerializeField, Min(0f)]
    private float failureLocalBonusPerSecond = 3f;


    // =====================================
    // PENGURANGAN SAAT BERHASIL
    // =====================================

    [Header("Pengurangan Saat Berhasil")]

    [SerializeField, Min(0f)]
    private float globalDecreasePerClean = 2f;

    [SerializeField, Min(0f)]
    private float localDecreasePerClean = 12f;


    // =====================================
    // PANEL GAME OVER
    // =====================================

    [Header("Bad Ending - Global 100%")]
    [SerializeField] private GameObject badEndingPanel;
    [SerializeField] private TMP_Text badEndingStatsText;

    [Header("Game Over - Local 100%")]
    [SerializeField] private GameObject localGameOverPanel;
    [SerializeField] private TMP_Text gameOverStatsText;


    // =====================================
    // PRIVATE VARIABLES
    // =====================================

    private bool isGameOver = false;
    private bool sectorEnded = false;

    private Vector3 lastPlayerPosition;
    private float idleTimer;
    private float failureBoostTimer;

    private float shownGlobal;
    private float shownLocal;
    private int lastGlobalText = -1;
    private int lastLocalText = -1;

    private int combo = 0;
    private float lastCleanTime;

    private float elapsedTime = 0f;
    private int cleanCount = 0;


    // =====================================
    // PROPERTI PUBLIK
    // =====================================

    public bool IsGameOver => isGameOver;
    public float GlobalValue => globalContamination;
    public float LocalValue => localContamination;
    public float ElapsedTime => elapsedTime;
    public int CleanCount => cleanCount;


    // =====================================
    // START
    // =====================================

    private void Start()
    {
        Time.timeScale = 1f;

        // Inisialisasi Global jika belum ada sesi aktif.
        if (!globalProgressInitialized)
        {
            savedGlobalContamination = Mathf.Clamp(
                globalStartingContamination,
                0f,
                100f
            );

            globalProgressInitialized = true;
        }

        // Ambil Global Gauge yang tersimpan.
        globalContamination = Mathf.Clamp(
            savedGlobalContamination,
            0f,
            100f
        );

        // Catat Global saat sektor dimulai.
        globalAtSectorStart = globalContamination;

        // Local Gauge menggunakan nilai awal sektor.
        localContamination = Mathf.Clamp(
            localStartingContamination,
            0f,
            100f
        );

        shownGlobal = globalContamination;
        shownLocal = localContamination;

        // Cari Player secara otomatis jika belum diisi.
        if (player == null)
        {
            GameObject playerObject =
                GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
        else
        {
            Debug.LogWarning(
                "Player tidak ditemukan! Periksa Tag Player."
            );
        }

        // Atur Image Fill.
        SetupFillImage(globalGaugeFill);
        SetupFillImage(localGaugeFill);

        // Sembunyikan panel dan peringatan saat awal.
        if (badEndingPanel != null)
            badEndingPanel.SetActive(false);

        if (localGameOverPanel != null)
            localGameOverPanel.SetActive(false);

        if (warningChip != null)
            warningChip.alpha = 0f;

        UpdateComboUI();
        AnimateUI();

        // Periksa apakah Global atau Lokal sudah penuh.
        CheckGameOver();
    }


    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        // Animasi UI selalu berjalan (memakai unscaled time).
        AnimateUI();

        if (isGameOver || sectorEnded)
            return;

        float deltaTime = Time.deltaTime;

        elapsedTime += deltaTime;

        UpdateComboExpiry();

        // Periksa aktivitas Player.
        UpdateIdleTimer(deltaTime);

        bool isIdle = idleTimer >= idleGracePeriod;
        bool failureBoostActive = failureBoostTimer > 0f;

        float globalRate = globalRisePerSecond;
        float localRate = localRisePerSecond;

        // Tambahan kenaikan jika Player terlalu lama diam.
        if (isIdle)
        {
            globalRate += idleGlobalBonusPerSecond;
            localRate += idleLocalBonusPerSecond;
        }

        // Tambahan kenaikan setelah gagal menyapu.
        if (failureBoostActive)
        {
            globalRate += failureGlobalBonusPerSecond;
            localRate += failureLocalBonusPerSecond;

            failureBoostTimer = Mathf.Max(
                0f,
                failureBoostTimer - deltaTime
            );
        }

        // Di atas ambang kritis, gauge naik lebih cepat.
        if (globalContamination >= criticalThreshold)
            globalRate *= criticalRiseMultiplier;

        if (localContamination >= criticalThreshold)
            localRate *= criticalRiseMultiplier;

        globalContamination = Mathf.Clamp(
            globalContamination + globalRate * deltaTime,
            0f,
            100f
        );

        localContamination = Mathf.Clamp(
            localContamination + localRate * deltaTime,
            0f,
            100f
        );

        SaveGlobalProgress();

        // Periksa Game Over.
        CheckGameOver();
    }


    // =====================================
    // DETEKSI PEMAIN DIAM
    // =====================================

    private void UpdateIdleTimer(float deltaTime)
    {
        if (player == null)
            return;

        float distanceSquared =
            (player.position - lastPlayerPosition).sqrMagnitude;

        if (distanceSquared >=
            movementThreshold * movementThreshold)
        {
            // Player bergerak.
            idleTimer = 0f;
            lastPlayerPosition = player.position;
        }
        else
        {
            // Player diam.
            idleTimer += deltaTime;
        }
    }


    // =====================================
    // PLAYER MELAKUKAN AKTIVITAS
    // =====================================

    public void RegisterActivity()
    {
        if (isGameOver || sectorEnded)
            return;

        idleTimer = 0f;

        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }


    // =====================================
    // PLAYER GAGAL MENYAPU
    // =====================================

    public void RegisterFailedSweep()
    {
        if (isGameOver || sectorEnded)
            return;

        globalContamination = Mathf.Clamp(
            globalContamination + failedGlobalPenalty,
            0f,
            100f
        );

        localContamination = Mathf.Clamp(
            localContamination + failedLocalPenalty,
            0f,
            100f
        );

        // Aktifkan kenaikan tambahan sementara.
        failureBoostTimer = failureBoostDuration;

        // Gagal memutus kombo.
        combo = 0;
        UpdateComboUI();

        SaveGlobalProgress();

        CheckGameOver();
    }


    // =====================================
    // PLAYER BERHASIL MEMBERSIHKAN DEBU
    // =====================================

    public void RegisterCleaningSuccess()
    {
        if (isGameOver || sectorEnded)
            return;

        // Kombo: bersihkan berturut-turut agar pengurangan makin besar.
        if (combo > 0 && Time.time - lastCleanTime > comboWindow)
            combo = 0;

        combo = Mathf.Min(combo + 1, maxCombo);
        lastCleanTime = Time.time;

        float multiplier = 1f + (combo - 1) * comboBonusPerLevel;

        globalContamination = Mathf.Clamp(
            globalContamination - globalDecreasePerClean * multiplier,
            0f,
            100f
        );

        localContamination = Mathf.Clamp(
            localContamination - localDecreasePerClean * multiplier,
            0f,
            100f
        );

        cleanCount++;

        UpdateComboUI();

        RegisterActivity();

        SaveGlobalProgress();
    }


    // =====================================
    // SEKTOR SELESAI (DIPANGGIL PlayerSweep)
    // =====================================

    public void StopSector()
    {
        sectorEnded = true;

        SaveGlobalProgress();
        SnapUI();
    }


    // =====================================
    // KOMBO
    // =====================================

    private void UpdateComboExpiry()
    {
        if (combo > 0 && Time.time - lastCleanTime > comboWindow)
        {
            combo = 0;
            UpdateComboUI();
        }
    }

    private void UpdateComboUI()
    {
        if (comboText == null)
            return;

        comboText.text = combo > 1 ? "KOMBO ×" + combo : "";
    }


    // =====================================
    // SIMPAN GLOBAL GAUGE
    // =====================================

    private void SaveGlobalProgress()
    {
        savedGlobalContamination = globalContamination;
    }


    // =====================================
    // PERIKSA KONDISI GAME OVER
    // =====================================

    private void CheckGameOver()
    {
        // Global memiliki prioritas tertinggi.
        if (globalContamination >= 100f)
        {
            TriggerBadEnding();
            return;
        }

        // Jika Global belum penuh, periksa Lokal.
        if (localContamination >= 100f)
        {
            TriggerLocalGameOver();
        }
    }


    // =====================================
    // BAD ENDING - GLOBAL 100%
    // =====================================

    private void TriggerBadEnding()
    {
        if (isGameOver || sectorEnded)
            return;

        isGameOver = true;

        globalContamination = 100f;

        SaveGlobalProgress();
        SnapUI();

        Debug.Log("BAD ENDING! Global Contamination mencapai 100%.");

        ShowEndPanel(badEndingPanel, badEndingStatsText);
    }


    // =====================================
    // GAME OVER - LOCAL 100%
    // =====================================

    private void TriggerLocalGameOver()
    {
        if (isGameOver || sectorEnded)
            return;

        isGameOver = true;

        localContamination = 100f;

        SnapUI();

        Debug.Log("GAME OVER! Local Contamination mencapai 100%.");

        ShowEndPanel(localGameOverPanel, gameOverStatsText);
    }

    private void ShowEndPanel(GameObject panel, TMP_Text statsText)
    {
        if (statsText != null)
            statsText.text = BuildStatsLine();

        if (panel != null)
            StartCoroutine(PanelFader.FadeIn(panel));

        // Hentikan permainan.
        Time.timeScale = 0f;
    }


    // =====================================
    // STATISTIK UNTUK LAYAR AKHIR
    // =====================================

    public string BuildStatsLine()
    {
        int seconds = Mathf.FloorToInt(elapsedTime);

        return
            $"Kotoran bersih  {cleanCount}" +
            $"        Waktu  {seconds / 60:00}:{seconds % 60:00}" +
            $"        Gauge Global  {Mathf.RoundToInt(globalContamination)}%";
    }


    // =====================================
    // PENGATURAN IMAGE FILL
    // =====================================

    private void SetupFillImage(Image fillImage)
    {
        if (fillImage == null)
            return;

        fillImage.type = Image.Type.Filled;

        fillImage.fillMethod = Image.FillMethod.Horizontal;

        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
    }


    // =====================================
    // ANIMASI & TAMPILAN GAUGE
    // =====================================

    private void SnapUI()
    {
        shownGlobal = globalContamination;
        shownLocal = localContamination;

        AnimateUI();
    }

    private void AnimateUI()
    {
        float step = fillSmoothSpeed * Time.unscaledDeltaTime;

        shownGlobal = Mathf.MoveTowards(
            shownGlobal, globalContamination, step);

        shownLocal = Mathf.MoveTowards(
            shownLocal, localContamination, step);

        ApplyGauge(globalGaugeFill, globalGaugeText,
            shownGlobal, ref lastGlobalText);

        ApplyGauge(localGaugeFill, localGaugeText,
            shownLocal, ref lastLocalText);

        // Chip peringatan berdenyut saat Lokal kritis.
        if (warningChip != null)
        {
            bool critical =
                !isGameOver &&
                !sectorEnded &&
                localContamination >= criticalThreshold;

            warningChip.alpha = critical
                ? 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 6f)
                : 0f;
        }
    }

    private void ApplyGauge(
        Image fill, TMP_Text label, float value, ref int lastText)
    {
        if (fill != null)
        {
            fill.fillAmount = value / 100f;
            fill.color = GaugeColor(value);
        }

        int rounded = Mathf.RoundToInt(value);

        if (label != null && rounded != lastText)
        {
            label.text = rounded + "%";
            lastText = rounded;
        }
    }

    private Color GaugeColor(float value)
    {
        float t = Mathf.Clamp01(value / 100f);

        return t < 0.5f
            ? Color.Lerp(lowColor, midColor, t * 2f)
            : Color.Lerp(midColor, highColor, (t - 0.5f) * 2f);
    }
}
