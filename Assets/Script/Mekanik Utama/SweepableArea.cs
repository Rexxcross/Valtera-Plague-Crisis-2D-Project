using UnityEngine;

public class SweepableArea : MonoBehaviour
{
    [SerializeField, Min(0.1f)]
    private float secondsToClean = 2f;

    private float sweepTime = 0f;
    private bool isCleaned = false;

    public bool IsCleaned => isCleaned;

    public float Progress01 =>
        isCleaned ? 1f :
        Mathf.Clamp01(sweepTime / secondsToClean);

    public bool Sweep(float deltaTime)
    {
        if (isCleaned)
            return false;

        sweepTime += deltaTime;

        if (sweepTime >= secondsToClean)
        {
            isCleaned = true;

            // Hilangkan debu ketika sudah 100%.
            gameObject.SetActive(false);

            return true;
        }

        return false;
    }

    public void CancelSweep()
    {
        if (!isCleaned)
        {
            // Reset jika tombol E dilepas.
            sweepTime = 0f;
        }
    }
}