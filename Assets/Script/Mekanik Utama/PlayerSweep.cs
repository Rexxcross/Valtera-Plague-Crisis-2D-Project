using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    [Header("Progress Keseluruhan")]
    [SerializeField, Min(1)]
    private int totalDustRequired = 5;

    [SerializeField] private TMP_Text progressText;

    private SweepableArea currentArea;
    private int cleanedDust = 0;

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

        UpdateProgressText();
    }

    private void Update()
    {
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

                if (completed)
                {
                    cleanedDust = Mathf.Min(
                        cleanedDust + 1,
                        totalDustRequired
                    );

                    if (contaminationManager != null)
                        contaminationManager.RegisterCleaningSuccess();

                    currentArea = null;
                    sweepInProgress = false;

                    UpdateProgressText();
                }
            }
            else
            {
                if (sweepInProgress)
                {
                    currentArea.CancelSweep();

                    if (contaminationManager != null)
                        contaminationManager.RegisterFailedSweep();

                    sweepInProgress = false;
                }

                if (sweepSlider != null)
                    sweepSlider.value = 0f;
            }
        }

        UpdateInteractionUI(holdingE);
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
            return;

        // Tempatkan UI di atas kepala Player.
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

    private void OnTriggerEnter2D(Collider2D other)
    {
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
            if (sweepInProgress)
            {
                if (contaminationManager != null)
                    contaminationManager.RegisterFailedSweep();
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
                interactionText.gameObject.SetActive(false);
        }
    }

    private void UpdateInteractionUI(bool holdingE)
    {
        bool nearDust =
            currentArea != null && !currentArea.IsCleaned;

        bool showPrompt = nearDust && !holdingE;
        bool showSlider = nearDust && holdingE;

        if (interactionText != null)
        {
            if (interactionText.gameObject.activeSelf != showPrompt)
                interactionText.gameObject.SetActive(showPrompt);
        }

        if (sweepSlider != null)
        {
            if (sweepSlider.gameObject.activeSelf != showSlider)
                sweepSlider.gameObject.SetActive(showSlider);
        }
    }

    private void UpdateProgressText()
    {
        int percentage = Mathf.RoundToInt(
            cleanedDust / (float)totalDustRequired * 100f
        );

        if (progressText != null)
        {
            progressText.text =
                $"Progress: {percentage}% ";
        }
    }
}
