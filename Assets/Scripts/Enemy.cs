using UnityEngine;
using TMPro;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public int MaxHP { get; protected set; }
    public int CurrentHP { get; protected set; }
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

    private Color _originalColor;
    private float _flashTimer;
    private bool _isFlashing;

    void Awake()
    {
        if (SpriteRenderer == null)
            SpriteRenderer = GetComponent<SpriteRenderer>();
        if (SpriteRenderer != null)
            _originalColor = SpriteRenderer.color;

        if (usesLabel) HPLabel.gameObject.SetActive(true); else HPLabel.gameObject.SetActive(false);
        if (usesVisual) HPVisual.gameObject.SetActive(true); else HPVisual.gameObject.SetActive(false);
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
    public void Initialize(int resolvedHP, int essenceReward)
    {
        MaxHP = EnemyStats.ComputeMaxHP(resolvedHP);
        CurrentHP = MaxHP;
        EssenceReward = essenceReward;
        UpdateHPVisual();
    }

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

    void UpdateHPVisual()
    {
        if (HPLabel != null && usesLabel) HPLabel.text = CurrentHP.ToString();
        if (HPVisual != null && usesVisual) HPVisual.localScale = Vector3.one * ((float)CurrentHP / MaxHP);
    }

    void FlashDamage()
    {
        if (SpriteRenderer == null) return;
        SpriteRenderer.color = DamageFlashColor;
        _flashTimer = FlashDuration;
        _isFlashing = true;
    }
}