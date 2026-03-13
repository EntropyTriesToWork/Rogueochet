using System.Collections;
using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    #region Inspector

    [Header("Stats")]
    public int MaxHP      { get; protected set; }
    public int CurrentHP  { get; protected set; }
    public int EssenceReward = 1;

    [Header("UI")]
    [Tooltip("Assign a child TextMeshPro to display current HP above the enemy.")]
    public TMP_Text HPLabel;
    public Transform HPVisual;
    public bool usesLabel, usesVisual;

    [Header("Visual Feedback")]
    public SpriteRenderer SpriteRenderer;
    public Color DamageFlashColor = Color.red;
    public float FlashDuration = 0.1f;

    [Header("Spawn Animation")]
    [Tooltip("How long the pop-in scale animation lasts in seconds.")]
    public float PopInDuration = 0.2f;

    #endregion

    #region Private State

    private Color _originalColor;
    private Vector3 _naturalScale;
    private float _flashTimer;
    private bool  _isFlashing;

    #endregion

    #region Lifecycle

    void Awake()
    {
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer != null)
            _originalColor = SpriteRenderer.color;

        _naturalScale = transform.localScale;

        if (usesLabel)  HPLabel.gameObject.SetActive(true);   else HPLabel.gameObject.SetActive(false);
        if (usesVisual) HPVisual.gameObject.SetActive(true);  else HPVisual.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isFlashing) return;
        _flashTimer -= Time.deltaTime;
        if (_flashTimer <= 0f)
        {
            _isFlashing = false;
            if (SpriteRenderer != null)
                SpriteRenderer.color = _originalColor;
        }
    }

    #endregion

    #region Initialisation

    /// <summary>
    /// Initialise with a fully-resolved HP value.
    /// HP is computed by WaveData (BaseHP x entry.HPMultiplier x WaveHPMultiplier),
    /// then EnemyStats global modifiers are applied on top.
    /// </summary>
    public void Initialize(int resolvedHP, int essenceReward)
    {
        MaxHP         = EnemyStats.ComputeMaxHP(resolvedHP);
        CurrentHP     = MaxHP;
        EssenceReward = essenceReward;
        UpdateHPVisual();
        StartCoroutine(PopIn());
    }

    IEnumerator PopIn()
    {
        float elapsed = 0f;
        transform.localScale = Vector3.zero;

        while (elapsed < PopInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / PopInDuration);
            // Overshoot slightly then settle — gives a punchy feel without a full bounce curve
            float scale = t < 0.75f
                ? Mathf.SmoothStep(0f, 1.15f, t / 0.75f)
                : Mathf.Lerp(1.15f, 1f, (t - 0.75f) / 0.25f);
            transform.localScale = _naturalScale * scale;
            yield return null;
        }

        transform.localScale = _naturalScale;
    }

    #endregion

    #region Combat

    public void TakeDamage(int damage)
    {
        if (CurrentHP <= 0) return;
        CurrentHP -= damage;
        UpdateHPVisual();
        FlashDamage();
        GameEvents.EnemyDamaged(this, damage, CurrentHP);
        Debug.Log($"[Enemy] {name} took {damage}. HP: {CurrentHP}/{MaxHP}");
        if (CurrentHP <= 0)
            Die();
    }

    void Die()
    {
        GameEvents.EnemyDied(this, EssenceReward);
        Destroy(gameObject);
    }

    public void OnReachedPaddle()
    {
        EnemyManager.Instance.OnEnemyDied(this);
        Destroy(gameObject);
    }

    #endregion

    #region Visuals

    void UpdateHPVisual()
    {
        if (HPLabel  != null && usesLabel)  HPLabel.text = CurrentHP.ToString();
        if (HPVisual != null && usesVisual) HPVisual.localScale = Vector3.one * ((float)CurrentHP / MaxHP);
    }

    void FlashDamage()
    {
        if (SpriteRenderer == null) return;
        SpriteRenderer.color = DamageFlashColor;
        _flashTimer          = FlashDuration;
        _isFlashing          = true;
    }

    #endregion
}
