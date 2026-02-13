using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
	// config

	[SerializeField]
	private List<AttackRule> _rules;
	public List<AttackRule> Rules => new List<AttackRule>(_rules);

	[SerializeField]
	private List<AttackRule> _interruptRules;
	public List<AttackRule> InterruptRules => new List<AttackRule>(_interruptRules);

	#region Attack Rule Priority
	[SerializeField]
	private AttackPriority _attackPriority;
	public AttackPriority Priority => _attackPriority;
	[SerializeField]
	private bool _canRepeatLastAction = true;
	#endregion

	// attack state

	internal int _currentRuleIndex = -1;
	public AttackRule CurrentRule => _currentRuleIndex == -1 ? null : _rules[_currentRuleIndex];

	private int _lastRuleIndex = -1;
	internal int LastRuleIndex => _lastRuleIndex;

	internal int _roundsSinceLastAction = 0;

	internal int _currentInterruptIndex = -1;
	public AttackRule CurrentInterrupt => _currentInterruptIndex == -1 ? null : _interruptRules[_currentInterruptIndex];

	private bool _isTurnComplete = false;
	public bool IsTurnComplete => _isTurnComplete;

	[SerializeField] //can be temporary, but for testing forecast display i need to see current forecast
    internal string _currentForecast = "";

    [SerializeField]
    private string _defaultForecast = "$NAME is waiting.";

	protected override void Awake()
	{
		base.Awake();

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
	}

	public override void UpdateTurn()
	{
		base.UpdateTurn();

		if (_isTurnComplete)
			return;

		if (CurrentInterrupt != null)
		{
			_isTurnComplete = CurrentInterrupt.UpdateTurn();
			return;
		}

		if (CurrentRule != null)
		{
			_isTurnComplete = CurrentRule.UpdateTurn();
			return;
		}

		// safeguard, if there is no interrupt and no rule, the turn ends.

		_isTurnComplete = true;
	}

	public void StartRound()
	{
		_roundsSinceLastAction++;
		_rules.ForEach(rule => rule._roundsSinceLastUsed++);
		_interruptRules.ForEach(interrupt => interrupt._roundsSinceLastUsed++);

		if (CurrentInterrupt != null)
		{
			CurrentInterrupt.StartRound(); // set turns since last use to 0 in AttackRule::StartTurn instead of here?
			CurrentInterrupt._roundsSinceLastUsed = 0;
			_roundsSinceLastAction = 0;
			UpdateForecast();
			return;
		}

		// if we don't have a rule, find one.

		if (CurrentRule == null)
		{
			TryFindRule();
		}

		// if we have a rule (whether from the previous turn or newly-selected), start the round.

		if (CurrentRule != null)
		{
			CurrentRule.StartRound(); // set turns since last use to 0 in AttackRule::StartTurn instead of here?
			CurrentRule._roundsSinceLastUsed = 0;
			_roundsSinceLastAction = 0;
			UpdateForecast();
			return;
		}

		UpdateForecast();
	}

	private void UpdateForecast()
	{
        _currentForecast = null;

        if (CurrentInterrupt != null)
            _currentForecast = CurrentInterrupt.GetForecast();

        else if (CurrentRule != null)
            _currentForecast = CurrentRule.GetForecast();

        // if the above attempts fail, fall back to _defaultForecast

        if (_currentForecast == null || _currentForecast.Length == 0)
            _currentForecast = _defaultForecast;
    }

	public override void StartTurn()
	{
		_isTurnComplete = false;

		// if we don't have an interrupt, look for one. An interrupt can't override another interrupt for now.

		bool newInterrupt = false;

		if (CurrentInterrupt == null && (CurrentRule == null || !CurrentRule._uninterruptible))
		{
			_currentInterruptIndex = _interruptRules.FindIndex(interrupt => interrupt.CanRun(this));
			newInterrupt = (CurrentInterrupt != null);
		}

		// if we have an interrupt, run it

		if (CurrentInterrupt != null)
		{
			if (CurrentRule != null)  // this should never happen but is a good safeguard.
			{
				if (CurrentRule.PastInterruptCheckpoint())
				{
					_lastRuleIndex = _currentRuleIndex;
				}

				CurrentRule.Cancel();
				_currentRuleIndex = -1;
			}

			if (newInterrupt)
			{
				_interruptRules[_currentInterruptIndex].StartRule();
				_interruptRules[_currentInterruptIndex].StartRound();
			}

			_interruptRules[_currentInterruptIndex].StartTurn();
		}

		// if we don't have an interrupt but this rule should be cancelled for some other reason, cancel it

		else if (CurrentRule != null)
		{
			if (CurrentRule.ShouldCancel(this))
			{
				if (CurrentRule.PastInterruptCheckpoint())
				{
					_lastRuleIndex = _currentRuleIndex;
				}

				CurrentRule.Cancel();
				_currentRuleIndex = -1;
			}
			else
			{
				CurrentRule.StartTurn();
			}
		}
		else // no rule, immediately mark the turn as complete
		{
			_isTurnComplete = true;
		}
	}

	public override void EndTurn()
	{
		base.EndTurn();

		if (CurrentRule != null)
		{
			CurrentRule.EndTurn();

			if (CurrentRule.Complete())
			{
				CurrentRule.EndRule();
				_lastRuleIndex = _currentRuleIndex;
				_currentRuleIndex = -1;
			}
		}

		else if (CurrentInterrupt != null)
		{
			CurrentInterrupt.EndTurn();

			if (CurrentInterrupt.Complete())
			{
				CurrentInterrupt.EndRule();
				_currentInterruptIndex = -1;
			}
		}
	}

	private void TryFindRule()
	{
		switch (_attackPriority)
		{
			case AttackPriority.Loop:
				int nextRuleIndex = (_lastRuleIndex + 1) % _rules.Count;

				if (_rules[nextRuleIndex].CanRun(this))
				{
					_currentRuleIndex = nextRuleIndex;
				}
				else
				{
					_currentRuleIndex = -1;
				}
				break;

			case AttackPriority.First_Available:
				bool foundViableRule = false;

				for (int i = 0; i < _rules.Count; i++)
				{
					if (!_canRepeatLastAction && i == _lastRuleIndex)
						continue;

					if (_rules[i].CanRun(this))
					{
						_currentRuleIndex = i;
						foundViableRule = true;
						break;
					}
				}

				if (!foundViableRule)
				{
					_currentRuleIndex = -1;
				}
				break;

			case AttackPriority.Random_From_All_Available:
				List<int> ruleCandidates = new List<int>();

				for (int i = 0; i < _rules.Count; i++)
				{
					if (!_canRepeatLastAction && i == _lastRuleIndex)
						continue;

					if (_rules[i].CanRun(this))
					{
						ruleCandidates.Add(i);
					}
				}

				if (ruleCandidates.Count > 0)
				{
					int index = Random.Range(0, ruleCandidates.Count);
					_currentRuleIndex = ruleCandidates[index];
				}
				else
				{
					_currentRuleIndex = -1;
				}
				break;
		}

		if (CurrentRule != null)
		{
			CurrentRule.StartRule();
		}
	}

    internal string FormattedForecast()
    {
        return _currentForecast.Replace("$NAME", this._displayName);
    }
}