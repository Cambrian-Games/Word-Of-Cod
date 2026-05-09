using System;
using UnityEngine;

public class Entity : MonoBehaviour
{
    // config
	public string _displayName;
	public Sprite _sprite;

	[SerializeField]
	private int _maxHealth;
	public int MaxHealth => _maxHealth;

	// state
    private int _currentHealth;
	public int CurrentHealth
	{
		get => _currentHealth;
		//set => _currentHealth = value;
	}

	protected int _lastDamageTaken = 0;
	public int LastDamageTaken => _lastDamageTaken;

	public void SetHealth(int value)
	{
		_currentHealth = value;
	}


    protected virtual void Awake()
    {
		_currentHealth = _maxHealth;
    }

	public virtual void Damage(int damage)
	{
		Debug.Assert(damage >= 0);

		_lastDamageTaken = Mathf.Min(damage, _currentHealth);
		_currentHealth = Mathf.Max(0, _currentHealth - damage);

		if (_lastDamageTaken > 0)
		{
			OnTakeDamage(_lastDamageTaken);
		}
	}

	public virtual void Heal(int heal)
	{
		Debug.Assert(heal >= 0);

		_lastDamageTaken = 0;

		float amountHealed = Mathf.Min(heal, _maxHealth - _currentHealth);
		_currentHealth = Mathf.Min(_maxHealth, _currentHealth + heal);

		if (amountHealed > 0)
		{
			OnHeal(amountHealed);
		}
	}

	protected virtual void OnTakeDamage(int damageTaken) { }
	protected virtual void OnHeal(float amountHealed) { }

	public float HealthPercent() => _currentHealth / (float) _maxHealth;
	public int HealthPercentReadable() => _currentHealth * 100 / _maxHealth;
}