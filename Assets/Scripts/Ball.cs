using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    [Header("Damage")]
    public int Damage = 1;
    public float InitialSpeed = 5f;

    [Header("Ball Durability")]
    public int MaxDurability = 5;
    public int CurrentDurability { get; private set; }

    [Header("Physics Correction")]
    [Tooltip("Minimum horizontal speed to prevent vertical-lock bouncing.")]
    public float MinHorizontalSpeed = 2f;
    [Tooltip("Random Y force range added on paddle bounce to prevent horizontal lock.")]
    public float PaddleDeflectionRandomRange = 3f;

    [Header("Speed Ramp")]
    [Tooltip("Target timeScale to ramp up to. Set by BallManager.")]
    public float MaxRampSpeed = 50f;     // kept for SpawnExtraBall compatibility; unused when using timeScale ramp
    [Tooltip("How quickly the ball accelerates toward MaxRampSpeed after the ramp triggers.")]
    public float RampAcceleration = 20f;

    [Header("Death Line")]
    [Tooltip("X position to the LEFT of which the ball is considered lost (behind the paddle).")]
    public float DeathLineX = -8f;

    private Rigidbody2D _rb;
    private bool _launched = false;
    private bool _isDead = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale = 0f;
        _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }
    public void Launch(Vector2 direction)
    {
        CurrentDurability = MaxDurability;
        _rb.linearVelocity = direction.normalized * InitialSpeed;
        _launched = true;
        GameEvents.BallDurabilityChanged(this, CurrentDurability, MaxDurability);
    }

    void Update()
    {
        if (!_launched || _isDead) return;

        if (transform.position.x < DeathLineX)
        {
            Die(lost: true);
            return;
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDead) return;

        if (col.gameObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(Damage);
            LoseDurability(5);      // hitting an enemy costs more durability
        }
        else if (col.gameObject.TryGetComponent<PaddleController>(out _))
        {
            ApplyPaddleDeflection();
        }
        else
        {
            // Wall hit
            LoseDurability(1);
        }

        CorrectHorizontalSpeed();
    }

    void LoseDurability(int amt)
    {
        CurrentDurability -= amt;
        GameEvents.BallDurabilityChanged(this, CurrentDurability, MaxDurability);

        if (CurrentDurability <= 0)
            Die(lost: false);
    }

    void Die(bool lost)
    {
        if (_isDead) return;
        _isDead = true;

        if (lost)
            BallManager.Instance.OnBallLost(this);      // fell behind paddle
        else
            BallManager.Instance.OnBallDestroyed(this); // ran out of durability
    }
    void ApplyPaddleDeflection()
    {
        Vector2 vel = _rb.linearVelocity;
        float nudge = Random.Range(-PaddleDeflectionRandomRange, PaddleDeflectionRandomRange);
        vel.y += nudge;
        _rb.linearVelocity = vel.normalized * vel.magnitude;
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

