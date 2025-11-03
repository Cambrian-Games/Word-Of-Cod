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
	private int _lastRule = -1;
	internal int LastRule => _lastRule;
	internal int _turnsSinceLastAction = 0;

	[SerializeField]
	private AttackPriority _attackPriority;
	public AttackPriority Priority => _attackPriority;
	[SerializeField]
	private bool _canRepeatLastAction = true;

	private bool _isTurnComplete = false;
	public bool IsTurnComplete => _isTurnComplete;

	public override void UpdateTurn()
	{
		base.UpdateTurn();

		if (!_isTurnComplete)
			_isTurnComplete = _rules[_currentRule].UpdateRule();
	}

	public override void StartTurn()
	{
		if (_currentRule != -1)
		{
			_lastRule = _currentRule;
		}

		// select new rule

		switch (_attackPriority)
		{
			case AttackPriority.Loop:
				int nextRule = (_lastRule + 1) % _rules.Count;

				if (_rules[nextRule].CanRun(this))
				{
					_currentRule = nextRule;
					_rules[_currentRule].StartRule();
					_turnsSinceLastAction = 0;
				}
				else
				{
					_currentRule = -1;
				}
				break;

			case AttackPriority.First_Available:
				bool foundViableRule = false;

				for (int i = 0; i < _rules.Count; i++)
				{
					if (!_canRepeatLastAction && i == _lastRule)
						continue;

					if (_rules[i].CanRun(this))
					{
						_currentRule = i;
						_rules[_currentRule].StartRule();
						_turnsSinceLastAction = 0;
						foundViableRule = true;
						break;
					}
				}

				if (!foundViableRule)
				{
					_currentRule = -1;
				}
				break;

			case AttackPriority.Random_From_All_Available:
				List<int> ruleCandidates = new List<int>();

				for (int i = 0; i < _rules.Count; i++)
				{
					if (!_canRepeatLastAction && i == _lastRule)
						continue;

					if (_rules[i].CanRun(this))
					{
						ruleCandidates.Add(i);
					}
				}

				if (ruleCandidates.Count > 0)
				{
					int index = UnityEngine.Random.Range(0, ruleCandidates.Count);
					_currentRule = ruleCandidates[index];
					_rules[_currentRule].StartRule();
					_turnsSinceLastAction = 0;
				}
				else
				{
					_currentRule = -1;
				}
				break;
		}

		// if there is no rule, the turn is complete and this does nothing

		_isTurnComplete = _currentRule == -1;
	}

	public override void EndTurn()
	{
		base.EndTurn();
		if (_currentRule != -1)
			_rules[_currentRule].EndRule();

		_turnsSinceLastAction++;
	}
}