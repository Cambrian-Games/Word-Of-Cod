using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class AttackRule
{
#if UNITY_EDITOR
	// variable must be named exactly like this to display correctly in a list
	public string name;
#endif
	public float _weight = 1.0f;
	internal int _index;

	public List<Condition> _conditions;

	[SerializeReference, SubclassSelector]
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

	public void StartRule(Enemy owner)
	{
		if (_effects.Count == 0)
		{
			Debug.LogError("No effects found!");
		}

		_currentEffectindex = -1; // no current effect
		_stayInCurrentEffect = false;
	}

	public void CancelRule(Enemy owner)
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
		CurrentEffect.StartTurn(owner);
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

			if (CurrentEffect.EndsTurn)
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

	internal bool InCriticalEffect()
	{
		return CurrentEffect.IsCritical;
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