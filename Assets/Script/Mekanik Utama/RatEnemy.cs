using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Tikus pembawa wabah. Ia mencari area yang SUDAH bersih lalu mengotorinya lagi.
/// Kabur jika Player mendekat, dan bisa dipukul (Spasi) agar pingsan sementara.
/// Bisa dipakai juga untuk lalat: kecepatan tinggi, waktu mengotori singkat, penalti kecil.
/// </summary>
public class RatEnemy : MonoBehaviour
{
    public static readonly List<RatEnemy> All = new List<RatEnemy>();

    private enum State { Idle, Hunting, Contaminating, Fleeing, Stunned }

    [Header("Gerakan")]
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [SerializeField, Min(0f)] private float fleeSpeed = 4.5f;
    [SerializeField, Min(0f)] private float wanderRadius = 3f;
    [SerializeField] private bool flipWithDirection = true;

    [Header("Perilaku")]
    [Tooltip("Tikus kabur jika Player lebih dekat dari jarak ini")]
    [SerializeField, Min(0f)] private float fleeRadius = 2f;
    [SerializeField, Min(0f)] private float fleeDuration = 1.2f;
    [Tooltip("Jeda sebelum tikus mencari area bersih berikutnya")]
    [SerializeField, Min(0.5f)] private float huntInterval = 6f;

    [Header("Menyebarkan Wabah")]
    [SerializeField, Min(0f)] private float contaminateTime = 1.5f;
    [SerializeField, Min(0f)] private float localPenalty = 6f;
    [SerializeField, Min(0f)] private float globalPenalty = 1f;

    [Header("Dipukul")]
    [SerializeField, Min(0f)] private float stunDuration = 3f;
    [SerializeField, Min(0f)] private float knockbackDistance = 1.5f;

    private State state = State.Idle;
    private float timer;
    private float huntTimer;

    private Vector3 home;
    private Vector3 wanderPoint;
    private float wanderPause;

    private SweepableArea target;
    private Transform player;
    private ContaminationGaugeManager manager;

    private SpriteRenderer sr;
    private Color baseColor = Color.white;

    public bool IsStunned => state == State.Stunned;


    // =====================================
    // LIFECYCLE
    // =====================================

    private void OnEnable() { All.Add(this); }

    private void OnDisable() { All.Remove(this); }

    private void Start()
    {
        home = transform.position;

        sr = GetComponentInChildren<SpriteRenderer>();

        if (sr != null)
            baseColor = sr.color;

        GameObject playerObject =
            GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;

        manager = FindAnyObjectByType<ContaminationGaugeManager>();

        huntTimer = Random.Range(huntInterval * 0.5f, huntInterval);

        PickWanderPoint();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        switch (state)
        {
            case State.Idle: UpdateIdle(dt); break;
            case State.Hunting: UpdateHunting(dt); break;
            case State.Contaminating: UpdateContaminating(dt); break;
            case State.Fleeing: UpdateFleeing(dt); break;
            case State.Stunned: UpdateStunned(dt); break;
        }
    }


    // =====================================
    // STATE
    // =====================================

    private void UpdateIdle(float dt)
    {
        // Berkeliaran santai di sekitar titik awal.
        if (wanderPause > 0f)
        {
            wanderPause -= dt;
        }
        else
        {
            MoveTowards(wanderPoint, moveSpeed * 0.5f, dt);

            if (Distance(transform.position, wanderPoint) < 0.1f)
            {
                wanderPause = Random.Range(0.5f, 2f);
                PickWanderPoint();
            }
        }

        huntTimer -= dt;

        if (huntTimer <= 0f)
        {
            if (!TryStartHunt())
                huntTimer = 2f; // belum ada area bersih, coba lagi nanti.
        }
    }

    private void UpdateHunting(float dt)
    {
        if (target == null || !target.IsCleaned)
        {
            ResetToIdle(2f);
            return;
        }

        if (PlayerTooClose())
        {
            StartFlee();
            return;
        }

        Vector3 goal = target.transform.position;

        MoveTowards(goal, moveSpeed, dt);

        if (Distance(transform.position, goal) < 0.25f)
        {
            state = State.Contaminating;
            timer = contaminateTime;
        }
    }

    private void UpdateContaminating(float dt)
    {
        if (target == null || !target.IsCleaned)
        {
            ResetToIdle(2f);
            return;
        }

        // Player mendekat = rencana tikus digagalkan.
        if (PlayerTooClose())
        {
            StartFlee();
            return;
        }

        timer -= dt;

        // Berkedip saat sedang mengotori.
        if (sr != null)
        {
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 18f);
            sr.color = Color.Lerp(baseColor, new Color(0.6f, 1f, 0.4f, 1f), blink);
        }

        if (timer <= 0f)
        {
            if (target.Recontaminate() && manager != null)
                manager.AddContamination(localPenalty, globalPenalty);

            RestoreColor();
            ResetToIdle(huntInterval);
        }
    }

    private void UpdateFleeing(float dt)
    {
        Vector2 away = player != null
            ? (Vector2)(transform.position - player.position)
            : Random.insideUnitCircle;

        if (away.sqrMagnitude < 0.0001f)
            away = Vector2.up;

        away.Normalize();

        Vector3 goal = transform.position + (Vector3)(away * 2f);

        MoveTowards(goal, fleeSpeed, dt);

        timer -= dt;

        if (timer <= 0f)
            ResetToIdle(huntInterval * 0.5f);
    }

    private void UpdateStunned(float dt)
    {
        timer -= dt;

        if (timer <= 0f)
        {
            RestoreColor();
            ResetToIdle(huntInterval);
        }
    }


    // =====================================
    // AKSI
    // =====================================

    private bool TryStartHunt()
    {
        SweepableArea best = null;
        float bestDistance = float.MaxValue;

        foreach (SweepableArea area in SweepableArea.All)
        {
            if (area == null || !area.IsCleaned)
                continue;

            if (IsTargetedByOtherRat(area))
                continue;

            float d = Distance(transform.position, area.transform.position);

            if (d < bestDistance)
            {
                bestDistance = d;
                best = area;
            }
        }

        if (best == null)
            return false;

        target = best;
        state = State.Hunting;

        return true;
    }

    private bool IsTargetedByOtherRat(SweepableArea area)
    {
        foreach (RatEnemy rat in All)
        {
            if (rat != this && rat.target == area)
                return true;
        }

        return false;
    }

    private void StartFlee()
    {
        target = null;
        state = State.Fleeing;
        timer = fleeDuration;

        RestoreColor();
    }

    // Dipanggil PlayerBroomSwing saat Player memukul.
    public void Stun(Vector2 hitOrigin)
    {
        Vector2 direction =
            (Vector2)transform.position - hitOrigin;

        if (direction.sqrMagnitude < 0.0001f)
            direction = Vector2.up;

        direction.Normalize();

        transform.position += (Vector3)(direction * knockbackDistance);

        target = null;
        state = State.Stunned;
        timer = stunDuration;

        if (sr != null)
            sr.color = new Color(0.6f, 0.7f, 1f, 1f);
    }

    private void ResetToIdle(float nextHuntDelay)
    {
        target = null;
        state = State.Idle;
        huntTimer = nextHuntDelay;

        RestoreColor();
        PickWanderPoint();
    }


    // =====================================
    // HELPER
    // =====================================

    private bool PlayerTooClose()
    {
        return player != null &&
            Distance(transform.position, player.position) < fleeRadius;
    }

    private void PickWanderPoint()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRadius;

        wanderPoint = home + (Vector3)offset;
    }

    private void MoveTowards(Vector3 goal, float speed, float dt)
    {
        Vector3 from = transform.position;

        Vector3 to = Vector3.MoveTowards(
            from,
            new Vector3(goal.x, goal.y, from.z),
            speed * dt
        );

        if (flipWithDirection && sr != null &&
            Mathf.Abs(to.x - from.x) > 0.001f)
        {
            sr.flipX = to.x < from.x;
        }

        transform.position = to;
    }

    private void RestoreColor()
    {
        if (sr != null)
            sr.color = baseColor;
    }

    private static float Distance(Vector3 a, Vector3 b)
    {
        return ((Vector2)a - (Vector2)b).magnitude;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, fleeRadius);
    }
}
