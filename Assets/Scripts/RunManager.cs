using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static BattleManager;

public class RunManager : MonoBehaviour
{
	[SerializeField]
	private List<EncounterPool> _pools;

	[SerializeField]
	private List<RunEvent> _runFormat;
	public List<RunEvent> RunFormat => _runFormat;

	[Header("Do not modify this! This shows what has been selected so far")]
	public List<Vector2Int> _currentRun;

	public static RunManager INSTANCE;

	public enum RunState
	{
		Nil = -1,
		Run_Start,
		Traveling_To_Next_Event,
		Choice,
		Post_Choice_Travel, // may not be needed if we can reuse Traveling_To_Next_Event
		In_Event,
		Post_Event, // rewards post-battle, usually

		Win,
		Lose
	}

	private RunState _state = RunState.Nil;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		//SelectNextEvent();
		//SelectNextEvent();
		//SelectNextEvent();
    }

    // Update is called once per frame
    void Update()
	{
		UpdateRunState();
	}

	internal void UpdateRunState()
	{
		while (true)
		{
			RunState stateCur = _state;

			switch (_state)
			{
				case RunState.Nil:
					break;
				case RunState.Run_Start:
					break;
				case RunState.Traveling_To_Next_Event:
					// if travel is complete and event has a choice, switch to choice. Else, switch to event.
					break;
				case RunState.Choice:
					break;
				case RunState.Post_Choice_Travel:
					break;
				case RunState.In_Event:
					break;
				case RunState.Post_Event:
					break;
				case RunState.Win:
					break;
				case RunState.Lose:
					break;
			}

			if (stateCur == _state)
				break;
		}
	}
	internal void SetRunState(RunState newState)
	{
		if (newState == _state)
			return;

		// leave old state

		switch (_state)
		{

		}

		_state = newState;

		switch (_state)
		{
			case RunState.Run_Start:
				SetRunState(RunState.Traveling_To_Next_Event);
				break;
			case RunState.Traveling_To_Next_Event:
				break;
			case RunState.Choice:
				break;
			case RunState.Post_Choice_Travel:
				break;
			case RunState.In_Event:
				break;
			case RunState.Post_Event:
				break;
			case RunState.Win:
				break;
			case RunState.Lose:
				break;
		}
	}

	private void OnValidate()
	{
		if (_pools != null)
		{
			bool[] poolsFound = new bool[(int) EncounterPoolKind.Max];

			for (int i = _pools.Count - 1; i >= 0; i--)
			{
				if (poolsFound[(int) _pools[i].PoolKind])
				{
					Debug.LogWarning($"Found more than one {_pools[i].PoolKind} Encounter Pool");
				}

				poolsFound[(int)_pools[i].PoolKind] = true;

				switch (_pools[i].PoolKind)
				{
					case EncounterPoolKind.All:
					case EncounterPoolKind.Shop:
						Debug.LogError($"Do not create pools with pool kind {_pools[i].PoolKind}, that is metadata.");
						break;
				}
			}
		}
	}


	public void SelectNextEvent(int option = 0)
	{
		if (_currentRun.Count >= _runFormat.Count)
			return;

		int eventIndex = _currentRun.Count;

		RunEvent nextEvent = _runFormat[eventIndex];

		if (nextEvent.EventKinds.Count == 0)
			return;

		if (nextEvent.EventKinds.Count < option)
		{
			Debug.LogWarning("Invalid option for RunEvent!");
			option = 0;
		}

		EncounterPoolKind poolKind = nextEvent.EventKinds[option];

		if (poolKind == EncounterPoolKind.Shop)
		{
			_currentRun.Add(new Vector2Int(option, -1));
			return;
		}

		if (poolKind == EncounterPoolKind.All)
		{
			Debug.LogError("We don't support EncounterPoolKind.All yet");
			_currentRun.Add(new Vector2Int(option, -1));
			return;
		}

		EncounterPool pool = _pools.Find(pool => pool.PoolKind == nextEvent.EventKinds[option]);

		if (eventIndex == 0)
		{
			_currentRun.Add(new Vector2Int(option, pool.GetWeightedIndex(-1, Vector2Int.zero)));
		}
		else
		{
			_currentRun.Add(new Vector2Int(option, pool.GetWeightedIndex(eventIndex - 1, _currentRun[eventIndex - 1])));
		}	
	}

	public RunEvent Event(int index) => _runFormat[index];
	public EncounterPool Pool(EncounterPoolKind kind) => _pools.Find(pool => pool.PoolKind == kind);

	public void WinFight()
	{
		// show shop, etc

		BattleManager.INSTANCE.Unload();

		// animation

		SelectNextEvent(0);

		const int CHOICE_INDEX = 0;
		const int ENCOUNTER_INDEX = 1;

		Vector2Int stage = _currentRun[^1];

		RunEvent evt = Event(_currentRun.Count - 1);
		EncounterPoolKind encounter = evt.EventKinds[stage[CHOICE_INDEX]];

		if (encounter != EncounterPoolKind.Shop)
		{
			Enemy enemy = Pool(encounter).EncounterPrefab(stage[ENCOUNTER_INDEX]);
			BattleManager.INSTANCE.SetEnemy(enemy);
			BattleManager.INSTANCE.Load();
		}
		throw new NotImplementedException();
	}
}


[Serializable]
public class RunEvent
{
	[SerializeField]
	private List<EncounterPoolKind> _eventKinds;

	public List<EncounterPoolKind> EventKinds => _eventKinds;
}

public enum EncounterPoolKind
{
	Area_1_Common,
	Area_1_Miniboss,

	Area_1_NonBoss,

	Area_1_Boss,

	All_Common,
	All_Miniboss,

	All_NonBoss,

	All_Boss,

	All,

	Shop,

	[InspectorName(null)]
	Max
}

[Serializable]
public class EncounterPool
{
	[SerializeField]
	private EncounterPoolKind _poolKind;
	public EncounterPoolKind PoolKind => _poolKind;

	public enum RepeatKind
	{
		Allowed,
		No_Consecutive,
		Never
	}

	[SerializeField]
	private RepeatKind _canRepeat;
	public RepeatKind CanRepeat => _canRepeat;

	[SerializeField]
	private PoolEntry[] _entries;

	[Serializable]
	public class PoolEntry
	{
		[SerializeField]
		private Enemy _prefab;
		public Enemy Prefab => _prefab;
		[SerializeField, Min(0.1f)]
		private float _weight = 1.0f;
		public float Weight => _weight;
	}

	internal int GetWeightedIndex(int lastEventIndex, Vector2Int lastOption)
	{
		if (_canRepeat == RepeatKind.No_Consecutive && lastEventIndex >= 0)
		{
			RunEvent lastEvent = RunManager.INSTANCE.Event(lastEventIndex);
			EncounterPool lastPool = RunManager.INSTANCE.Pool(lastEvent.EventKinds[lastOption[0]]);

			if (lastPool == this)
			{
				float sumNoRepeat = 0;

				for (int i = 0; i < _entries.Length; i++)
				{
					if (i == lastOption[1])
						continue;

					sumNoRepeat += _entries[i].Weight;
				}

				float randNoRepeat = UnityEngine.Random.Range(0.0f, 1.0f) * sumNoRepeat; // long-term we should have a centralized RNG so we can have consistent test cases.

				for (int i = 0; i < _entries.Length; i++)
				{
					if (i == lastOption[1])
						continue;

					if (randNoRepeat < _entries[i].Weight)
						return i;

					randNoRepeat -= _entries[i].Weight;
				}

				return 0;
			}
		}

		if (_canRepeat == RepeatKind.Never)
		{
			throw new NotSupportedException("We do not currently support encounter pools that can never repeat an entry. If/when we run into cases where this is needed, we'll add it");
		}

		float sum = _entries.Sum(entry => entry.Weight);
		float rand = UnityEngine.Random.Range(0.0f, 1.0f) * sum; // long-term we should have a centralized RNG so we can have consistent test cases.

		for (int i = 0; i < _entries.Length; i++)
		{
			if (rand < _entries[i].Weight)
				return i;

			rand -= _entries[i].Weight;
		}

		return 0;
	}

	public Enemy EncounterPrefab(int index)
	{
		return _entries[index].Prefab;
	}
}