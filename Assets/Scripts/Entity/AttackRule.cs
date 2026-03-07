using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AttackPriority
{
	Loop,
	[InspectorName("Select First Available")]
	First_Available,
	[InspectorName("Select Random From All Available")]
	Random_From_All_Available
}

[Serializable]
public class AttackRule
{
	public List<AttackCondition> _conditions;
	public bool _matchAllToRun; // true = all

	public List<AttackCondition> _cancelConditions;
	public bool _matchAllToCancel;

	public bool _uninterruptible;

	public float _weight = 1.0f;

	public List<AttackEffect> _effects;

	EffectData _effectData;

	int _currentEffectindex;
	public AttackEffect CurrentEffect => _currentEffectindex == -1 ? null : _effects[_currentEffectindex];

	internal int _roundsSinceLastUsed;


	public bool CanRun(Enemy owner)
	{
		if (_effects.Count == 0)
			return false;

		if (_conditions.Count == 0)
			return true;

		if (ShouldCancel(owner))
			return false;

		return _matchAllToRun ? _conditions.All(cond => cond.IsConditionSatisfied(owner)) : _conditions.Any(cond => cond.IsConditionSatisfied(owner));
	}

	public bool ShouldCancel(Enemy owner)
	{
		if (_uninterruptible)
			return false;

		if (_cancelConditions.Count == 0)
			return false;

		return _matchAllToCancel ? _cancelConditions.All(cond => cond.IsConditionSatisfied(owner)) : _conditions.Any(cond => cond.IsConditionSatisfied(owner));
	}

	public void StartRule()
	{
		if (_effects.Count == 0)
		{
			Debug.LogError("No effects found!");
			return;
		}

		_currentEffectindex = -1; // no current effect
	}

	internal void StartRound()
	{
		// update next effect here because it's required for the forecast

		_currentEffectindex++;
		Debug.Assert(_currentEffectindex < _effects.Count);
		_effectData = CurrentEffect.GenerateData();
	}

	internal void StartTurn()
	{
		// TBD, may not do anything aside from animations.
	}

	public bool UpdateTurn()
	{
		if (_effectData == null)
		{
			Debug.LogError("No effect data, can't update rule.");
			return true;
		}

		// current effect is incomplete

		if (_effectData._effectEndTime <= 0.0f)
		{
			bool isEffectComplete = CurrentEffect.UpdateEffect(_effectData);

			if (isEffectComplete)
			{
				// if the final effect is complete, exit

				if (_currentEffectindex + 1 >= _effects.Count)
					return true;
				
				// if the turn is over, exit

				if (CurrentEffect.EndsTurn)
					return true;

				_effectData._effectEndTime = Time.time;
			}
		}

		// if current effect is complete but turn is not over, move to next effect

		if (_effectData._effectEndTime > 0 &&
			(_effectData._effectEndTime + CurrentEffect.AfterEffectDelay) <= Time.time)
		{
			_currentEffectindex++;
			_effectData = CurrentEffect.GenerateData();
		}

		return false;
	}

	internal void EndTurn()
	{
		_effectData = null;
	}

	public void EndRule()
	{
		// TBD, might be animations
	}

	public bool Complete()
	{
		// we want to end the rule if the player is dead

		return Player.INSTANCE.CurrentHealth <= 0 || _currentEffectindex >= _effects.Count - 1;
	}

	internal bool PastInterruptCheckpoint()
	{
		int checkpointIndex = _effects.FindIndex(effect => effect.IsInterruptCheckpoint);
		if (checkpointIndex == -1)
			return false;

		// if we've already completed the checkpoint effect, this rule is safe to mark as complete if interrupted

		return _currentEffectindex > checkpointIndex;
	}

	internal void Cancel()
	{
		// mostly TBD

		_currentEffectindex = -1;
		_effectData = null;
	}

    internal string GetForecast()
    {
        if (CurrentEffect != null)
            return CurrentEffect.ForecastDescription;

        return "";
    }
}

[Serializable]
public class AttackCondition
{
	public enum ConditionField
	{
		[InspectorName("Enemy Health")]
		Enemy_Health,
		[InspectorName("Enemy Health (Percentage)")]
		Enemy_Health_Percent,
		[InspectorName("Player Health")]
		Player_Health,
		[InspectorName("Player Health (Percentage)")]
		Player_Health_Percent,
		[InspectorName("First Turn")]
		First_Turn,
		[InspectorName("Not First Turn")]
		Not_First_Turn,
		[InspectorName("Turns Since Last Action")]
		Turns_Since_Last_Action,
		[InspectorName(null), Obsolete]
		Last_Action_Index,
		[InspectorName("Length of Last Word")]
		Last_Word_Length,
		[InspectorName("Combo Length")]
		Combo_Length,
		[InspectorName("Combo Broken")]
		Combo_Break,
		[InspectorName(null), Obsolete]
		Enemy_Killed,
		//damage taken / percentage, can only be a cancel condition
	}

	public enum Comparator
	{
		[InspectorName("Equals")]
		Equal,
		[InspectorName("Does Not Equal")]
		Not_Equal,
		[InspectorName("Is Less Than")]
		Less_Than,
		[InspectorName("Is Greater Than")]
		Greater_Than,
		[InspectorName("Is Less Than or Equal To")]
		Less_Than_Or_Equal,
		[InspectorName("Is Greater Than or Equal To")]
		Greater_Than_Or_Equal,
	}

	[SerializeField]
	private ConditionField _field;
	[SerializeField]
	private Comparator _is;
	[SerializeField]
	private float _value;

	public bool IsConditionSatisfied(Enemy owner)
	{
		switch (_field)
		{
			case ConditionField.Combo_Break:
				throw new NotImplementedException();
			case ConditionField.Enemy_Killed:
				throw new NotImplementedException();
			case ConditionField.First_Turn:
				throw new NotImplementedException();
			case ConditionField.Not_First_Turn:
				throw new NotImplementedException();
		}

		int input = _field switch
		{
			ConditionField.Enemy_Health => owner.CurrentHealth,
			ConditionField.Enemy_Health_Percent => owner.HealthPercent(),
			ConditionField.Player_Health => Player.INSTANCE.CurrentHealth,
			ConditionField.Player_Health_Percent => Player.INSTANCE.HealthPercent(),
			ConditionField.Turns_Since_Last_Action => owner._roundsSinceLastAction,
			ConditionField.Last_Action_Index => owner.LastRuleIndex,
			ConditionField.Last_Word_Length => BattleManager.INSTANCE.MostRecentWord.Text.Length,
			ConditionField.Combo_Length => throw new NotImplementedException(),
			_ => throw new NotImplementedException()
		};

		return _is switch
		{
			Comparator.Equal => input == _value,
			Comparator.Not_Equal => input != _value,
			Comparator.Less_Than => input < _value,
			Comparator.Greater_Than => input > _value,
			Comparator.Less_Than_Or_Equal => input <= _value,
			Comparator.Greater_Than_Or_Equal => input >= _value,
			_ => throw new InvalidOperationException()
		};
	}
}

[Serializable]
public class AttackEffect
{
	public enum EffectKind
	{
		[InspectorName("Do Nothing")]
		Do_Nothing,
		[InspectorName("Standard Attack")]
		Standard_Attack,
		[InspectorName("Transform Tiles")]
		Transform_Tiles,
		[InspectorName("Schooling Attack")]
		Schooling_Attack,
	}

	[SerializeField]
	private float _afterEffectDelay;
	public float AfterEffectDelay => _afterEffectDelay;

	[SerializeField]
	private bool _endsTurn;
	public bool EndsTurn => _endsTurn;

    [SerializeField]
    private string _forecastDescription;
    public string ForecastDescription => _forecastDescription;

    [SerializeField, Tooltip("If past this effect, treat rule as complete if interrupted")]
	private bool _isInterruptCheckpoint;
	public bool IsInterruptCheckpoint => _isInterruptCheckpoint;


	[SerializeField]
	private EffectKind _effectKind;
	public EffectKind Effect => _effectKind;

	[Min(0), SerializeField]
	public int _damage = 0;

	[Min(1), SerializeField]
	public int _minSchoolAttackHits = 1;

	[Min(1), SerializeField]
	public int _maxSchoolAttackHits = 1;

	[SerializeField]
	private Tile.TileKind _from;
	[SerializeField]
	private Tile.TileKind _to;
	[SerializeField]
	private int _numTiles;

	public EffectData GenerateData()
	{
		return _effectKind switch
		{
			EffectKind.Do_Nothing => new WaitTurnData(),
			EffectKind.Standard_Attack => new StandardAttackData(),
			EffectKind.Transform_Tiles => new TransformTilesData(),
			EffectKind.Schooling_Attack => new SchoolingAttackData(_minSchoolAttackHits, _maxSchoolAttackHits),
			_ => null,
		};
	}

	internal void StartEffect(EffectData data)
	{
		switch (_effectKind)
		{
			case EffectKind.Do_Nothing:
				((WaitTurnData)data)._turnsWaited++;
				break;
		}
	}

	/// <summary>
	/// Ticks once per frame via EnemyTurnHandler. Returns true if there is no more work to be done by this rule and false <br/>
	/// if more work is required (i.e. animations). Not intended to be called again once it has returned true. 
	/// </summary>
	/// <param name="data">State data required for some rules</param>
	/// <returns></returns>
	internal bool UpdateEffect(EffectData data)
	{
		switch (_effectKind)
		{
			case EffectKind.Standard_Attack:
                
                Player.INSTANCE._inventory.OnEnemyAttack(_damage, out float modifiedStandardDamage);
                GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup((int) modifiedStandardDamage);
                Player.INSTANCE.CurrentHealth -= (int) modifiedStandardDamage;

				((StandardAttackData)data)._hasAttacked = true;
				break;

			case EffectKind.Transform_Tiles:
				GameBoard.INSTANCE.TransformRandomTiles(oldKind: _from, newKind: _to, num: _numTiles);
				((TransformTilesData)data)._hasTransformed = true;
				break;

			case EffectKind.Schooling_Attack:
				SchoolingAttackData schoolData = (SchoolingAttackData)data;

				// animations would play here

				if (schoolData._numHits < schoolData._targetHits)
				{
					schoolData._numHits++;
				}

				if (schoolData._numHits < schoolData._targetHits)
				{
					break;
				}

				Player.INSTANCE._inventory.OnEnemyAttack(_damage * schoolData._targetHits, out float modifiedSchoolDamage);
				GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup((int) modifiedSchoolDamage);
				Player.INSTANCE.CurrentHealth -= (int) modifiedSchoolDamage;
				schoolData._hasDamaged = true;
				break;
		}

		return IsComplete(data);
	}

	internal bool IsComplete(EffectData data)
	{
		return _effectKind switch
		{
			EffectKind.Do_Nothing => true,
			EffectKind.Standard_Attack => ((StandardAttackData)data)._hasAttacked,
			EffectKind.Transform_Tiles => ((TransformTilesData)data)._hasTransformed,
			EffectKind.Schooling_Attack => ((SchoolingAttackData)data)._hasDamaged,
			_ => throw new NotImplementedException($"IsComplete() does not handle {_effectKind}"),
		};
	}
}

/// <summary>
/// Any extra metadata we need to complete an AttackRule
/// </summary>
public class EffectData
{
	public readonly AttackEffect.EffectKind _effectKind;

	public float _effectEndTime = -1.0f;

	public EffectData(AttackEffect.EffectKind effectKind)
	{
		_effectKind = effectKind;
	}
}

public class WaitTurnData : EffectData
{
	// TODO support multiple turns of waiting to avoid having to create multiple effects for a multi-turn wait
	public int _turnsWaited = 0;

	public WaitTurnData() : base(AttackEffect.EffectKind.Do_Nothing)
	{
	}

	public override string ToString()
	{
		return "Turns Waited: " + _turnsWaited;
	}
}

public class StandardAttackData : EffectData
{
	public bool _hasAttacked = false;

	public StandardAttackData() : base(AttackEffect.EffectKind.Standard_Attack)
	{
	}

	public override string ToString()
	{
		return "Has Attacked: " + _hasAttacked;
	}
}

public class TransformTilesData : EffectData
{
	public bool _hasTransformed = false;

	public TransformTilesData() : base(AttackEffect.EffectKind.Transform_Tiles)
	{
	}

	public override string ToString()
	{
		return "Has Transformed: " + _hasTransformed;
	}
}

public class SchoolingAttackData : EffectData
{
	public int _numHits = 0;
	public int _targetHits = 0;

	public bool _hasDamaged = false;

	public SchoolingAttackData(int minHits, int maxHits) : base(AttackEffect.EffectKind.Schooling_Attack)
	{
		_targetHits = minHits + (int)((BattleManager.INSTANCE.CurrentEnemy.HealthPercent() / 100f) * (maxHits - minHits + 1));

		// if minhits is 1 and maxHits is 20, we have [0, 0.05) = 1 hit, [0.05, 0.1) = 2 hits, etc
		//  but at 1 exactly, it would equal 21 hits, so we clamp it.

		if (_targetHits > maxHits)
		{
			_targetHits = maxHits;
		}
	}
}
