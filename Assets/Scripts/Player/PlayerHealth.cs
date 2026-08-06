using System;
using UnityEngine;

/// <summary>
/// 玩家血量管理：扣血/回血/医疗包，通过事件通知其他系统
/// 阶段2新增：医疗包计数、消耗式治疗
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("血量参数")]
    public float maxHealth = 100f;

    [Header("医疗参数")]
    public int initialMedkits = 3;                   // 初始医疗包数量
    public float healAmount = 30f;                   // 每次治疗恢复血量
    public int maxMedkits = 5;                       // 医疗包携带上限

    // 事件：受伤(伤害值)、死亡、血量变化(当前/最大)、医疗包数量变化
    public event Action<float> OnDamaged;
    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged;
    public event Action<int> OnMedkitChanged;

    private float _currentHealth;
    private int _medkitCount;
    private Animator _animator; // 受击动画驱动器（从角色模型子物体获取）

    private void Start()
    {
        _currentHealth = maxHealth;
        _medkitCount = initialMedkits;
        _animator = GetComponentInChildren<Animator>();
        // 立即触发事件，确保 UIManager 能获取初始医疗包数量
        OnMedkitChanged?.Invoke(_medkitCount);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f) return;

        _currentHealth = Mathf.Max(0f, _currentHealth - amount);
        OnDamaged?.Invoke(amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);

        // 受击时播放 hit reaction 动画（UpperBody 层 HitReaction 状态）
        _animator?.SetTrigger("Hit");

        if (_currentHealth <= 0f)
        {
            // 触发死亡动画（Base Layer 的 Die 状态，AnyState 转移）
            _animator?.SetTrigger("Died");
            OnDeath?.Invoke();
        }
    }

    /// <summary>尝试使用医疗包治疗，返回是否成功</summary>
    public bool TryHeal()
    {
        if (_medkitCount <= 0) return false;
        if (_currentHealth >= maxHealth) return false;

        _currentHealth = Mathf.Min(maxHealth, _currentHealth + healAmount);
        _medkitCount--;
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
        OnMedkitChanged?.Invoke(_medkitCount);
        return true;
    }

    /// <summary>直接回血（不受医疗包限制，用于测试或特殊效果）</summary>
    public void Heal(float amount)
    {
        if (amount <= 0f) return;
        _currentHealth = Mathf.Min(maxHealth, _currentHealth + amount);
        OnHealthChanged?.Invoke(_currentHealth, maxHealth);
    }

    /// <summary>增加医疗包（拾取物调用）</summary>
    public void AddMedkit(int count)
    {
        _medkitCount = Mathf.Min(_medkitCount + count, maxMedkits);
        OnMedkitChanged?.Invoke(_medkitCount);
    }

    public float CurrentHealth => _currentHealth;

    public int MedkitCount => _medkitCount;

    public float GetHealthPercent() => _currentHealth / maxHealth;
}
