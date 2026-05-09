using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum AttackSchedulePolicy
{
	[InspectorName("Repeat Current Until Next Is Available")]
	Repeat_Until_Next_Available,
	[InspectorName("Next Available")]
	Next_Available,
	[InspectorName("Select First Available")]
	First_Available,
	[InspectorName("Select Random From All Available")]
	Random_From_All_Available
}



public class Enemy : Entity
{
	// config

	[SerializeField]
	private AttackSchedulePolicy _schedulePolicy;
	public AttackSchedulePolicy SchedulePolicy => _schedulePolicy;

	[SerializeField]
	private List<AttackRule> _rules;
	public List<AttackRule> Rules => new List<AttackRule>(_rules);

	[SerializeField]
	private List<AttackRule> _interruptRules;
	public List<AttackRule> InterruptRules => new List<AttackRule>(_interruptRules);

	// attack state

	internal int _currentRuleIndex = -1;
	internal int _currentInterruptIndex = -1;

	// used for attack selection
	private int _lastRuleIndex = -1;

	public AttackRule CurrentRule => _currentRuleIndex == -1 ? null : _rules[_currentRuleIndex];
	public AttackRule CurrentInterrupt => _currentInterruptIndex == -1 ? null : _interruptRules[_currentInterruptIndex];

	private bool _isTurnComplete = false;
	public bool IsTurnComplete => _isTurnComplete;

	private string _currentForecast;

	private bool _hasSelectedAttack = false;

	// create once, never re-assign
	internal readonly Dictionary<string, float> _blackboard = new Dictionary<string, float>();




	protected override void Awake()
	{
		base.Awake();

#if UNITY_EDITOR
		bool hasNullRules = false;

		for (int i = _rules.Count - 1; i >= 0; i--)
		{
			if (_rules[i] == null)
			{
				hasNullRules = true;
				_rules.RemoveAt(i);
			}
			else
			{
				for (int j = _rules[i].Effects.Count - 1; j >= 0; j--)
				{
					if (_rules[i].Effects[j] == null)
					{
						Debug.LogError($"{_displayName}: {_rules[i]._name}'s effect {j} is null!");
						_rules[i].Effects.RemoveAt(j);
					}
				}
			}
		}

		for (int i = _interruptRules.Count - 1; i >= 0; i--)
		{
			if (_interruptRules[i] == null)
			{
				hasNullRules = true;
				_interruptRules.RemoveAt(i);
			}
			else
			{
				for (int j = _interruptRules[i].Effects.Count - 1; j >= 0; j--)
				{
					if (_interruptRules[i].Effects[j] == null)
					{
						Debug.LogError($"{_displayName}: {_interruptRules[i]._name}'s effect {j} is null!");
						_interruptRules[i].Effects.RemoveAt(j);
					}
				}
			}
		}

		Debug.Assert(!hasNullRules, $"Enemy {name} has at least one null rule! Removing all null rules.");
#endif
	}

	private void OnValidate()
	{
		for (int ruleIndex = 0; ruleIndex < _rules.Count; ruleIndex++)
		{
			_rules[ruleIndex]._index = ruleIndex;
			// if we decide we want effect-specific validation we can add it here
		}

		for (int ruleIndex = 0; ruleIndex < _interruptRules.Count; ruleIndex++)
		{
			_interruptRules[ruleIndex]._index = ruleIndex;
		}
	}

	public bool SelectRule()
	{
		if (_hasSelectedAttack)
		{
			Debug.Assert((CurrentRule != null) != (CurrentInterrupt != null), "One of CurrentRule and CurrentInterrupt should always be null");
		}

		_hasSelectedAttack = true;

		// check if the current rule should continue to run. At least one of CurrentRule and CurrentInterrupt exists here
		//  if it is not the first time an attack is selected.

		AttackRule activeRule = CurrentInterrupt ?? CurrentRule;

		if (activeRule != null && !activeRule.IsComplete(this))
		{
			// cannot leave a critical effect

			if (activeRule.InCriticalEffect())
				return false;

			if (!activeRule.CanRun(this))
			{
				activeRule.CancelRule(this);

				// one of these was already -1 when entering this section, and we are cancelling the other one

				_currentInterruptIndex = -1;
				_currentRuleIndex = -1;
			}
		}

		// check for an applicable interrupt. This CanRun() check may be funky for interrupts and may require
		//  extra fields in AttackRule::Condition.

		List<AttackRule> interruptCandidates = new List<AttackRule>();

		foreach (AttackRule interrupt in _interruptRules)
		{
			// no self-interrupting. Make a duplicate if you want this
			if (interrupt != null && (interrupt == CurrentInterrupt))
				continue;

			if (!interrupt.CanRun(this))
				continue;

			interruptCandidates.Add(interrupt);
		}

		if (interruptCandidates.Count > 0)
		{
			int newInterruptIndex = -1;

			float totalWeight = interruptCandidates.Sum(rule => rule._weight);

			if (totalWeight == 0)
			{
				newInterruptIndex = interruptCandidates[Random.Range(0, interruptCandidates.Count)]._index;
			}
			else
			{
				float output = Random.Range(0, totalWeight);

				int index = 0;

				while (output > interruptCandidates[index]._weight)
				{
					output -= interruptCandidates[index]._weight;
					index++;
				}

				newInterruptIndex = interruptCandidates[index]._index;
			}

			// if we've found a new interrupt

			//if (newInterruptIndex != -1)
			{
				Debug.Assert(_interruptRules[newInterruptIndex].CanRun(this));

				_currentInterruptIndex = newInterruptIndex;

				// ensure that CurrentRule is null because CurrentInterrupt is now non-null

				_lastRuleIndex = _currentRuleIndex;
				_currentRuleIndex = -1;

				CurrentInterrupt.StartRule(this);

				return true;
			}
		}

		// no applicable new interrupt was found, and we have an in-progress rule (normal or interrupt), stay in it

		activeRule = CurrentInterrupt ?? CurrentRule;

		if (activeRule != null && !activeRule.IsComplete(this))
			return false;

		// clear out any state data in all rules. Required for CanRun() to behave properly on a previously-started rule

		foreach (AttackRule rule in _rules)
		{
			rule.CancelRule(this);
		}

		// no active rule or interrupt, select a new attack

		#region Local Selection Functions
			int RepeatUntilNextAvailable()
			{
				int nextRuleIndex = (_lastRuleIndex + 1) % _rules.Count;

				if (_rules[nextRuleIndex].CanRun(this))
					return nextRuleIndex;

				Debug.Assert(_rules[Mathf.Max(0, _lastRuleIndex)].CanRun(this), "Can't select a rule! Please check configuration.");
				return Mathf.Max(0, _lastRuleIndex);
			}

			int NextAvailable()
			{
				int numRules = _rules.Count;
				int nextRuleIndex = (_lastRuleIndex + 1) % numRules;

				// loop through every rule starting with the next in line, checking whether it can run.
				// The loop will terminate the first time it can run a rule, or if nextRuleIndex == _lastRuleIndex (after checking if it can run)

				while (!_rules[nextRuleIndex].CanRun(this) && nextRuleIndex != _lastRuleIndex)
					nextRuleIndex = nextRuleIndex + 1 % numRules;

				Debug.Assert(_rules[nextRuleIndex].CanRun(this), "Can't select a rule! Please check configuration.");
				return nextRuleIndex;
			}

			int FirstAvailable()
			{
				int numRules = _rules.Count;
				int nextRuleIndex = 0;

				// loop through every rule starting at 0, checking whether it can run.
				// The loop will terminate after checking every rule or when it can find a valid rule.

				while (nextRuleIndex < numRules && !_rules[nextRuleIndex].CanRun(this))
					nextRuleIndex = nextRuleIndex + 1;

				Debug.Assert(nextRuleIndex < numRules && _rules[nextRuleIndex].CanRun(this), "Can't select a rule! Please check configuration.");
				return nextRuleIndex < numRules ? nextRuleIndex : 0;
			}

			int RandomFromAllAvailable()
			{
				List<AttackRule> attackCandidates = _rules.Where(rule => rule.CanRun(this)).ToList();

				if (attackCandidates.Count == 0)
				{
					Debug.Assert(false, "Can't select a rule! Please check configuration.");
					return 0;
				}

				float totalWeight = attackCandidates.Sum(rule => rule._weight);

				if (totalWeight == 0)
				{
					return attackCandidates[Random.Range(0, attackCandidates.Count)]._index;
				}
				else
				{
					float output = Random.Range(0, totalWeight);

					int index = 0;

					while (output > attackCandidates[index]._weight)
					{
						output -= attackCandidates[index]._weight;
						index++;
					}

					return attackCandidates[index]._index;
				}

			}
			#endregion

		int pendingRule = _schedulePolicy switch
		{
			AttackSchedulePolicy.Repeat_Until_Next_Available => RepeatUntilNextAvailable(),
			AttackSchedulePolicy.Next_Available => NextAvailable(),
			AttackSchedulePolicy.First_Available => FirstAvailable(),
			AttackSchedulePolicy.Random_From_All_Available => RandomFromAllAvailable(),
			_ => throw new System.NotImplementedException(),
		};

		Debug.Assert(_rules[pendingRule].CanRun(this));

		// ensure that CurrentRule is non-null and CurrentInterrupt is null

		_lastRuleIndex = _currentRuleIndex = pendingRule;
		_currentInterruptIndex = -1;

		CurrentRule.StartRule(this);

		return true;
	}

	public void StartRound()
	{
		_lastDamageTaken = 0;

		SelectRule();

		(CurrentInterrupt ?? CurrentRule).StartRound(this);

		UpdateForecast();
	}

	public void StartTurn()
	{
		_isTurnComplete = false;

		if (SelectRule())
		{
			// if the current attack has changed, start the round for the new rule and update the forecast

			(CurrentInterrupt ?? CurrentRule).StartRound(this);
			UpdateForecast();
		}

		(CurrentInterrupt ?? CurrentRule).StartTurn(this);
	}

	public void UpdateTurn()
	{
		if (_isTurnComplete)
			return;

		_isTurnComplete = (CurrentInterrupt ?? CurrentRule).UpdateTurn(this);
	}

	private void UpdateForecast()
	{
        _currentForecast = (CurrentInterrupt ?? CurrentRule).FormattedForecast();

		Debug.Assert(_currentForecast != null, "CurrentInterrupt or CurrentRule should be non-null.");
    }
	
	// this should be cached and should be called by a UI file, not the Battle Manager. Fine for now.
    internal string FormattedForecast()
    {
        return _currentForecast.Replace("$NAME", this._displayName);
    }
}