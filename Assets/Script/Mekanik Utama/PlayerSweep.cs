using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class PlayerSweep : MonoBehaviour
{
    [Header("Contamination Manager")]
    [SerializeField] private ContaminationGaugeManager contaminationManager;

    private bool sweepInProgress = false;

    [Header("Interaksi")]
    [Tooltip("Wadah prompt (latar + teks + slider). Jika diisi, wadah inilah yang mengikuti Player.")]
    [SerializeField] private RectTransform promptRoot;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private Slider sweepSlider;
    [SerializeField] private Camera mainCamera;

    [SerializeField] private Vector3 uiOffset =
        new Vector3(0f, 1.2f, 0f);

    [Header("Cleaning SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip cleaningSFX;

    [Header("Sector Completion")]
    [SerializeField] private GameObject sectorCompletePanel;
    [SerializeField] private TMP_Text victoryStatsText;

    private bool sectorCompleted = false;

    [Header("Progress Keseluruhan")]
    [SerializeField] private TMP_Text progressText;

    private SweepableArea currentArea;
    private int shownCleaned = -1;
    private int shownTotal = -1;


    // =====================================
    // START
    // =====================================

    private void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (promptRoot != null)
            promptRoot.gameObject.SetActive(false);

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        if (sweepSlider != null)
        {
            sweepSlider.minValue = 0f;
            sweepSlider.maxValue = 1f;
            sweepSlider.value = 0f;
            sweepSlider.interactable = false;
            sweepSlider.gameObject.SetActive(false);
        }

        if (contaminationManager == null)
        {
            contaminationManager =
                FindAnyObjectByType<ContaminationGaugeManager>();
        }

        if (sectorCompletePanel != null)
        {
            sectorCompletePanel.SetActive(false);
        }

        UpdateProgressText();
    }


    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        // Jangan jalankan mekanik setelah sektor selesai / game over.
        if (sectorCompleted)
            return;

        if (contaminationManager != null &&
            contaminationManager.IsGameOver)
        {
            HidePrompt();
            return;
        }

        // Progres dihitung ulang tiap frame (area bisa dikotori lagi oleh tikus).
        UpdateProgressText();

        if (currentArea != null && currentArea.IsCleaned)
            currentArea = null;

        bool holdingE =
            currentArea != null &&
            Keyboard.current != null &&
            Keyboard.current.eKey.isPressed;

        if (currentArea != null)
        {
            if (holdingE)
            {
                sweepInProgress = true;

                if (contaminationManager != null)
                    contaminationManager.RegisterActivity();

                SweepableArea area = currentArea;

                bool completed = area.Sweep(Time.deltaTime);

                if (sweepSlider != null)
                    sweepSlider.value = area.Progress01;

                // Debu selesai dibersihkan.
                if (completed)
                {
                    // Putar SFX.
                    if (audioSource != null && cleaningSFX != null)
                    {
                        audioSource.PlayOneShot(cleaningSFX);
                    }

                    // Kurangi contamination gauge.
                    if (contaminationManager != null)
                    {
                        contaminationManager.RegisterCleaningSuccess();
                    }

                    currentArea = null;
                    sweepInProgress = false;

                    UpdateProgressText();

                    // Sektor selesai jika SEMUA area bersih bersamaan.
                    if (SweepableArea.CleanedCount >=
                        SweepableArea.TotalCount)
                    {
                        CompleteSector();
                        return;
                    }
                }
            }
            else
            {
                // E dilepas sebelum selesai.
                if (sweepInProgress)
                {
                    currentArea.CancelSweep();

                    if (contaminationManager != null)
                    {
                        contaminationManager.RegisterFailedSweep();
                    }

                    sweepInProgress = false;
                }

                if (sweepSlider != null)
                    sweepSlider.value = 0f;
            }
        }

        UpdateInteractionUI(holdingE);
    }


    // =====================================
    // LATE UPDATE - POSISI UI
    // =====================================

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        Vector3 screenPosition =
            mainCamera.WorldToScreenPoint(
                transform.position + uiOffset
            );

        if (promptRoot != null)
        {
            promptRoot.position = screenPosition;
            return;
        }

        // Cara lama: teks dan slider bergerak sendiri-sendiri.
        if (interactionText != null)
        {
            interactionText.transform.position = screenPosition;
        }

        if (sweepSlider != null)
        {
            sweepSlider.transform.position =
                screenPosition + new Vector3(0f, -25f, 0f);
        }
    }


    // =====================================
    // TRIGGER ENTER / STAY / EXIT
    // =====================================

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (sectorCompleted)
            return;

        SweepableArea area =
            other.GetComponentInParent<SweepableArea>();

        if (area != null && !area.IsCleaned)
        {
            currentArea = area;
        }
    }

    // Mengambil area berikutnya jika Player masih berdiri di dekatnya
    // (misalnya setelah area dikotori lagi oleh tikus).
    private void OnTriggerStay2D(Collider2D other)
    {
        if (sectorCompleted || currentArea != null)
            return;

        SweepableArea area =
            other.GetComponentInParent<SweepableArea>();

        if (area != null && !area.IsCleaned)
        {
            currentArea = area;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        SweepableArea area =
            other.GetComponentInParent<SweepableArea>();

        if (area != null && currentArea == area)
        {
            if (sweepInProgress && contaminationManager != null)
            {
                contaminationManager.RegisterFailedSweep();
            }

            area.CancelSweep();

            currentArea = null;
            sweepInProgress = false;

            if (sweepSlider != null)
                sweepSlider.value = 0f;

            HidePrompt();
        }
    }


    // =====================================
    // UPDATE INTERACTION UI
    // =====================================

    private void UpdateInteractionUI(bool holdingE)
    {
        bool nearDust =
            currentArea != null && !currentArea.IsCleaned;

        bool showPrompt = nearDust && !holdingE;
        bool showSlider = nearDust && holdingE;

        if (promptRoot != null)
            SetActive(promptRoot, nearDust);

        SetActive(interactionText, showPrompt);
        SetActive(sweepSlider, showSlider);
    }

    private void HidePrompt()
    {
        SetActive(promptRoot, false);
        SetActive(interactionText, false);
        SetActive(sweepSlider, false);
    }

    private static void SetActive(Component target, bool active)
    {
        if (target != null &&
            target.gameObject.activeSelf != active)
        {
            target.gameObject.SetActive(active);
        }
    }


    // =====================================
    // UPDATE PROGRESS TEXT
    // =====================================

    private void UpdateProgressText()
    {
        int cleaned = SweepableArea.CleanedCount;
        int total = SweepableArea.TotalCount;

        if (cleaned == shownCleaned && total == shownTotal)
            return;

        shownCleaned = cleaned;
        shownTotal = total;

        if (progressText != null)
        {
            progressText.text = $"Kotoran {cleaned}/{total}";
        }
    }


    // =====================================
    // SECTOR COMPLETED (VICTORY)
    // =====================================

    private void CompleteSector()
    {
        if (sectorCompleted)
            return;

        // Jika Game Over sudah terjadi lebih dulu, abaikan.
        if (contaminationManager != null &&
            contaminationManager.IsGameOver)
            return;

        sectorCompleted = true;

        string sceneName = SceneManager.GetActiveScene().name;

        // Buka sektor berikutnya & simpan bintang (tersimpan).
        GameProgress.CompleteSector(sceneName);

        if (contaminationManager != null)
        {
            contaminationManager.StopSector();

            GameProgress.SaveStars(
                sceneName,
                contaminationManager.CalculateStars());
        }

        HidePrompt();

        if (victoryStatsText != null && contaminationManager != null)
            victoryStatsText.text = contaminationManager.BuildVictoryLine();

        // Tampilkan panel Victory.
        if (sectorCompletePanel != null)
        {
            StartCoroutine(PanelFader.FadeIn(sectorCompletePanel));
        }
        else
        {
            Debug.LogWarning(
                "Sector Complete Panel belum dihubungkan!"
            );
        }

        // Hentikan permainan sementara.
        Time.timeScale = 0f;
    }


    // =====================================
    // NEXT SECTOR
    // =====================================

    public void NextSector()
    {
        GameProgress.LoadNextSectorOrSelection();
    }
}