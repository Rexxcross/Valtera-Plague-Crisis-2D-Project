using UnityEngine;

public enum PowerUpType
{
    Kapur,   // gauge berhenti naik beberapa detik
    Ramuan   // gauge lokal turun langsung
}

/// <summary>
/// Item yang bisa diambil Player. Pasang di objek ber-sprite dengan Collider2D
/// (isTrigger diaktifkan otomatis).
/// </summary>
public class PowerUpPickup : MonoBehaviour
{
    [SerializeField] private PowerUpType type = PowerUpType.Kapur;

    [Header("Efek")]
    [SerializeField, Min(0f)] private float freezeSeconds = 10f;
    [SerializeField, Min(0f)] private float localReduction = 25f;

    [Header("Animasi & SFX")]
    [SerializeField, Min(0f)] private float bobHeight = 0.1f;
    [SerializeField, Min(0f)] private float bobSpeed = 3f;
    [SerializeField] private AudioClip pickupSFX;

    private Vector3 startPosition;
    private ContaminationGaugeManager manager;

    private void Reset()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }

    private void Awake()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        startPosition = transform.position;

        manager = FindAnyObjectByType<ContaminationGaugeManager>();
    }

    private void Update()
    {
        // Melayang naik-turun agar mudah terlihat.
        transform.position = startPosition +
            Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponentInParent<PlayerSweep>() == null)
            return;

        if (manager == null)
            return;

        switch (type)
        {
            case PowerUpType.Kapur:
                manager.FreezeRise(freezeSeconds);
                break;

            case PowerUpType.Ramuan:
                manager.ReduceLocal(localReduction);
                break;
        }

        if (pickupSFX != null)
            AudioSource.PlayClipAtPoint(pickupSFX, transform.position);

        Destroy(gameObject);
    }
}
