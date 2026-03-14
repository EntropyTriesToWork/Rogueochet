using UnityEngine;

/// <summary>
/// Fired by ShootingEnemy. Travels left toward the paddle.
/// Touching the paddle deals damage and destroys the projectile.
/// A ball with CanBreakProjectiles destroys it on contact.
/// A ball without that flag bounces off it normally.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class EnemyProjectile : MonoBehaviour
{
    #region Inspector

    [Tooltip("World-space units per second the projectile travels left.")]
    public float Speed = 5f;
    [Tooltip("Damage dealt to the player when the projectile hits the paddle.")]
    public int Damage = 1;
    [Tooltip("Destroyed automatically when it travels this far left of its spawn X.")]
    public float MaxTravelDistance = 20f;

    #endregion

    #region Private State

    private float _startX;
    private bool  _dead = false;

    #endregion

    #region Lifecycle

    void Start()
    {
        _startX = transform.position.x;

        // Ensure the collider is a trigger so it doesn't physically push the paddle
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void Update()
    {
        if (_dead) return;

        transform.position += Vector3.left * Speed * Time.deltaTime;

        if (transform.position.x < _startX - MaxTravelDistance)
            Destroy(gameObject);
    }

    #endregion

    #region Collision

    void OnTriggerEnter2D(Collider2D other)
    {
        if (_dead) return;

        if (other.TryGetComponent(out PaddleController _))
        {
            GameManager.Instance.TakeDamage(Damage);
            Destroy(gameObject);
            return;
        }

        if (other.TryGetComponent(out Ball ball))
        {
            if (ball.CanBreakProjectiles)
                Deflect();
            // Balls without the flag are handled by Ball.OnCollisionEnter2D
        }
    }

    #endregion

    #region Public API

    /// <summary>Destroys the projectile, called by a ball that can break projectiles.</summary>
    public void Deflect()
    {
        if (_dead) return;
        _dead = true;
        Destroy(gameObject);
    }

    #endregion
}
