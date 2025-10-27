using System;
using UnityEngine;

public class Entity : MonoBehaviour
{
    // config
	public string _displayName;
	public Sprite _sprite;
	public int _maxHealth;

    // state
    internal int _currentHealth;
    private bool _hasInit;

    public void Init()
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