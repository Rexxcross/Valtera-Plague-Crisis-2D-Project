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

    [RuntimeInitializeOnLoadMethod(
        RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeData()
    {
        savedGlobalContamination = 0f;
        globalProgressInitialized = false;
    }

    // Dipanggil ketika memulai permainan baru dari Main Menu.
    public static void ResetGlobalProgress()
    {
        savedGlobalContamination = 0f;
        globalProgressInitialized = false;
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

    [Header("Sector Failed - Local 100%")]
    [SerializeField] private GameObject localGameOverPanel;


    // =====================================
    // PRIVATE VARIABLES
    // =====================================

    private bool isGameOver = false;

    private Vector3 lastPlayerPosition;

    private float idleTimer;

    private float failureBoostTimer;


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

        // Local Gauge menggunakan nilai awal sektor.
        localContamination = Mathf.Clamp(
            localStartingContamination,
            0f,
            100f
        );

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

        // Sembunyikan kedua panel saat awal.
        if (badEndingPanel != null)
            badEndingPanel.SetActive(false);

        if (localGameOverPanel != null)
            localGameOverPanel.SetActive(false);

        UpdateGaugeUI();

        // Periksa apakah Global atau Lokal sudah penuh.
        CheckGameOver();
    }


    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        if (isGameOver)
            return;

        float deltaTime = Time.deltaTime;

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


        // Naikkan Global Gauge.
        globalContamination = Mathf.Clamp(
            globalContamination + globalRate * deltaTime,
            0f,
            100f
        );


        // Naikkan Local Gauge.
        localContamination = Mathf.Clamp(
            localContamination + localRate * deltaTime,
            0f,
            100f
        );


        // Simpan Global Gauge.
        SaveGlobalProgress();

        // Perbarui tampilan.
        UpdateGaugeUI();

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
        if (isGameOver)
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
        if (isGameOver)
            return;

        // Penalti Global.
        globalContamination = Mathf.Clamp(
            globalContamination + failedGlobalPenalty,
            0f,
            100f
        );

        // Penalti Lokal.
        localContamination = Mathf.Clamp(
            localContamination + failedLocalPenalty,
            0f,
            100f
        );

        // Aktifkan kenaikan tambahan sementara.
        failureBoostTimer = failureBoostDuration;

        SaveGlobalProgress();

        UpdateGaugeUI();

        Debug.Log("Gagal menyapu! Kontaminasi meningkat.");

        CheckGameOver();
    }


    // =====================================
    // PLAYER BERHASIL MEMBERSIHKAN DEBU
    // =====================================

    public void RegisterCleaningSuccess()
    {
        if (isGameOver)
            return;

        // Kurangi Global.
        globalContamination = Mathf.Clamp(
            globalContamination - globalDecreasePerClean,
            0f,
            100f
        );

        // Kurangi Lokal.
        localContamination = Mathf.Clamp(
            localContamination - localDecreasePerClean,
            0f,
            100f
        );

        RegisterActivity();

        SaveGlobalProgress();

        UpdateGaugeUI();

        Debug.Log("Kontaminasi berhasil dikurangi!");
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
        if (isGameOver)
            return;

        isGameOver = true;

        globalContamination = 100f;

        SaveGlobalProgress();

        UpdateGaugeUI();

        Debug.Log(
            "BAD ENDING! Global Contamination mencapai 100%."
        );

        if (badEndingPanel != null)
        {
            badEndingPanel.SetActive(true);
        }

        // Hentikan permainan.
        Time.timeScale = 0f;
    }


    // =====================================
    // SECTOR FAILED - LOCAL 100%
    // =====================================

    private void TriggerLocalGameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        localContamination = 100f;

        UpdateGaugeUI();

        Debug.Log(
            "SECTOR FAILED! Local Contamination mencapai 100%."
        );

        if (localGameOverPanel != null)
        {
            localGameOverPanel.SetActive(true);
        }

        // Hentikan permainan.
        Time.timeScale = 0f;
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

        fillImage.fillOrigin =
            (int)Image.OriginHorizontal.Left;
    }


    // =====================================
    // UPDATE TAMPILAN GAUGE
    // =====================================

    private void UpdateGaugeUI()
    {
        // Global Fill.
        if (globalGaugeFill != null)
        {
            globalGaugeFill.fillAmount =
                globalContamination / 100f;
        }

        // Lokal Fill.
        if (localGaugeFill != null)
        {
            localGaugeFill.fillAmount =
                localContamination / 100f;
        }

        // Global Text.
        if (globalGaugeText != null)
        {
            globalGaugeText.text =
                $"Global: {Mathf.RoundToInt(globalContamination)}%";
        }

        // Lokal Text.
        if (localGaugeText != null)
        {
            localGaugeText.text =
                $"Lokal: {Mathf.RoundToInt(localContamination)}%";
        }
    }
}