using System;
using UnityEngine;

/// <summary>
/// 玩家血量管理：扣血/回血，通过事件通知其他系统
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量参数")]
    public float maxHealth = 100f;

    // 事件：受伤(伤害值)、死亡、血量变化(当前/最大)
    public event Action<float> OnDamaged;
    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;

    private float _currentHealth;

    private void Start()
    {
        _currentHealth = maxHealth;
    }

    /// <summary>扣血，血量归零时触发死亡事件</summary>
    public void TakeDamage(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        if (_currentHealth <= 0f)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>回血，不超过上限</summary>
    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    /// <summary>当前血量</summary>
    public float CurrentHealth => _currentHealth;

    /// <summary>当前血量百分比 0~1</summary>
    public float GetHealthPercent()
    {
        return _currentHealth / maxHealth;
    }
}
