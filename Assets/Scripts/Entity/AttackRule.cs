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
	public bool _matchAll; // true = all

	public List<AttackEffect> _effects;

	EffectData _effectData;
	int _currentEffect;

	public bool CanRun(Enemy owner)
	{
		if (_effects.Count == 0)
			return false;

		if (_conditions.Count == 0)
			return true;

		return _matchAll ? _conditions.All(cond => cond.IsConditionSatisfied(owner)) : _conditions.Any(cond => cond.IsConditionSatisfied(owner));
	}

	public void StartRule()
	{
		_currentEffect = 0;
		_effectData = _effects[_currentEffect].GenerateData();
	}

	public bool UpdateRule()
	{
		if (_effectData._effectEndTime <= 0.0f)
		{
			if (_effects[_currentEffect].UpdateEffect(_effectData))
			{
				// the final effect is complete
				if (_currentEffect + 1 >= _effects.Count)
					return true;

				_effectData._effectEndTime = Time.time;
			}
		}

		if (_effectData._effectEndTime > 0 &&
			(_effectData._effectEndTime + _effects[_currentEffect].AfterEffectDelay) <= Time.time)
		{
			_currentEffect++;
			_effectData = _effects[_currentEffect].GenerateData();
		}

		return false;
	}

	public void EndRule()
	{
		_effectData = null;
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
		[InspectorName("Turns Since Last Action")]
		Turns_Since_Last_Action,
		[InspectorName("Index of Last Action")]
		Last_Action_Index,
		[InspectorName("Length of Last Word")]
		Last_Word_Length,
		[InspectorName("Combo Length")]
		Combo_Length,
		[InspectorName("Combo Broken")]
		Combo_Break,
		[InspectorName("Enemy Killed Last Turn")]
		Enemy_Killed,
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
		}

		int input = _field switch
		{
			ConditionField.Enemy_Health => owner._currentHealth,
			ConditionField.Enemy_Health_Percent => owner._currentHealth * 100 / owner._maxHealth,
			ConditionField.Player_Health => BattleManager.INSTANCE.CurrentPlayerHealth(),
			ConditionField.Player_Health_Percent => BattleManager.INSTANCE.CurrentPlayerHealth() * 100 / BattleManager.INSTANCE.MaxPlayerHealth(),
			ConditionField.Turns_Since_Last_Action => owner._turnsSinceLastAction,
			ConditionField.Last_Action_Index => owner.LastRule,
			ConditionField.Last_Word_Length => BattleManager.INSTANCE.LastWord.Length,
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
	}

	[SerializeField]
	private float _afterEffectDelay;
	public float AfterEffectDelay => _afterEffectDelay;

	[SerializeField]
	private EffectKind _effectKind;
	public EffectKind Effect => _effectKind;

	[Min(0), SerializeField]
	public int _damage = 0;
	public int Damage => (_effectKind == EffectKind.Standard_Attack) ? _damage : throw new InvalidOperationException();

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
			EffectKind.Do_Nothing => new EffectData(EffectKind.Do_Nothing),
			EffectKind.Standard_Attack => new StandardAttackData(),
			EffectKind.Transform_Tiles => new TransformTilesData(),
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
				BattleManager.INSTANCE.DamagePlayer(_damage);
				((StandardAttackData)data)._hasAttacked = true;
				break;

			case EffectKind.Transform_Tiles:
				GameBoard.INSTANCE.TransformTiles(oldKind: _from, newKind: _to, num: _numTiles);
				((TransformTilesData)data)._hasTransformed = true;
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