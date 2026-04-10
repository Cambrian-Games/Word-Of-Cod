using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[Serializable]
public class AttackRule
{
	public string _name;
	public float _weight = 1.0f;

	// not shown in inspector, only public to ensure it's written to yaml. Would be internal otherwise
	[HideInInspector]
	public int _index;

	public List<Condition> _conditions;

	[SerializeReference, SubclassSelector]
	public List<AttackEffect> _effects;

	private int _currentEffectIndex;
	private bool _stayInCurrentEffect;

	private int _roundCount = 0;

	public AttackEffect CurrentEffect => _currentEffectIndex == -1 ? null : _effects[_currentEffectIndex];



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

				// if we're outside of the condition's applicable frame, ignore it

				if (cond._firstCancelRound > _roundCount)
					continue;

				if (cond._lastCancelRound < _roundCount)
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

	public bool HasStarted() => _currentEffectIndex >= 0;

	public void StartRule(Enemy owner)
	{
		if (_effects.Count == 0)
		{
			Debug.LogError("No effects found!");
		}

		_currentEffectIndex = -1; // no current effect
		_stayInCurrentEffect = false;
		_roundCount = 0;

		_effects.ForEach(_effect => _effect.Reset(owner));
	}

	public void CancelRule(Enemy owner)
	{
		_currentEffectIndex = -1;
		_effects.ForEach(_effect => _effect.Reset(owner));
	}

	internal void StartRound(Enemy owner)
	{
		bool advanceToNextEffect = !(CurrentEffect != null && _stayInCurrentEffect);

		if (advanceToNextEffect)
		{
			// update next effect here because it's required for the forecast

			_currentEffectIndex++;
			CurrentEffect.StartEffect(owner);
		}

		_stayInCurrentEffect = false;
		_roundCount++;

		Debug.Assert(_currentEffectIndex < _effects.Count);
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

		bool effectTurnComplete = CurrentEffect.IsTurnComplete(owner);

		if (!effectTurnComplete)
		{
			effectTurnComplete = CurrentEffect.UpdateEffect(owner);
		}

		if (effectTurnComplete)
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

			_currentEffectIndex++;

			if (_currentEffectIndex >= _effects.Count)
				return true;

			CurrentEffect.StartEffect(owner);
			CurrentEffect.StartTurn(owner);

			return false;
		}

		return false;
	}

	public bool IsComplete(Enemy owner)
	{
		// we want to end the rule if the player is dead, otherwise check if the final effect is complete

		return Player.INSTANCE.CurrentHealth <= 0 || _effects.All(effect => effect.IsEffectComplete(owner));
	}

	internal bool InCriticalEffect()
	{
		return CurrentEffect?.IsCritical ?? false;
	}

	internal string FormattedForecast()
    {
		if (CurrentEffect != null)
			return CurrentEffect.FormattedForecast()
				.Replace("$RULE_NAME", _name);

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

		[InspectorName("Combo Broken"), NoValue]
		Combo_Break = 200,

		// Parameter Required

		[InspectorName("Variant Tiles On Board"), NeedsParameter]
		Variant_Tiles_On_Board = 300,

		// Misc lookup

		[InspectorName("Blackboard Value"), NeedsParameter]
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
		[InspectorName("Never")]
		IGNORE = 0,
		[InspectorName("Start of Enemy Turn")]
		START_OF_ENEMY_TURN = 1,
		[InspectorName("Start of Round")]
		START_OF_ROUND = 2,
		[InspectorName("During Player Turn")]
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
	public int _firstCancelRound;
	public int _lastCancelRound;



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

	#region Enum Attributes
	[AttributeUsage(AttributeTargets.Field)]
	public class NeedsParameterAttribute : PropertyAttribute
	{
		public bool NeedsParameter { get; }

		public NeedsParameterAttribute(bool needsParameter = true)
		{
			NeedsParameter = needsParameter;
		}
	}

	[AttributeUsage(AttributeTargets.Field)]
	public class NoValueAttribute : PropertyAttribute
	{
		public NoValueAttribute()
		{

		}
	}
	#endregion

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Condition))]
	public class ConditionPropertyDrawer : PropertyDrawer
	{
		protected static readonly float Y_OFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = EditorGUIUtility.singleLineHeight;

			EditorGUI.PropertyField(position, property.FindPropertyRelative("_category"));

			if (CategoryNeedsParameter(property))
			{
				position.y += Y_OFFSET;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_parameter"));
			}

			if (CategoryNeedsValue(property))
			{
				position.y += Y_OFFSET;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_comparator"));

				position.y += Y_OFFSET;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_value"));
			}

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Cancellation Rules", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_cancelKind"), new GUIContent("Cancel if False"));

			if (CancelKindNeedsRoundNumbers(property))
			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_firstCancelRound"), new GUIContent("Between Rounds"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_lastCancelRound"), new GUIContent("  and"));

				position.width = tmpWidth;
				position.x = tmpX;
			}
		}

		private bool CategoryNeedsParameter(SerializedProperty property)
		{
			Type enumType = typeof(Condition.Category);
			string name = Enum.GetName(enumType, property.FindPropertyRelative("_category").enumValueFlag);
			NeedsParameterAttribute attr = enumType.GetField(name).GetCustomAttributes(false).OfType<NeedsParameterAttribute>().SingleOrDefault();

			return attr != null && attr.NeedsParameter;
		}

		private bool CategoryNeedsValue(SerializedProperty property)
		{
			Type enumType = typeof(Condition.Category);
			string name = Enum.GetName(enumType, property.FindPropertyRelative("_category").enumValueFlag);
			NoValueAttribute attr = enumType.GetField(name).GetCustomAttributes(false).OfType<NoValueAttribute>().SingleOrDefault();

			return attr == null;
		}

		private bool CancelKindNeedsRoundNumbers(SerializedProperty property)
		{
			return property.FindPropertyRelative("_cancelKind").enumValueFlag != 0;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) +
				((CategoryNeedsParameter(property) ? 1 : 0) +
				(CategoryNeedsValue(property) ? 2 : 0) +
				2 + 
				(CancelKindNeedsRoundNumbers(property) ? 1 : 0)
				)
				 * Y_OFFSET;
		}
	}

#endif
}