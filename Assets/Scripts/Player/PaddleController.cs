using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PaddleController : MonoBehaviour
{
    public static PaddleController Instance { get; private set; }

    #region Inspector
    [SerializeField] GameStats _stats;

    [Header("Movement")]
    [SerializeField] float _baseMoveSpeed = 8f;
    public bool UseMouse = true;

    [Header("Size")]
    [SerializeField] float _paddleHalfHeight = 1f;
    float _paddleSizeMultiplier = 1f;
    [Tooltip("Small gap kept between the paddle edge and the wall inner edge.")]
    public float PaddleOffset = 0.1f;

    [Header("Size Animation")]
    [Tooltip("Scale units per second the paddle grows/shrinks. Not a lerp.")]
    [SerializeField] float SizeChangeSpeed = 4f;
    #endregion

    #region Private State
    private float _minY;
    private float _maxY;
    private Camera _cam;
    private bool _inputEnabled = true;

    private Coroutine _sizeCoroutine;
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
        _playBoundaryY = _cam != null ? _cam.orthographicSize : 5f; //Default size until wall controller changes the boundary
        ApplySizeImmediate(_paddleHalfHeight);
        SubscribeToEvents();
    }
    void Update()
    {
        if (!_inputEnabled || PauseManager.Instance.IsPaused) return;
        if(Input.GetKey(KeyCode.Space) || Input.GetMouseButton(0)) { return; }
        float targetY = transform.position.y;

        if (UseMouse)
        {
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            targetY = mouseWorld.y;
            mouseWorld.z = 0;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            targetY = transform.position.y + PaddleSpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            targetY = transform.position.y - PaddleSpeed * Time.deltaTime;

        float newY = Mathf.MoveTowards(transform.position.y, targetY, PaddleSpeed * Time.deltaTime);
        newY = Mathf.Clamp(newY, _minY, _maxY);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
    void OnDestroy() => UnsubscribeFromEvents();
    #endregion

    #region Events
    void SubscribeToEvents()
    {
        GameEvents.OnWaveStarted += (int v) => _inputEnabled = true;
        //GameEvents.OnRoomCleared += () => _inputEnabled = false;
        GameEvents.OnShopOpened += () => _inputEnabled = false;
        GameEvents.OnGameOver += () => _inputEnabled = false;
        GameEvents.OnVictory += () => _inputEnabled = false;
    }
    void UnsubscribeFromEvents()
    {
        GameEvents.OnWaveStarted -= (int v) => _inputEnabled = true;
        //GameEvents.OnRoomCleared -= () => _inputEnabled = false;
        GameEvents.OnShopOpened -= () => _inputEnabled = false;
        GameEvents.OnGameOver -= () => _inputEnabled = false;
        GameEvents.OnVictory -= () => _inputEnabled = false;
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
        _minY = -_playBoundaryY + PaddleHalfSize + PaddleOffset;
        _maxY =  _playBoundaryY - PaddleHalfSize - PaddleOffset;
    }

    #endregion

    #region Size and Speed
    void UpdatePaddle(float newHalfHeight, float sizeMultiplier)
    {
        _paddleHalfHeight = newHalfHeight;
        _paddleSizeMultiplier = sizeMultiplier;
        RecalculateBounds();

        if (_sizeCoroutine != null) StopCoroutine(_sizeCoroutine);
        _sizeCoroutine = StartCoroutine(AnimateSize(newHalfHeight));
    }
    public void ApplySizeImmediate(float halfHeight)
    {
        _paddleHalfHeight = halfHeight;
        transform.localScale = new Vector3(
            transform.localScale.x,
            PaddleHalfSize * 2f,
            transform.localScale.z);
        RecalculateBounds();
    }
    public void ApplySizeBonus(float newHalfHeight, float sizeMultiplier = 0f)
    {
        UpdatePaddle(newHalfHeight + _paddleHalfHeight, sizeMultiplier);
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
    public float PaddleSize => _paddleHalfHeight * 2f * _paddleSizeMultiplier;
    public float PaddleHalfSize => _paddleHalfHeight * _paddleSizeMultiplier;
    public float PaddleSpeed => _baseMoveSpeed + _stats.PaddleSpeedBonus;
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