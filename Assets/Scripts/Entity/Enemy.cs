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
	private int _lastRuleIndex = -1;

	public AttackRule CurrentRule => _currentRuleIndex == -1 ? null : _rules[_currentRuleIndex];
	public AttackRule CurrentInterrupt => _currentInterruptIndex == -1 ? null : _interruptRules[_currentInterruptIndex];

	private bool _isTurnComplete = false;
	public bool IsTurnComplete => _isTurnComplete;

	private string _currentForecast;

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
		}

		for (int i = _interruptRules.Count - 1; i >= 0; i--)
		{
			if (_interruptRules[i] == null)
			{
				hasNullRules = true;
				_interruptRules.RemoveAt(i);
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
		}

		for (int ruleIndex = 0; ruleIndex < _interruptRules.Count; ruleIndex++)
		{
			_interruptRules[ruleIndex]._index = ruleIndex;
		}
	}

	public bool SelectRule()
	{
		Debug.Assert((CurrentRule != null) != (CurrentInterrupt != null), "One of CurrentRule and CurrentInterrupt should always be null");

		// check if the current interrupt should continue to run

		if (CurrentInterrupt != null)
		{
			if (CurrentInterrupt.InCriticalEffect())
				return false;

			else if (!CurrentInterrupt.CanRun(this))
			{
				CurrentRule.CancelRule(this);
				_currentInterruptIndex = -1;
			}
		}

		// check if the current rule should continue to run. CurrentRule and CurrentInterrupt should never both be non-null

		if (CurrentRule != null)
		{
			if (CurrentRule.InCriticalEffect())
				return false;

			else if (!CurrentRule.CanRun(this))
			{
				CurrentRule.CancelRule(this);
				_currentRuleIndex = -1;
			}
		}

		// check for an interrupt

		List<AttackRule> interruptCandidates = _interruptRules.Where(interrupt => interrupt.CanRun(this)).ToList();

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

			if (newInterruptIndex != -1)
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

		// no applicable interrupt was found, and we have an in-progress rule

		if (CurrentRule != null && !CurrentRule.IsComplete(this))
			return false;

		#region Local Selection Functions
			int RepeatUntilNextAvailable()
			{
				int nextRuleIndex = (_lastRuleIndex + 1) % _rules.Count;

				if (_rules[nextRuleIndex].CanRun(this))
					return nextRuleIndex;

				Debug.Assert(_rules[_lastRuleIndex].CanRun(this), "Can't select a rule! Please check configuration.");
				return _lastRuleIndex;
			}

			int NextAvailable()
			{
				int nextRuleIndex = (_lastRuleIndex + 1) % _rules.Count;

				// loop through every rule starting with the next in line, checking whether it can run.
				// The loop will terminate the first time it can run a rule, or if nextRuleIndex == _lastRuleIndex (after checking if it can run)

				while (!_rules[nextRuleIndex].CanRun(this) && nextRuleIndex != _lastRuleIndex)
					nextRuleIndex = nextRuleIndex + 1 % _rules.Count;

				Debug.Assert(_rules[nextRuleIndex].CanRun(this), "Can't select a rule! Please check configuration.");
				return nextRuleIndex;
			}

			int FirstAvailable()
			{
				int nextRuleIndex = 0;
				int numRules = _rules.Count;

				// loop through every rule starting at 0, checking whether it can run.
				// The loop will terminate after checking every rule or when it can find a valid rule.

				while (nextRuleIndex < numRules && !_rules[nextRuleIndex].CanRun(this))
					nextRuleIndex = nextRuleIndex + 1;

				Debug.Assert(nextRuleIndex < numRules && _rules[nextRuleIndex].CanRun(this), "Can't select a rule! Please check configuration.");
				return nextRuleIndex < numRules ? nextRuleIndex : 0;
			}

			int RandomFromAllAvailable()
			{
				List<AttackRule> candidates = _rules.Where(rule => rule.CanRun(this)).ToList();

				if (candidates.Count == 0)
				{
					Debug.Assert(candidates.Count > 0, "Can't select a rule! Please check configuration.");
					return 0;
				}

				float totalWeight = candidates.Sum(rule => rule._weight);

				if (totalWeight == 0)
				{
					return interruptCandidates[Random.Range(0, interruptCandidates.Count)]._index;
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

					return interruptCandidates[index]._index;
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
    internal string FormattedForecast()
    {
        return _currentForecast.Replace("$NAME", this._displayName);
    }
}