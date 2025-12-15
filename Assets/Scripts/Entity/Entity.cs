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

    protected virtual void Awake()
    {
		_currentHealth = _maxHealth;
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