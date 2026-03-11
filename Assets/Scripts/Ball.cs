using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    [Header("Damage")]
    public int Damage = 1;
    public float InitialSpeed = 5;

    [Header("Ball HP")]
    public int MaxHP = 5;
    public int CurrentHP { get; private set; }

    [Header("Physics Correction")]
    [Tooltip("Minimum horizontal speed to prevent vertical-lock bouncing.")]
    public float MinHorizontalSpeed = 2f;
    [Tooltip("Random Y force range added on paddle bounce to prevent horizontal lock.")]
    public float PaddleDeflectionRandomRange = 3f;

    [Header("Speed Ramp")]
    [Tooltip("Target speed when the ramp timer fires (5x the launch speed is set by BallManager).")]
    public float MaxRampSpeed = 50f;
    [Tooltip("How quickly the ball accelerates toward MaxRampSpeed after the ramp triggers.")]
    public float RampAcceleration = 20f;

    [Header("Death Line")]
    [Tooltip("X position to the LEFT of which the ball is considered lost (behind the paddle).")]
    public float DeathLineX = -8f;

    private Rigidbody2D _rb;
    private bool _launched = false;
    private bool _rampActive = false;
    private bool _isDead = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void OnEnable()
    {
        GameEvents.OnBallSpeedRampTriggered += HandleSpeedRamp;
    }

    void OnDisable()
    {
        GameEvents.OnBallSpeedRampTriggered -= HandleSpeedRamp;
    }

    public void Launch(Vector2 direction)
    {
        CurrentHP = MaxHP;
        _rb.linearVelocity = direction * InitialSpeed;
        _launched = true;
        GameEvents.BallHPChanged(this, CurrentHP, MaxHP);
    }

    void Update()
    {
        if (!_launched || _isDead) return;

        if (transform.position.x < DeathLineX)
        {
            Die(lost: true);
            return;
        }

        // Gradually ramp speed up to MaxRampSpeed once triggered
        if (_rampActive && _rb.linearVelocity.sqrMagnitude > 0)
        {
            float current = _rb.linearVelocity.magnitude;
            if (current < MaxRampSpeed)
            {
                float newSpeed = Mathf.MoveTowards(current, MaxRampSpeed, RampAcceleration * Time.deltaTime);
                _rb.linearVelocity = _rb.linearVelocity.normalized * newSpeed;
            }
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDead) return;

        if (col.gameObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(Damage);
            TakeHPDamage(5);  // hitting an enemy costs the ball 2 hp
        }
        else if (col.gameObject.TryGetComponent<PaddleController>(out _))
        {
            ApplyPaddleDeflection();
        }
        else //Wall
        {
            TakeHPDamage(1);
        }

        CorrectHorizontalSpeed();
    }
    void TakeHPDamage(int amt)
    {
        CurrentHP -= amt;
        GameEvents.BallHPChanged(this, CurrentHP, MaxHP);

        if (CurrentHP <= 0)
            Die(lost: false);
    }

    void Die(bool lost)
    {
        if (_isDead) return;
        _isDead = true;

        if (lost)
            BallManager.Instance.OnBallLost(this);   // fell behind paddle
        else
            BallManager.Instance.OnBallDestroyed(this); // ran out of HP
    }
    void ApplyPaddleDeflection() // Adds a small random Y component on paddle bounce so the ball never gets locked into a perfectly horizontal back-and-forth.
    {
        Vector2 vel = _rb.linearVelocity;
        float nudge = Random.Range(-PaddleDeflectionRandomRange, PaddleDeflectionRandomRange);
        vel.y += nudge;
        _rb.linearVelocity = vel.normalized * vel.magnitude;
    }
    void HandleSpeedRamp()
    {
        _rampActive = true;
    }

    public void ActivateRamp()
    {
        _rampActive = true;
    }

    void CorrectHorizontalSpeed()
    {
        Vector2 vel = _rb.linearVelocity;
        if (Mathf.Abs(vel.x) < MinHorizontalSpeed)
        {
            float sign = vel.x >= 0f ? 1f : -1f;
            vel.x = MinHorizontalSpeed * sign;
            _rb.linearVelocity = vel.normalized * _rb.linearVelocity.magnitude;
        }
    }

    public Vector2 Velocity => _rb.linearVelocity;
    public float   Speed    => _rb.linearVelocity.magnitude;

    public void SetSpeed(float speed)
    {
        if (_rb.linearVelocity.sqrMagnitude > 0)
            _rb.linearVelocity = _rb.linearVelocity.normalized * speed;
    }
}

