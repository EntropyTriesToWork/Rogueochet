using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class Ball : MonoBehaviour
{
    #region Inspector

    [Header("Damage")]
    public int Damage = 1;
    public float InitialSpeed = 5f;

    [Header("Durability")]
    public int MaxDurability = 5;
    public int CurrentDurability { get; private set; }

    [Header("Physics Correction")]
    [Tooltip("Minimum horizontal speed to prevent vertical-lock bouncing.")]
    public float MinHorizontalSpeed = 2f;
    [Tooltip("Random Y nudge on paddle bounce to prevent lock.")]
    public float PaddleDeflectionRandomRange = 3f;

    [Header("Projectiles")]
    [Tooltip("When true this ball destroys enemy projectiles on contact instead of taking durability damage.")]
    public bool CanBreakProjectiles = false;

    [Header("Death Line")]
    [Tooltip("X position left of which the ball is considered lost.")]
    public float DeathLineX = -8f;

    #endregion

    #region Private State

    private Rigidbody2D _rb;
    private bool _launched = false;
    private bool _isDead   = false;

    #endregion

    #region Lifecycle

    void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _rb.gravityScale            = 0f;
        _rb.collisionDetectionMode  = CollisionDetectionMode2D.Continuous;
        _rb.interpolation           = RigidbodyInterpolation2D.Interpolate;
        _rb.constraints             = RigidbodyConstraints2D.FreezeRotation;
    }

    void Update()
    {
        if (!_launched || _isDead) return;
        if (transform.position.x < DeathLineX)
            Die(lost: true);
    }

    #endregion

    #region Launch

    public void Launch(Vector2 direction)
    {
        CurrentDurability  = MaxDurability;
        _rb.linearVelocity = direction.normalized * InitialSpeed;
        _launched          = true;
        GameEvents.BallDurabilityChanged(this, CurrentDurability, MaxDurability);
    }

    #endregion

    #region Collision

    void OnCollisionEnter2D(Collision2D col)
    {
        if (_isDead) return;

        if (col.gameObject.TryGetComponent(out Enemy enemy))
        {
            enemy.TakeDamage(Damage);
            LoseDurability(5);
        }
        else if (col.gameObject.TryGetComponent(out EnemyProjectile projectile))
        {
            if (CanBreakProjectiles)
                projectile.Deflect();
            else
                LoseDurability(1);
        }
        else if (col.gameObject.TryGetComponent<PaddleController>(out _))
        {
            ApplyPaddleDeflection();
        }
        else
        {
            LoseDurability(1);
        }

        CorrectHorizontalSpeed();
    }

    #endregion

    #region Durability

    void LoseDurability(int amount)
    {
        CurrentDurability -= amount;
        GameEvents.BallDurabilityChanged(this, CurrentDurability, MaxDurability);
        if (CurrentDurability <= 0)
            Die(lost: false);
    }

    void Die(bool lost)
    {
        if (_isDead) return;
        _isDead = true;

        if (lost) BallManager.Instance.OnBallLost(this);
        else      BallManager.Instance.OnBallDestroyed(this);
    }

    #endregion

    #region Physics Helpers

    void ApplyPaddleDeflection()
    {
        Vector2 vel   = _rb.linearVelocity;
        float   nudge = Random.Range(-PaddleDeflectionRandomRange, PaddleDeflectionRandomRange);
        vel.y        += nudge;
        _rb.linearVelocity = vel.normalized * vel.magnitude;
    }

    void CorrectHorizontalSpeed()
    {
        Vector2 vel = _rb.linearVelocity;
        if (Mathf.Abs(vel.x) < MinHorizontalSpeed)
        {
            vel.x              = MinHorizontalSpeed * (vel.x >= 0f ? 1f : -1f);
            _rb.linearVelocity = vel.normalized * _rb.linearVelocity.magnitude;
        }
    }

    #endregion

    #region Public API

    public Vector2 Velocity => _rb.linearVelocity;
    public float   Speed    => _rb.linearVelocity.magnitude;

    public void SetSpeed(float speed)
    {
        if (_rb.linearVelocity.sqrMagnitude > 0)
            _rb.linearVelocity = _rb.linearVelocity.normalized * speed;
    }

    #endregion
}
