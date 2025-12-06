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

	// do we want some sort of OnDamage() function?
	public int CurrentHealth { get => _currentHealth; set => _currentHealth = Mathf.Clamp(value, 0, _maxHealth); }
	public int HealthPercent() => CurrentHealth * 100 / _maxHealth;

    private bool _hasInit;

    public virtual void Init()
    {
        if (!_hasInit)
        {
            _currentHealth = _maxHealth;
            _hasInit = true;
        }
    }

	public virtual void UpdateTurn()
	{

	}

	public virtual void StartTurn()
	{
	}

	public virtual void EndTurn()
	{

	}
}