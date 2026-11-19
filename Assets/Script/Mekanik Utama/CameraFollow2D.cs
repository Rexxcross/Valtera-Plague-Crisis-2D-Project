
using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Camera Settings")]
    [SerializeField] private Vector3 offset =
        new Vector3(0f, 0f, -10f);

    [SerializeField, Min(0f)]
    private float smoothTime = 0.15f;

    private Vector3 velocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Tentukan posisi tujuan kamera.
        Vector3 targetPosition = target.position + offset;

        // Gerakkan kamera secara halus mengikuti Player.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );
    }
}