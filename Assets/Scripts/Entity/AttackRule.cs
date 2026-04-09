using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class AttackRule
{
#if UNITY_EDITOR
	public string _name;
#endif
	public float _weight = 1.0f;
	internal int _index;

	public List<Condition> _conditions;
	public List<AttackEffect> _effects;

	private int _currentEffectindex;
	private bool _stayInCurrentEffect;

	public AttackEffect CurrentEffect => _currentEffectindex == -1 ? null : _effects[_currentEffectindex];


	public bool CanRun(Enemy owner)
	{
		if (_effects.Count == 0)
			return false;

		if (_conditions.Count == 0)
			return true;

		foreach (Condition cond in _conditions)
		{
			if (!cond.Passes(owner))
			{
				if (cond._cancelKind == Condition.FCANCEL.IGNORE)
					continue;

				// conditions may have different situations where they can cancel an attack.
				//  if a condition fails, we check the battle state and see whether the
				//  condition would cancel an attack at that point

				BattleManager.BattleState battleState = BattleManager.INSTANCE.CurrentState;

				switch (battleState)
				{
					case BattleManager.BattleState.Pre_Player_Turn:
						if ((cond._cancelKind & Condition.FCANCEL.START_OF_ROUND) != 0)
						{
							return false;
						}
						continue;

					case BattleManager.BattleState.Player_Turn:
						if ((cond._cancelKind & Condition.FCANCEL.DURING_PLAYER_TURN) != 0)
						{
							return false;
						}
						continue;

					case BattleManager.BattleState.Enemy_Turn:
						if ((cond._cancelKind & Condition.FCANCEL.START_OF_ENEMY_TURN) != 0)
						{
							return false;
						}
						continue;
				}
			}
		}

		return true;
	}

	public bool HasStarted() => _currentEffectindex >= 0;

	public void StartRule()
	{
		if (_effects.Count == 0)
		{
			Debug.LogError("No effects found!");
		}

		_currentEffectindex = -1; // no current effect
		_stayInCurrentEffect = false;
	}

	public void Cancel(Enemy owner)
	{
		_currentEffectindex = -1;
	}

	internal void StartRound(Enemy owner)
	{
		bool advanceToNextEffect = !(CurrentEffect != null && _stayInCurrentEffect);

		if (advanceToNextEffect)
		{
			// update next effect here because it's required for the forecast

			_currentEffectindex++;
			CurrentEffect.StartEffect(owner);
		}

		_stayInCurrentEffect = false;

		Debug.Assert(_currentEffectindex < _effects.Count);
	}

	internal void StartTurn(Enemy owner)
	{
		// TBD, may not do anything aside from animations.
	}

	public bool UpdateTurn(Enemy owner)
	{
		if (CurrentEffect == null)
		{
			Debug.LogError("No effect exists.");
			return true;
		}

		// current effect is incomplete. This first check should be true in most cases but is a good safeguard.

		if (!CurrentEffect.IsTurnComplete(owner))
		{
			CurrentEffect.UpdateEffect(owner);
		}

		if (CurrentEffect.IsTurnComplete(owner))
		{
			// If this is a multiturn effect and it has turns remaining, end turn and prevent advancing to next effect

			if (!CurrentEffect.IsEffectComplete(owner))
			{
				_stayInCurrentEffect = true;
				return true;
			}
				
			// This explicitly ends the turn

			if (CurrentEffect.EndsTurn(owner))
				return true;

			// try to go to next effect. If none exists, return true, otherwise start next effect.

			_currentEffectindex++;

			if (_currentEffectindex >= _effects.Count)
				return true;

			CurrentEffect.StartEffect(owner);

			return false;
		}

		return false;
	}

	public bool IsComplete(Enemy owner)
	{
		// we want to end the rule if the player is dead, otherwise check if the final effect is complete

		return Player.INSTANCE.CurrentHealth <= 0 || _effects[^1].IsEffectComplete(owner);
	}

    internal string Forecast()
    {
        if (CurrentEffect != null)
            return CurrentEffect.ForecastDescription;

        return "";
    }
}

[Serializable]
public class Condition
{
	public enum Category
	{
		// No Parameter

		[InspectorName("Enemy Health (Percentage)")]
		Enemy_Health_Percent,
		[InspectorName("Player Health (Percentage)")]
		Player_Health_Percent,
		[InspectorName("Round Number")]
		Round_Number,

		// Usually Interrupt/Cancel Conditions

		[InspectorName("Damage Taken")]
		Damage_Taken = 100,
		[InspectorName("Length of Last Word Submitted")]
		Last_Word_Length,
		[InspectorName("Combo Length")]
		Combo_Length,

		// No Value Needed

		[InspectorName("Combo Broken")]
		Combo_Break = 200,

		// Parameter Required

		[InspectorName("Variant Tiles On Board")]
		Variant_Tiles_On_Board = 300,

		// Misc lookup

		[InspectorName("Blackboard Value")]
		Blackboard_Value = 400
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

	[Flags]
	public enum FCANCEL
	{
		IGNORE = 0,
		START_OF_ENEMY_TURN = 1,
		START_OF_ROUND = 2,
		DURING_PLAYER_TURN = 4,
	}

	[SerializeField]
	private Category _category;
	[SerializeField]
	private string _parameter;
	[SerializeField]
	private Comparator _comparator;
	[SerializeField]
	private float _value;

	public FCANCEL _cancelKind;

	public bool Passes(Enemy owner)
	{
		switch (_category)
		{
			case Category.Combo_Break:
				throw new NotImplementedException();
		}

		float input = _category switch
		{
			Category.Enemy_Health_Percent => owner.HealthPercent(),
			Category.Player_Health_Percent => Player.INSTANCE.HealthPercent(),
			Category.Round_Number => throw new NotImplementedException(),
			Category.Last_Word_Length => BattleManager.INSTANCE.MostRecentWord?.Text.Length ?? 0,
			Category.Combo_Length => throw new NotImplementedException(),
			Category.Damage_Taken => owner.LastDamageTaken,
			Category.Variant_Tiles_On_Board => GameBoard.INSTANCE.CountTiles((Tile.TileKind) Enum.Parse(typeof(Tile.TileKind), _parameter)),
			Category.Blackboard_Value => (int) owner._blackboard.GetValueOrDefault(_parameter, 0),
			_ => throw new InvalidOperationException()
		};

		bool categoryIsPercent = _category == Category.Enemy_Health_Percent || _category == Category.Player_Health_Percent;
		bool comparatorIsEqual = _comparator == Comparator.Equal;

		Debug.Assert(categoryIsPercent != comparatorIsEqual, "Trying to compare equality for two decimal numbers is not recommended.");

		return _comparator switch
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

		[InspectorName("Count Variant Tiles")]
		Count_Variant_Tiles,
		[InspectorName("Attack Per Variant Tile")]
		Variant_Tile_Attack,
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

			EffectKind.Count_Variant_Tiles => new EffectData(EffectKind.Count_Variant_Tiles),
			EffectKind.Variant_Tile_Attack => new VariantTileAttackData(),
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
                Player.INSTANCE.Damage((int) modifiedStandardDamage);

				((StandardAttackData)data)._hasAttacked = true;
				break;

			case EffectKind.Transform_Tiles:
				if (_numTiles > 0)
				{
					GameBoard.INSTANCE.TransformRandomTiles(oldKind: _from, newKind: _to, num: _numTiles);
				}
				else
				{
					GameBoard.INSTANCE.TransformAllTiles(oldKind: _from, newKind: _to);
				}
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
				Player.INSTANCE.Damage((int) modifiedSchoolDamage);
				schoolData._hasDamaged = true;
				break;

			case EffectKind.Count_Variant_Tiles:
				BattleManager.INSTANCE.CurrentEnemy._blackboard[_to.ToString()] = GameBoard.INSTANCE.CountTiles(_to);
				break;

			case EffectKind.Variant_Tile_Attack:

				VariantTileAttackData variantData = (VariantTileAttackData)data;

				Player.INSTANCE._inventory.OnEnemyAttack(_damage * BattleManager.INSTANCE.CurrentEnemy._blackboard[_to.ToString()], out float modifiedVariantDamage);
				GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup((int)modifiedVariantDamage);
				Player.INSTANCE.Damage((int) modifiedVariantDamage);

				BattleManager.INSTANCE.CurrentEnemy._blackboard.Remove(_to.ToString());
				variantData._hasAttacked = true;
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
			EffectKind.Count_Variant_Tiles => true,
			EffectKind.Variant_Tile_Attack => ((VariantTileAttackData)data)._hasAttacked,
			_ => throw new NotImplementedException($"IsComplete() does not handle {_effectKind}"),
		};
	}
}

/// <summary>
/// Any extra metadata we need to complete an AttackEffect
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

public class VariantTileAttackData : EffectData
{
	public bool _hasAttacked = false;

	public VariantTileAttackData() : base(AttackEffect.EffectKind.Variant_Tile_Attack)
	{

	}
}

