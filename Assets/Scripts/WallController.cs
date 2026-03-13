using System.Collections;
using UnityEngine;

public class WallController : MonoBehaviour
{
    public static WallController Instance { get; private set; }

    #region Inspector

    [Header("Walls")]
    public Transform TopWall;
    public Transform BottomWall;

    [Header("Animation")]
    [Tooltip("Scale units per second. Not a lerp.")]
    public float ScaleSpeed = 3f;

    #endregion

    #region Private State

    private Camera _cam;
    private Coroutine _scaleCoroutine;

    private float _baseScaleY;
    private float _wallBaseHalfThickness;

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
        if (TopWall != null)
        {
            _baseScaleY = TopWall.localScale.y;
            _wallBaseHalfThickness = GetWallHalfThickness(TopWall);
        }
        SetWallScaleY(0.2f);
        GameEvents.OnWaveSetup += HandleWaveSetup;
        GameEvents.OnGameOver  += HandleReset;
        GameEvents.OnVictory   += HandleReset;
    }

    void OnDestroy()
    {
        GameEvents.OnWaveSetup -= HandleWaveSetup;
        GameEvents.OnGameOver  -= HandleReset;
        GameEvents.OnVictory   -= HandleReset;
    }

    #endregion

    #region Handlers

    void HandleWaveSetup(float wallScale) => SetWallScale(wallScale);

    void HandleReset() => SetWallScale(0.2f);

    #endregion

    #region Public API
    public void SetWallScale(float scale)
    {
        if (_scaleCoroutine != null) StopCoroutine(_scaleCoroutine);
        _scaleCoroutine = StartCoroutine(AnimateScale(scale));
    }

    #endregion

    #region Animation

    IEnumerator AnimateScale(float targetScale)
    {
        if (TopWall == null || BottomWall == null) yield break;

        float targetScaleY = _baseScaleY * targetScale;

        while (!Mathf.Approximately(TopWall.localScale.y, targetScaleY))
        {
            float next = Mathf.MoveTowards(
                TopWall.localScale.y,
                targetScaleY,
                ScaleSpeed * Time.unscaledDeltaTime);

            SetWallScaleY(next);
            NotifyPaddle(next);
            yield return null;
        }

        SetWallScaleY(targetScaleY);
        NotifyPaddle(targetScaleY);
        _scaleCoroutine = null;
    }

    void SetWallScaleY(float scaleY)
    {
        if (TopWall != null)
            TopWall.localScale    = new Vector3(TopWall.localScale.x,    scaleY, TopWall.localScale.z);
        if (BottomWall != null)
            BottomWall.localScale = new Vector3(BottomWall.localScale.x, scaleY, BottomWall.localScale.z);
    }

    void NotifyPaddle(float currentScaleY)
    {
        if (PaddleController.Instance == null || _cam == null) return;

        float scaleFactor          = currentScaleY / Mathf.Max(0.0001f, _baseScaleY);
        float currentHalfThickness = _wallBaseHalfThickness * scaleFactor;
        float screenHalfHeight     = _cam.orthographicSize;
        float innerEdge            = screenHalfHeight - currentHalfThickness;

        PaddleController.Instance.SetPlayBounds(innerEdge);
    }

    #endregion

    #region Helpers
    float GetWallHalfThickness(Transform wall)
    {
        Collider2D col = wall.GetComponent<Collider2D>();
        if (col != null) return col.bounds.extents.y;

        SpriteRenderer sr = wall.GetComponent<SpriteRenderer>();
        if (sr != null) return sr.bounds.extents.y;

        return wall.localScale.y * 0.5f;
    }

    #endregion
}
