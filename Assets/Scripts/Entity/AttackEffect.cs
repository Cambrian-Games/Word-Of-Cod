using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public abstract class AttackEffect
{
#if UNITY_EDITOR
	public string _name;
#endif

	[SerializeField]
	protected string _forecast;

	[SerializeField, Min(1)]
	protected int _numTurns = 1;
	public int NumTurns => _numTurns;

	private int _currentTurn = 0;

	[SerializeField]
	private bool _endsTurn;
	public bool EndsTurn => _endsTurn;

	[SerializeField]
	private bool _isCritical;
	public bool IsCritical => _isCritical;

	// IsEffectComplete() is called a LOT, so we cache it once it becomes true.
	protected bool _isEffectComplete = false;



	internal virtual void StartEffect(Enemy owner)
	{
		_currentTurn = 0;
		_isEffectComplete = false;
	}

	internal virtual void StartTurn(Enemy owner)
	{
		_currentTurn++;
		_isEffectComplete = false;
	}

	internal virtual void Reset(Enemy owner)
	{
		_currentTurn = 0;
		_isEffectComplete = false;
	}

	/// <summary>
	/// Ticks once per frame via EnemyTurnHandler. Returns true if there is no more work to be done this turn by this rule and false <br/>
	/// if more work is required (i.e. animations). Not intended to be called again during a turn once it has returned true. 
	/// </summary>
	internal virtual bool UpdateEffect(Enemy owner) => IsTurnComplete(owner);
	internal virtual bool IsTurnComplete(Enemy owner) => true;
	internal virtual bool IsEffectComplete(Enemy owner)
	{
		if (_isEffectComplete)
			return true;

		_isEffectComplete = IsTurnComplete(owner) && OnLastTurn();
		return _isEffectComplete;
	}

	internal virtual bool OnLastTurn() => _currentTurn >= _numTurns;

	public virtual string FormattedForecast()
	{
		string nowReplaced = _forecast.Replace("$NOW", "This Round");

		bool inOneRound = _numTurns - _currentTurn == 1;

		// there is probably a bug with the forecast math when an interrupt happens.

		return nowReplaced.Replace("$ROUNDS", inOneRound ? "Next Round" : $"In {_numTurns - _currentTurn} Rounds");
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(AttackEffect))]
	public class AttackEffectPropertyDrawer : PropertyDrawer
	{
		protected static readonly float Y_OFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;



		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			position.height = EditorGUIUtility.singleLineHeight;
			EffectGUI(position, label, property);
		}

		protected virtual Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_name"));

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_forecast"));

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTurns"));

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_endsTurn"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_isCritical"));

				position.width = tmpWidth;
				position.x = tmpX;
			}

			return position;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return base.GetPropertyHeight(property, label) + EffectHeight();
		}

		protected virtual float EffectHeight()
		{
			return 3 * Y_OFFSET;
		}
	}
#endif
}

[Serializable]
public class StandardAttack : AttackEffect
{
	[SerializeField, Min(0)]
	protected int _baseDamage;

	protected bool _hasAttacked = false;



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

	internal override void Reset(Enemy owner)
	{
		base.Reset(owner);

		_hasAttacked = false;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		Player.INSTANCE._inventory.OnEnemyAttack(_baseDamage, out float modifiedDamage);
		GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup(Mathf.RoundToInt(modifiedDamage));
		Player.INSTANCE.Damage(Mathf.RoundToInt(modifiedDamage));

		_hasAttacked = true;

		return base.UpdateEffect(owner);
	}

	internal override bool IsTurnComplete(Enemy owner)
	{
		return _hasAttacked;
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(StandardAttack))]
	public class StandardAttackPropertyDrawer : AttackEffectPropertyDrawer
	{
		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			position = base.EffectGUI(position, label, property);

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Standard Attack Data", EditorStyles.boldLabel);
			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_baseDamage"));

			return position;
		}

		protected override float EffectHeight()
		{
			return base.EffectHeight() + 2 * Y_OFFSET;
		}
	}
#endif
}

[Serializable]
public class Wait : AttackEffect
{
	[SerializeField, Min(0.0f)]
	private float _timeToWait = 0.0f;

	private float _timeWaited = 0.0f;
	private bool _hasWaited = false;



	internal override void StartEffect(Enemy owner)
	{
		base.StartEffect(owner);

		_timeWaited = 0.0f;
		_hasWaited = false;
	}

	internal override void StartTurn(Enemy owner)
	{
		base.StartTurn(owner);

		_timeWaited = 0.0f;
		_hasWaited = false;
	}

	internal override void Reset(Enemy owner)
	{
		base.Reset(owner);

		_timeWaited = 0.0f;
		_hasWaited = false;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		_timeWaited += Time.deltaTime;

		if (_timeWaited >= _timeToWait)
			_hasWaited = true;

		return base.UpdateEffect(owner);
	}

	internal override bool IsTurnComplete(Enemy owner)
	{
		return _hasWaited;
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(Wait))]
	public class WaitPropertyDrawer : AttackEffectPropertyDrawer
	{
		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			position = base.EffectGUI(position, label, property);

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Wait Data", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_timeToWait"));

			return position;
		}

		protected override float EffectHeight()
		{
			return base.EffectHeight() + 2 * Y_OFFSET;
		}
	}
#endif
}

[Serializable]
public class TransformTiles : AttackEffect
{
	[SerializeField, Min(0)]
	[Tooltip("Leave as 0 to transform all matching tiles.")]
	private int _numTiles = 0;

	[SerializeField]
	private Tile.TileKind _from = Tile.TileKind.Normal;

	[SerializeField]
	private Tile.TileKind _to = Tile.TileKind.Sandy;

	private bool _hasTransformedTiles = false;



	internal override void StartEffect(Enemy owner)
	{
		base.StartEffect(owner);

		_hasTransformedTiles = false;
	}

	internal override void StartTurn(Enemy owner)
	{
		base.StartTurn(owner);

		_hasTransformedTiles = false;
	}

	internal override void Reset(Enemy owner)
	{
		base.Reset(owner);

		_hasTransformedTiles = false;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		GameBoard.INSTANCE.TransformRandomTiles(oldKind: _from, newKind: _to, num: _numTiles);

		_hasTransformedTiles = true;

		return base.UpdateEffect(owner);
	}

	internal override bool IsTurnComplete(Enemy owner)
	{
		return _hasTransformedTiles;
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(TransformTiles))]
	public class TransformTilesPropertyDrawer : AttackEffectPropertyDrawer
	{
		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			position = base.EffectGUI(position, label, property);

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Transform Tiles Data", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTiles"), new GUIContent("Change"));

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_from"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_to"));

				position.width = tmpWidth;
				position.x = tmpX;
			}
			
			return position;
		}

		protected override float EffectHeight()
		{
			return base.EffectHeight() + 3 * Y_OFFSET;
		}
	}
#endif
}

[Serializable]
public class SchoolingAttack : StandardAttack
{
	[SerializeField, Min(0)]
	private int _minHits = 1;

	[SerializeField, Min(1)]
	private int _maxHits = 1;

	[SerializeField, Min(0), 
		Tooltip("HP Percent is multiplied by this when determining damage per sub-hit. " +
				"For example, Round(6.25 * 100%) * 10 hits = 60 damage.")]
	private float _damageScale = 0.5f;

	[SerializeField]
	private int _targetHits = 0;

	private int _numHits = 0;



	internal override void StartEffect(Enemy owner)
	{
		base.StartEffect(owner);
		
		_numHits = 0;

		float a = _minHits;
		float b = _maxHits;
		float u = owner.HealthPercent();

		_targetHits = Mathf.RoundToInt(
						Mathf.Min(b, 
							Mathf.Lerp(a, b + 1, u))); // b + 1 is necessary, plug into desmos with a = 1, b = 20, and u in increments of 0.05
	}

	internal override void StartTurn(Enemy owner)
	{
		base.StartTurn(owner);

		_numHits = 0;

		float a = _minHits;
		float b = _maxHits;
		float u = owner.HealthPercent();

		_targetHits = Mathf.RoundToInt(
						Mathf.Min(b,
							Mathf.Lerp(a, b + 1, u)));
	}

	internal override void Reset(Enemy owner)
	{
		base.Reset(owner);

		_hasAttacked = false;
		_targetHits = 0;
		_numHits = 0;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		if (_numHits < _targetHits)
		{
			_numHits++;
		}

		if (_numHits < _targetHits)
			return false;

		// value specified in design doc was Current HP / 20 * 2.5. Max HP was 200, I showed
		// Matt a graph of what the damage output looked like for if current HP was instead 100 (as in 100%).
		// Damage output was way too high, so I proposed halving it again, so (100 * (currentHP / maxHP) / 2) / 20 * 2.5.
		// This creates a base multiplier of 100 / 40 * 2.5 or 6.25, which is the value currently stored in the editor.

		int damagePerHit = Mathf.RoundToInt(owner.HealthPercent() * _damageScale);
		int damage = Mathf.Max(_baseDamage, _numHits * damagePerHit);

		Player.INSTANCE._inventory.OnEnemyAttack(damage, out float modifiedDamage);
		GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup(Mathf.RoundToInt(modifiedDamage));
		Player.INSTANCE.Damage(Mathf.RoundToInt(modifiedDamage));

		_hasAttacked = true;

		return IsTurnComplete(owner);
	}



#if UNITY_EDITOR
	// this intentionally inherits from AttackEffectPropertyDrawer, not StandardAttackPropertyDrawer
	[CustomPropertyDrawer(typeof(SchoolingAttack))]
	public class SchoolingAttackPropertyDrawer : AttackEffectPropertyDrawer
	{
		private static bool SHOW_DAMAGE_SIM = false;
		private static float HEALTH_SIM = 1.0f;

		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			position = base.EffectGUI(position, label, property);

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Schooling Data", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Hover over Damage Scale to read tooltip");

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_damageScale"), new GUIContent("Damage Scale"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_baseDamage"), new GUIContent("Minimum Damage"));

				position.width = tmpWidth;
				position.x = tmpX;
			}

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				SerializedProperty minHitProperty = property.FindPropertyRelative("_minHits");
				EditorGUI.PropertyField(position, minHitProperty);

				position.x += position.width;
				SerializedProperty maxHitProperty = property.FindPropertyRelative("_maxHits");
				EditorGUI.PropertyField(position, maxHitProperty);
				maxHitProperty.intValue = Mathf.Max(minHitProperty.intValue, maxHitProperty.intValue);

				position.width = tmpWidth;
				position.x = tmpX;
			}

			position.y += Y_OFFSET;
			SHOW_DAMAGE_SIM = EditorGUI.Toggle(position, "Show Damage Sim", SHOW_DAMAGE_SIM);

			if (SHOW_DAMAGE_SIM)
			{
				EditorGUI.indentLevel++;

				position.y += Y_OFFSET;
				HEALTH_SIM = EditorGUI.Slider(position, "Health", HEALTH_SIM, 0.0f, 1.0f);

				{
					position.y += Y_OFFSET;

					float a = property.FindPropertyRelative("_minHits").intValue;
					float b = property.FindPropertyRelative("_maxHits").intValue;
					int numHits =
						Mathf.RoundToInt(
							Mathf.Min(b,
								Mathf.Lerp(a, b + 1, HEALTH_SIM)));

					int minDamage = property.FindPropertyRelative("_baseDamage").intValue;
					float damageScale = property.FindPropertyRelative("_damageScale").floatValue;

					GUI.enabled = false;
					EditorGUI.IntField(position, "Total Damage", Mathf.Max(minDamage, numHits * Mathf.RoundToInt(HEALTH_SIM * damageScale)));
					GUI.enabled = true;
				}
				

				EditorGUI.indentLevel--;
			}

			return position;
		}

		protected override float EffectHeight()
		{
			return base.EffectHeight() + (5 + (SHOW_DAMAGE_SIM ? 2 : 0)) * Y_OFFSET;
		}
	}
#endif
}


[Serializable]
public abstract class VariableTurnAttack : AttackEffect
{
#if UNITY_EDITOR
	[SerializeField, Min(1)]
	protected int _minTurns = 1;

	[SerializeField, Min(1)]
	protected int _maxTurns = 1;
#endif

	protected int _repetitions = 0;



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(VariableTurnAttack))]
	public class VariableTurnAttackPropertyDrawer : AttackEffectPropertyDrawer
	{
		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_name"));

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_forecast"));

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Minimum and Maximum Turns This Effect Can Take ", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "(Not Used In-Game)", EditorStyles.boldLabel);

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				GUI.enabled = false;
				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_minTurns"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_maxTurns"));
				GUI.enabled = true;

				position.width = tmpWidth;
				position.x = tmpX;
			}

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTurns"), new GUIContent("Repeat Count"));

			{
				position.y += Y_OFFSET;

				float tmpWidth = position.width;
				float tmpX = position.x;

				position.width /= 2;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_endsTurn"));

				position.x += position.width;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_isCritical"));

				position.width = tmpWidth;
				position.x = tmpX;
			}

			return position;
		}

		protected override float EffectHeight()
		{
			return 6 * Y_OFFSET;
		}
	}
#endif
}

[Serializable]
public class SandSuckAttack : VariableTurnAttack
{
	private enum SandGatherState
	{
		Needs_To_Gather,
		Has_Attempted_Gather,
		Needs_To_Attack,
		Has_Attacked
	}



	[SerializeField, Min(1)]
	private int _damagePerTile;

	[SerializeField]
	private string _secondTurnForecast;

	// used for shuffling forecasts around when there are multiple
	private string _forecastTmp;

	private int _tilesGathered = 0;
	private SandGatherState _state = SandGatherState.Needs_To_Gather;



	internal override void StartEffect(Enemy owner)
	{
		base.StartEffect(owner);

		_repetitions = 0;

		_tilesGathered = 0;
		_state = SandGatherState.Needs_To_Gather;
	}

	internal override void StartTurn(Enemy owner)
	{
		base.StartTurn(owner);

		switch (_state)
		{
			case SandGatherState.Needs_To_Gather:
			case SandGatherState.Has_Attacked:

				// starting fresh or after an attack, we need to gather

				_tilesGathered = 0;
				_repetitions++;

				_state = SandGatherState.Needs_To_Gather;
				break;

			case SandGatherState.Has_Attempted_Gather:
				if (_tilesGathered == 0)
				{
					// failed to collect sand last turn, needs to gather. Repetition increments
					_repetitions++;

					_state = SandGatherState.Needs_To_Gather;
				}
				else
				{
					// sand has been collected, need to attack
					_state = SandGatherState.Needs_To_Attack;
				}
				break;
		}
	}

	internal override void Reset(Enemy owner)
	{
		base.Reset(owner);

		_repetitions = 0;

		_tilesGathered = 0;
		_state = SandGatherState.Needs_To_Gather;
	}

	internal override bool UpdateEffect(Enemy owner)
	{
		switch (_state)
		{
			case SandGatherState.Needs_To_Gather:
				_tilesGathered = GameBoard.INSTANCE.CountTiles(Tile.TileKind.Sandy);
				GameBoard.INSTANCE.TransformAllTiles(Tile.TileKind.Sandy, Tile.TileKind.Normal);

				_state = SandGatherState.Has_Attempted_Gather;
				break;

			case SandGatherState.Needs_To_Attack:

				Debug.Assert(_tilesGathered > 0);

				Player.INSTANCE._inventory.OnEnemyAttack(_damagePerTile * _tilesGathered, out float modifiedDamage);
				GameObject.Find("Player Damage Popup").GetComponent<DamagePopupScript>().Popup(Mathf.RoundToInt(modifiedDamage));
				Player.INSTANCE.Damage(Mathf.RoundToInt(modifiedDamage));

				_tilesGathered = 0;

				_state = SandGatherState.Has_Attacked;
				break;
		}

		return true;
	}

	// all sub-effects are single-frame right now
	internal override bool IsTurnComplete(Enemy owner) => _state == SandGatherState.Has_Attempted_Gather || _state == SandGatherState.Has_Attacked;

	// if the last loop has terminated
	internal override bool IsEffectComplete(Enemy owner)
	{
		if (_isEffectComplete)
			return true;

		// not on final repetition

		if (_repetitions < _numTurns)
			return false;

		// attack has happened on final repetition

		if (_state == SandGatherState.Has_Attacked)
		{
			_isEffectComplete = true;
			return _isEffectComplete;
		}

		// if we have failed to gather sand on the final repetition, effect is over.

		_isEffectComplete = _state == SandGatherState.Has_Attempted_Gather && _tilesGathered == 0;
		return _isEffectComplete;
	}

	internal override bool OnLastTurn() => _repetitions >= _numTurns;

	public override string FormattedForecast()
	{
		if (_state == SandGatherState.Needs_To_Gather || _state == SandGatherState.Has_Attacked)
		{
			if (_forecast == _secondTurnForecast)
			{
				// the first forecast is in _forecastTmp, so we move it and empty the temporary variable

				_forecast = _forecastTmp;
				_forecastTmp = null;
			}
		}
		else if (_state == SandGatherState.Needs_To_Attack ||
				_state == SandGatherState.Has_Attempted_Gather && _tilesGathered > 0)
		{
			if (_forecast != _secondTurnForecast)
			{
				_forecastTmp = _forecast;
				_forecast = _secondTurnForecast;
			}
		}

		return base.FormattedForecast();
	}



#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(SandSuckAttack))]
	public class SandSuckAttackPropertyDrawer : VariableTurnAttackPropertyDrawer
	{
		protected override Rect EffectGUI(Rect position, GUIContent label, SerializedProperty property)
		{
			position = base.EffectGUI(position, label, property);

			// this is bad practice but there aren't any good alternatives that don't require adding a bunch of Validate() functions.
			property.FindPropertyRelative("_minTurns").intValue = 1;
			property.FindPropertyRelative("_maxTurns").intValue = 2;

			// count variables from here

			position.y += Y_OFFSET;
			EditorGUI.LabelField(position, "Sand Suck Attack Data", EditorStyles.boldLabel);

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_secondTurnForecast"), new GUIContent("Turn 2 Forecast"));

			position.y += Y_OFFSET;
			EditorGUI.PropertyField(position, property.FindPropertyRelative("_damagePerTile"));

			return position;
		}

		protected override float EffectHeight()
		{
			return base.EffectHeight() + 3 * Y_OFFSET;
		}
	}
#endif
}