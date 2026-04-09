using System;
using UnityEngine;

[Serializable]
public abstract class AttackEffect
{
#if UNITY_EDITOR
	// variable must be named exactly like this to display correctly in a list
	public string name;
#endif

	[SerializeField]
	private string _forecastDescription;
	public string ForecastDescription => _forecastDescription;

	[SerializeField, Min(1)]
	protected int _numTurns;
	public int NumTurns => _numTurns;

	protected int _currentTurn = 0;

	[SerializeField]
	private bool _endsTurn;
	public bool EndsTurn => _endsTurn;

	[SerializeField]
	private bool _isCritical;
	public bool IsCritical => _isCritical;



	internal virtual void StartEffect(Enemy owner)
	{
		_currentTurn = 0;
	}

	internal virtual void StartTurn(Enemy owner)
	{
		_currentTurn++;
	}

	/// <summary>
	/// Ticks once per frame via EnemyTurnHandler. Returns true if there is no more work to be done this turn by this rule and false <br/>
	/// if more work is required (i.e. animations). Not intended to be called again once it has returned true. 
	/// </summary>
	internal virtual bool UpdateEffect(Enemy owner) { return true; }
	internal virtual bool IsTurnComplete(Enemy owner) { return true; }
	internal virtual bool IsEffectComplete(Enemy owner) { return IsTurnComplete(owner); }
}

[Serializable]
public class StandardAttack : AttackEffect
{
	public int _baseDamage;

	private bool _hasAttacked = false;

	internal override void StartEffect(Enemy owner)
	{
		base.StartEffect(owner);

		_hasAttacked = false;
	}

	internal override void StartTurn(Enemy owner)
	{
		base.StartTurn(owner);

		_hasAttacked = false;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		base.UpdateEffect(owner);

		Player.INSTANCE._inventory.OnEnemyAttack(_baseDamage, out float modifiedDamage);
		GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup((int)modifiedDamage);
		Player.INSTANCE.Damage((int)modifiedDamage);

		_hasAttacked = true;

		return true;
	}

	internal override bool IsTurnComplete(Enemy owner)
	{
		return _hasAttacked;
	}

	internal override bool IsEffectComplete(Enemy owner)
	{
		return _hasAttacked && _currentTurn >= _numTurns;
	}
}
