using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ContaminationGaugeManager : MonoBehaviour
{
    [Header("UI Gauge Global")]
    [SerializeField] private Slider globalGaugeSlider;
    [SerializeField] private TMP_Text globalGaugeText;

    [Header("UI Gauge Lokal")]
    [SerializeField] private Slider localGaugeSlider;
    [SerializeField] private TMP_Text localGaugeText;

    [Header("Nilai Awal Gauge (0-100)")]
    [SerializeField, Range(0f, 100f)]
    private float globalContamination = 0f;

    [SerializeField, Range(0f, 100f)]
    private float localContamination = 0f;

    [Header("Kenaikan Normal per Detik")]
    [SerializeField, Min(0f)]
    private float globalRisePerSecond = 0.4f;

    [SerializeField, Min(0f)]
    private float localRisePerSecond = 0.8f;

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

    [Header("Pengurangan Saat Berhasil")]
    [SerializeField, Min(0f)]
    private float globalDecreasePerClean = 2f;

    [SerializeField, Min(0f)]
    private float localDecreasePerClean = 12f;

    [Header("Bad Ending / Game Over")]
    [SerializeField] private GameObject gameOverPanel;

    private bool isGameOver = false;

    private Vector3 lastPlayerPosition;
    private float idleTimer;
    private float failureBoostTimer;
    private void Start()
    {
        // Pastikan permainan berjalan normal.
        Time.timeScale = 1f;

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
                "Player belum ditemukan! Pastikan Tag Player sudah benar."
            );
        }

        // Atur Slider.
        SetupSlider(globalGaugeSlider);
        SetupSlider(localGaugeSlider);

        // Sembunyikan panel Game Over.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Perbarui tampilan gauge.
        UpdateGaugeUI();
    }
    private void Update()
    {
        // Jangan jalankan sistem jika sudah Game Over.
        if (isGameOver)
            return;

        float deltaTime = Time.deltaTime;

        // Periksa apakah Player sedang diam.
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

        // Naikkan Gauge Global.
        globalContamination = Mathf.Clamp(
            globalContamination + globalRate * deltaTime,
            0f,
            100f
        );

        // Naikkan Gauge Lokal.
        localContamination = Mathf.Clamp(
            localContamination + localRate * deltaTime,
            0f,
            100f
        );

        // Perbarui UI.
        UpdateGaugeUI();

        // Periksa apakah Gauge Global sudah mencapai 100%.
        if (globalContamination >= 100f)
        {
            TriggerGameOver();
        }
    }

    private void UpdateIdleTimer(float deltaTime)
    {
        if (player == null)
            return;

        float distanceSquared =
            (player.position - lastPlayerPosition).sqrMagnitude;

        if (distanceSquared >=
            movementThreshold * movementThreshold)
        {
            // Player bergerak, reset waktu diam.
            idleTimer = 0f;

            lastPlayerPosition = player.position;
        }
        else
        {
            // Player tidak bergerak.
            idleTimer += deltaTime;
        }
    }

    public void RegisterActivity()
    {
        if (isGameOver)
            return;

        // Reset waktu diam ketika Player menyapu.
        idleTimer = 0f;

        if (player != null)
        {
            lastPlayerPosition = player.position;
        }
    }

    public void RegisterFailedSweep()
    {
        if (isGameOver)
            return;

        // Penalti langsung pada Gauge Global.
        globalContamination = Mathf.Clamp(
            globalContamination + failedGlobalPenalty,
            0f,
            100f
        );

        // Penalti langsung pada Gauge Lokal.
        localContamination = Mathf.Clamp(
            localContamination + failedLocalPenalty,
            0f,
            100f
        );

        // Gauge naik lebih cepat selama beberapa detik.
        failureBoostTimer = failureBoostDuration;

        UpdateGaugeUI();

        Debug.Log(
            "Gagal menyapu! Kontaminasi meningkat."
        );

        // Periksa Game Over setelah penalti.
        if (globalContamination >= 100f)
        {
            TriggerGameOver();
        }
    }

    public void RegisterCleaningSuccess()
    {
        if (isGameOver)
            return;

        // Kurangi Gauge Global.
        globalContamination = Mathf.Clamp(
            globalContamination - globalDecreasePerClean,
            0f,
            100f
        );

        // Kurangi Gauge Lokal.
        localContamination = Mathf.Clamp(
            localContamination - localDecreasePerClean,
            0f,
            100f
        );

        // Reset waktu diam.
        RegisterActivity();

        UpdateGaugeUI();

        Debug.Log(
            "Berhasil membersihkan debu! Kontaminasi berkurang."
        );
    }

    private void SetupSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 100f;
    }

    private void UpdateGaugeUI()
    {
        if (globalGaugeSlider != null)
        {
            globalGaugeSlider.value = globalContamination;
        }

        if (localGaugeSlider != null)
        {
            localGaugeSlider.value = localContamination;
        }

        if (globalGaugeText != null)
        {
            globalGaugeText.text =
                $"Global: {Mathf.RoundToInt(globalContamination)}%";
        }

        if (localGaugeText != null)
        {
            localGaugeText.text =
                $"Lokal: {Mathf.RoundToInt(localContamination)}%";
        }
    }

    private void TriggerGameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;

        Debug.Log(
            "GAME OVER! Kontaminasi Global mencapai 100%."
        );

        // Pastikan Gauge Global menunjukkan 100%.
        globalContamination = 100f;
        UpdateGaugeUI();

        // Tampilkan Panel Game Over.
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Hentikan seluruh waktu permainan.
        Time.timeScale = 0f;
    }
}