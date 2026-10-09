using UnityEngine;
using UnityEngine.InputSystem;

public class ParallaxMainMenu : MonoBehaviour
{
    [Header("Gerakan Otomatis")]
    public float strength = 0.5f;        // jarak ayun maksimum (makin dekat kamera, makin besar)
    public float speed = 0.3f;           // kecepatan ayun (0.1 = sangat pelan, 0.5 = cukup cepat)
    public float verticalAmount = 0.3f;  // 0 = hanya kiri-kanan, 1 = sama kuat dengan horizontal

    [Header("Mouse (Opsional)")]
    public bool useMouse = false;        // centang kalau mau gerak otomatis + ikut mouse
    public float smoothing = 5f;

    Vector3 startPos;
    Vector3 mouseOffset;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // gerakan otomatis
        float t = Time.time * speed;
        Vector3 auto = new Vector3(
            Mathf.Sin(t),
            Mathf.Cos(t * 0.7f) * verticalAmount,
            0f
        ) * strength;

        // gerakan mouse (hanya kalau diaktifkan)
        Vector3 mouseTarget = Vector3.zero;
        if (useMouse && Mouse.current != null)
        {
            Vector2 m = Mouse.current.position.ReadValue();
            float x = Mathf.Clamp((m.x / Screen.width - 0.5f) * 2f, -1f, 1f);
            float y = Mathf.Clamp((m.y / Screen.height - 0.5f) * 2f, -1f, 1f);
            mouseTarget = new Vector3(-x, -y, 0f) * strength;
        }
        mouseOffset = Vector3.Lerp(mouseOffset, mouseTarget, smoothing * Time.deltaTime);

        transform.position = startPos + auto + mouseOffset;
    }
}