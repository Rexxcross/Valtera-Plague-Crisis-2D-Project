
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

    [SerializeField] private string sectorSelectionSceneName =
        "SectorSelection";

    private bool sectorCompleted = false;

    [Header("Progress Keseluruhan")]
    [SerializeField, Min(1)]
    private int totalDustRequired = 5;

    [SerializeField] private TMP_Text progressText;

    private SweepableArea currentArea;
    private int cleanedDust = 0;


    // =====================================
    // START
    // =====================================

    private void Start()
    {
        totalDustRequired = Mathf.Max(1, totalDustRequired);

        if (mainCamera == null)
            mainCamera = Camera.main;

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
        // Jangan jalankan mekanik setelah sektor selesai.
        if (sectorCompleted)
            return;

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
                    cleanedDust = Mathf.Min(
                        cleanedDust + 1,
                        totalDustRequired
                    );

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

                    Debug.Log(
                        "Debu berhasil dibersihkan! " +
                        cleanedDust + "/" + totalDustRequired
                    );

                    // Periksa apakah seluruh debu sudah bersih.
                    if (cleanedDust >= totalDustRequired)
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
    // TRIGGER ENTER
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


    // =====================================
    // TRIGGER EXIT
    // =====================================

    private void OnTriggerExit2D(Collider2D other)
    {
        SweepableArea area =
            other.GetComponentInParent<SweepableArea>();

        if (area != null && currentArea == area)
        {
            if (sweepInProgress)
            {
                if (contaminationManager != null)
                {
                    contaminationManager.RegisterFailedSweep();
                }
            }

            area.CancelSweep();

            currentArea = null;
            sweepInProgress = false;

            if (sweepSlider != null)
            {
                sweepSlider.value = 0f;
                sweepSlider.gameObject.SetActive(false);
            }

            if (interactionText != null)
            {
                interactionText.gameObject.SetActive(false);
            }
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

        if (interactionText != null)
        {
            if (interactionText.gameObject.activeSelf != showPrompt)
            {
                interactionText.gameObject.SetActive(showPrompt);
            }
        }

        if (sweepSlider != null)
        {
            if (sweepSlider.gameObject.activeSelf != showSlider)
            {
                sweepSlider.gameObject.SetActive(showSlider);
            }
        }
    }


    // =====================================
    // UPDATE PROGRESS TEXT
    // =====================================

    private void UpdateProgressText()
    {
        int percentage = Mathf.RoundToInt(
            cleanedDust / (float)totalDustRequired * 100f
        );

        if (progressText != null)
        {
            progressText.text =
                $"Progress: {percentage}%";
        }
    }


    // =====================================
    // SECTOR COMPLETED
    // =====================================

    private void CompleteSector()
    {
        if (sectorCompleted)
            return;

        sectorCompleted = true;

        Debug.Log(
            "SECTOR COMPLETED! Semua debu berhasil dibersihkan."
        );

        // Sembunyikan UI interaksi.
        if (interactionText != null)
            interactionText.gameObject.SetActive(false);

        if (sweepSlider != null)
            sweepSlider.gameObject.SetActive(false);

        // Tampilkan panel Sector Complete.
        if (sectorCompletePanel != null)
        {
            sectorCompletePanel.SetActive(true);
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
    // NEXT SECTOR - SECTOR SELECTION
    // =====================================

    public void NextSector()
    {
        Debug.Log("Menuju Sector Selection.");

        // Aktifkan kembali waktu permainan.
        Time.timeScale = 1f;

        // Pindah ke halaman pemilihan sektor.
        SceneManager.LoadScene(sectorSelectionSceneName);
    }
}
