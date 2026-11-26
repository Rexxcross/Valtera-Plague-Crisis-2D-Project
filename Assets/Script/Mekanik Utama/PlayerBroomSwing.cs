using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Spasi = ayunkan sapu. Tikus di dekat Player terpental dan pingsan sementara.
/// Pasang di objek Player.
/// </summary>
public class PlayerBroomSwing : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float swingRadius = 1.6f;
    [SerializeField, Min(0f)] private float cooldown = 0.8f;

    [Header("SFX (opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip swingSFX;
    [SerializeField] private AudioClip hitSFX;

    private float nextSwingTime = 0f;

    private void Update()
    {
        // Jangan bisa mengayun saat game berhenti (Game Over / Victory).
        if (Time.timeScale == 0f)
            return;

        if (Keyboard.current == null ||
            !Keyboard.current.spaceKey.wasPressedThisFrame)
            return;

        if (Time.time < nextSwingTime)
            return;

        nextSwingTime = Time.time + cooldown;

        bool hitAny = false;

        foreach (RatEnemy rat in RatEnemy.All)
        {
            if (rat == null || rat.IsStunned)
                continue;

            float distance = Vector2.Distance(
                transform.position, rat.transform.position);

            if (distance <= swingRadius)
            {
                rat.Stun(transform.position);
                hitAny = true;
            }
        }

        if (audioSource != null)
        {
            AudioClip clip = hitAny && hitSFX != null ? hitSFX : swingSFX;

            if (clip != null)
                audioSource.PlayOneShot(clip);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, swingRadius);
    }
}
