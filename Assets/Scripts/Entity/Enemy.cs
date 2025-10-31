using System;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
    // config

	[SerializeField]
	private List<AttackRule> _rules;
	public List<AttackRule> Rules => _rules;

    // attack state

	internal int _currentRule = -1;
	private RuleData _currentRuleData = null;

	private bool _isTurnComplete = false;
	public bool IsTurnComplete => _isTurnComplete;

	public override void UpdateTurn()
	{
		base.UpdateTurn();

		if (!_isTurnComplete)
			_isTurnComplete = _rules[_currentRule].UpdateTurn(_currentRuleData);
	}

	public override void StartTurn()
	{
		if (_currentRuleData == null)
		{
			_currentRule = 0;
			_currentRuleData = _rules[_currentRule].GenerateData();
		}

		_isTurnComplete = false;
		_rules[_currentRule].StartTurn(_currentRuleData);

		Debug.Log(_currentRuleData.ToString());
	}

	public override void EndTurn()
	{
		base.EndTurn();

		if (_currentRule >= _rules.Count)
		{
			_currentRule = 0;
			_currentRuleData = _rules[_currentRule].GenerateData();
		}

		if (_rules[_currentRule].IsComplete(_currentRuleData))
		{
			_currentRule = (_currentRule + 1) % _rules.Count;
			_currentRuleData = _rules[_currentRule].GenerateData();
		}
	}
}

[Serializable]
public class AttackRule
{
	public enum RuleKind
	{
		[InspectorName("Wait Turns")]
		Wait_Turns,
		[InspectorName("Standard Attack")]
		Standard_Attack,
		[InspectorName("Pin Prick")]
		Create_Spiny_And_Damage // will try to support sub-rules to make this more modular.
	}

	[SerializeField]
	private RuleKind _ruleKind;
	public RuleKind Rule => _ruleKind;

	// This will eventually support ranges.
	[Min(0), SerializeField]
	private int _turnsToWait = 0;
	public int TurnsToWait => (_ruleKind == RuleKind.Wait_Turns) ? _turnsToWait : throw new InvalidOperationException();

	[Min(0), SerializeField]
	public int _damage = 0;
	public int Damage => (_ruleKind == RuleKind.Standard_Attack) ? _damage : throw new InvalidOperationException();

	public RuleData GenerateData()
	{
		return _ruleKind switch
		{
			RuleKind.Wait_Turns => new WaitTurnData(),
			RuleKind.Standard_Attack => new StandardAttackData(),
			RuleKind.Create_Spiny_And_Damage => new StandardAttackData(),
			_ => null,
		};
	}

	internal void StartTurn(RuleData data)
	{
		switch (_ruleKind)
		{
			case RuleKind.Wait_Turns:
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
	internal bool UpdateTurn(RuleData data)
	{
		switch (_ruleKind)
		{
			case RuleKind.Wait_Turns:
				return true;

			case RuleKind.Standard_Attack:
				BattleManager.INSTANCE.DamagePlayer(Damage);
				((StandardAttackData)data)._hasAttacked = true;
				return true;

			case RuleKind.Create_Spiny_And_Damage:
				BattleManager.INSTANCE.DamagePlayer(Damage);
				GameBoard.INSTANCE.TransformTiles(oldKind: Tile.TileKind.Normal, newKind: Tile.TileKind.Spiny, num: 2);
				((StandardAttackData)data)._hasAttacked = true;
				return true;

			default:
				return true;
		}
	}

	internal bool IsComplete(RuleData data)
	{
		return _ruleKind switch
		{
			RuleKind.Wait_Turns => ((WaitTurnData)data)._turnsWaited >= TurnsToWait,
			RuleKind.Standard_Attack => ((StandardAttackData)data)._hasAttacked,
			RuleKind.Create_Spiny_And_Damage => ((StandardAttackData)data)._hasAttacked,
			_ => true,
		};
	}
}

/// <summary>
/// Any extra metadata we need to complete an AttackRule
/// </summary>
public abstract class RuleData
{
	public readonly AttackRule.RuleKind _ruleKind;

	public RuleData(AttackRule.RuleKind ruleKind)
	{
		_ruleKind = ruleKind;
	}
}

public class WaitTurnData : RuleData
{
	public int _turnsWaited = 0;

	public WaitTurnData() : base(AttackRule.RuleKind.Wait_Turns)
	{
	}

	public override string ToString()
	{
		return "Turns Waited: " + _turnsWaited;
	}
}

public class StandardAttackData : RuleData
{
	public bool _hasAttacked = false;

	public StandardAttackData() : base(AttackRule.RuleKind.Standard_Attack)
	{
	}

	public override string ToString()
	{
		return "Has Attacked: " + _hasAttacked;
	}
}
