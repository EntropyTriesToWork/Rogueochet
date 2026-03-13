using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PaddleController : MonoBehaviour
{
    public static PaddleController Instance { get; private set; }

    #region Inspector

    [Header("Movement")]
    public float MoveSpeed = 8f;
    public bool UseMouse = true;

    [Header("Size")]
    public float PaddleHalfHeight = 0.6f;
    [Tooltip("Small gap kept between the paddle edge and the wall inner edge.")]
    public float PaddleOffset = 0.1f;

    [Header("Size Animation")]
    [Tooltip("Scale units per second the paddle grows/shrinks. Not a lerp.")]
    public float SizeChangeSpeed = 4f;

    #endregion

    #region Private State

    private float _minY;
    private float _maxY;
    private Camera _cam;
    private bool _inputEnabled = true;

    private Coroutine _sizeCoroutine;

    // The Y coordinate of the inner wall edge, set by WallController each frame during animation.
    // Falls back to camera orthographicSize when WallController is not present.
    private float _playBoundaryY;

    #endregion

    #region Lifecycle

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
    }

    void Start()
    {
        // Default bounds from screen size until WallController overrides them
        _playBoundaryY = _cam != null ? _cam.orthographicSize : 5f;
        ApplySizeImmediate(PaddleHalfHeight);
        SubscribeToEvents();
    }

    void OnDestroy() => UnsubscribeFromEvents();

    #endregion

    #region Events

    void SubscribeToEvents()
    {
        GameEvents.OnRoundStarted += () => _inputEnabled = true;
        GameEvents.OnRoundEnded   += () => _inputEnabled = false;
        GameEvents.OnShopOpened   += () => _inputEnabled = false;
        GameEvents.OnGameOver     += () => _inputEnabled = false;
        GameEvents.OnVictory      += () => _inputEnabled = false;
    }

    void UnsubscribeFromEvents()
    {
        GameEvents.OnRoundStarted -= () => _inputEnabled = true;
        GameEvents.OnRoundEnded   -= () => _inputEnabled = false;
        GameEvents.OnShopOpened   -= () => _inputEnabled = false;
        GameEvents.OnGameOver     -= () => _inputEnabled = false;
        GameEvents.OnVictory      -= () => _inputEnabled = false;
    }

    #endregion

    #region Update

    void Update()
    {
        if (!_inputEnabled) return;

        float targetY = transform.position.y;

        if (UseMouse)
        {
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            targetY = mouseWorld.y;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            targetY = transform.position.y + MoveSpeed * Time.unscaledDeltaTime;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            targetY = transform.position.y - MoveSpeed * Time.unscaledDeltaTime;

        float newY = Mathf.MoveTowards(transform.position.y, targetY, MoveSpeed * Time.unscaledDeltaTime);
        newY = Mathf.Clamp(newY, _minY, _maxY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    #endregion

    #region Bounds
    public void SetPlayBounds(float innerEdgeY)
    {
        _playBoundaryY = innerEdgeY;
        RecalculateBounds();
    }

    void RecalculateBounds()
    {
        _minY = -_playBoundaryY + PaddleHalfHeight + PaddleOffset;
        _maxY =  _playBoundaryY - PaddleHalfHeight - PaddleOffset;
    }

    #endregion

    #region Size
    public void UpdatePaddle(float newHalfHeight)
    {
        PaddleHalfHeight = newHalfHeight;
        RecalculateBounds();

        if (_sizeCoroutine != null) StopCoroutine(_sizeCoroutine);
        _sizeCoroutine = StartCoroutine(AnimateSize(newHalfHeight));
    }
    public void ApplySizeImmediate(float halfHeight)
    {
        PaddleHalfHeight = halfHeight;
        transform.localScale = new Vector3(
            transform.localScale.x,
            halfHeight * 2f,
            transform.localScale.z);
        RecalculateBounds();
    }

    IEnumerator AnimateSize(float targetHalfHeight)
    {
        float targetScaleY = targetHalfHeight * 2f;

        while (!Mathf.Approximately(transform.localScale.y, targetScaleY))
        {
            float next = Mathf.MoveTowards(
                transform.localScale.y,
                targetScaleY,
                SizeChangeSpeed * 2f * Time.unscaledDeltaTime);

            transform.localScale = new Vector3(transform.localScale.x, next, transform.localScale.z);
            yield return null;
        }

        transform.localScale = new Vector3(transform.localScale.x, targetScaleY, transform.localScale.z);
        _sizeCoroutine = null;
    }

    #endregion

    #region Collisions

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.TryGetComponent(out Enemy enemy))
        {
            GameEvents.EnemyReachedPaddle(enemy);
            enemy.OnReachedPaddle();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out Enemy enemy))
        {
            GameEvents.EnemyReachedPaddle(enemy);
            enemy.OnReachedPaddle();
        }
    }

    #endregion
}
