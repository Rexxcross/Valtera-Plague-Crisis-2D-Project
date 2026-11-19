using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    private bool isSprinting;

    // =====================================
    // START
    // =====================================

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        if (rb == null)
        {
            Debug.LogError("Rigidbody2D tidak ditemukan!");
        }

        if (animator == null)
        {
            Debug.LogWarning("Animator tidak ditemukan!");
        }
    }

    // =====================================
    // UPDATE
    // =====================================

    private void Update()
    {
        // Periksa apakah tombol Shift sedang ditahan.
        if (Keyboard.current != null)
        {
            isSprinting =
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed;
        }
        else
        {
            isSprinting = false;
        }
    }

    // =====================================
    // PHYSICS MOVEMENT
    // =====================================

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        // Tentukan kecepatan Player.
        float currentSpeed = isSprinting
            ? runSpeed
            : moveSpeed;

        // Terapkan pergerakan.
        rb.linearVelocity = moveInput * currentSpeed;
    }

    // =====================================
    // INPUT MOVEMENT
    // =====================================

    public void Move(InputAction.CallbackContext context)
    {
        if (context.canceled)
        {
            if (animator != null)
            {
                animator.SetBool("isWalking", false);

                animator.SetFloat("LastInputX", moveInput.x);
                animator.SetFloat("LastInputY", moveInput.y);
            }

            moveInput = Vector2.zero;
            return;
        }

        // Ambil input arah pergerakan.
        moveInput = Vector2.ClampMagnitude(
            context.ReadValue<Vector2>(),
            1f
        );

        if (animator != null)
        {
            bool isWalking = moveInput.sqrMagnitude > 0.01f;

            animator.SetBool("isWalking", isWalking);

            animator.SetFloat("InputX", moveInput.x);
            animator.SetFloat("InputY", moveInput.y);
        }
    }
}
