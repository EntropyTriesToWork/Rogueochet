using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PaddleController : MonoBehaviour
{
    public static PaddleController Instance { get; private set; }

    [Header("Movement")]
    [Tooltip("Units per second the paddle can move. Increase this via shop upgrades.")]
    public float MoveSpeed = 8f;
    public bool UseMouse = true;

    [Header("Bounds")]
    [Tooltip("Half the paddle height — used to clamp position within screen bounds.")]
    public float PaddleHalfHeight = 0.6f;
    public float PaddleOffset = 0.1f;

    private float _minY;
    private float _maxY;
    private Camera _cam;
    private bool _inputEnabled = true;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cam = Camera.main;
    }

    void Start()
    {
        UpdatePaddleSize(PaddleHalfHeight * 2f);
        SubscribeToEvents();
    }

    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    void SubscribeToEvents()
    {
        GameEvents.OnRoundStarted += () => _inputEnabled = true;
        GameEvents.OnRoundEnded += () => _inputEnabled = false;
        GameEvents.OnShopOpened += () => _inputEnabled = false;
        GameEvents.OnGameOver += () => _inputEnabled = false;
        GameEvents.OnVictory += () => _inputEnabled = false;
    }

    void UnsubscribeFromEvents()
    {
        // Lambda subscriptions can't be individually unsubscribed.
        // OnDestroy fires when the GameObject is destroyed, which ends the subscriptions.
    }

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
            targetY = transform.position.y + MoveSpeed * Time.deltaTime;
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            targetY = transform.position.y - MoveSpeed * Time.deltaTime;

        float newY = Mathf.MoveTowards(transform.position.y, targetY, MoveSpeed * Time.deltaTime);
        newY = Mathf.Clamp(newY, _minY, _maxY);

        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    void CalculateBounds()
    {
        if (_cam == null) return;
        float screenHalfHeight = _cam.orthographicSize;
        _minY = -screenHalfHeight + PaddleHalfHeight + PaddleOffset;
        _maxY = screenHalfHeight - PaddleHalfHeight - PaddleOffset;
    }
    public void UpdatePaddleSize(float newSize)
    {
        PaddleHalfHeight = newSize / 2f;
        transform.localScale = new Vector3(transform.localScale.x, newSize, transform.localScale.z);
        CalculateBounds();
    }
    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.TryGetComponent<Enemy>(out Enemy enemy))
        {
            GameEvents.EnemyReachedPaddle(enemy);
            enemy.OnReachedPaddle();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            GameEvents.EnemyReachedPaddle(enemy);
            enemy.OnReachedPaddle();
        }
    }
}