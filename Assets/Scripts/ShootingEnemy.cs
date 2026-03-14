using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to any enemy prefab to give it a ranged attack.
/// Fires EnemyProjectile prefabs at a configurable interval.
/// Shooting pauses when the enemy is dead or the round is not active.
/// </summary>
public class ShootingEnemy : MonoBehaviour
{
    #region Inspector

    [Header("Projectile")]
    public GameObject ProjectilePrefab;

    [Tooltip("Offset from this enemy's position where projectiles spawn.")]
    public Vector2 FireOffset = new Vector2(-0.5f, 0f);

    [Header("Timing")]
    [Tooltip("Seconds between each shot.")]
    public float FireInterval = 2f;
    [Tooltip("Seconds before the first shot after spawning.")]
    public float InitialDelay = 1f;

    #endregion

    #region Private State

    private bool _canShoot = false;
    private Coroutine _shootCoroutine;

    #endregion

    #region Lifecycle

    void Start()
    {
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnRoundEnded   += HandleRoundEnded;
        GameEvents.OnGameOver     += HandleStop;
        GameEvents.OnVictory      += HandleStop;
        GameEvents.OnShopOpened   += HandleStop;
    }

    void OnDestroy()
    {
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnRoundEnded   -= HandleRoundEnded;
        GameEvents.OnGameOver     -= HandleStop;
        GameEvents.OnVictory      -= HandleStop;
        GameEvents.OnShopOpened   -= HandleStop;
    }

    #endregion

    #region Event Handlers

    void HandleRoundStarted()
    {
        _canShoot = true;
        if (_shootCoroutine != null) StopCoroutine(_shootCoroutine);
        _shootCoroutine = StartCoroutine(ShootLoop());
    }

    void HandleRoundEnded()
    {
        _canShoot = false;
        if (_shootCoroutine != null) StopCoroutine(_shootCoroutine);
    }

    void HandleStop()
    {
        _canShoot = false;
        if (_shootCoroutine != null) StopCoroutine(_shootCoroutine);
    }

    #endregion

    #region Shooting

    IEnumerator ShootLoop()
    {
        yield return new WaitForSeconds(InitialDelay);

        while (_canShoot)
        {
            Fire();
            yield return new WaitForSeconds(FireInterval);
        }
    }

    void Fire()
    {
        if (ProjectilePrefab == null) return;

        Vector3 spawnPos = transform.position + (Vector3)FireOffset;
        Instantiate(ProjectilePrefab, spawnPos, Quaternion.identity);
    }

    #endregion
}
