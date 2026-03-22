using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.Analytics;
using UnityEngine.UnityConsent;

public class RunManager : MonoBehaviour
{
	[SerializeField]
	private List<EncounterPool> _pools;

	[SerializeField]
	private List<RunEvent> _runFormat;
	public List<RunEvent> RunFormat => _runFormat;

	[Header("Do not modify this! This shows what has been selected so far")]
	[SerializeField]
	private List<SelectedEvent> _currentRun;

	public static RunManager INSTANCE;

	public float _distanceBetweenEvents = 7.5f;
	public float _travelTime = 3.0f;

	public SceneAsset _loseScene;
	public SceneAsset _winScene;

	public GameObject _storeObject;
	private Vector3 _destination;

	private bool _hasSelectedNextEvent = false;

	public AnalyticsManager _analyticsManager;

	public enum RunState
	{
		Nil = -1,
		Run_Start,
		Traveling_To_Next_Event,
		Choice,
		Post_Choice_Travel, // may not be needed if we can reuse Traveling_To_Next_Event
		Enter_Event,
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
		SetRunState(RunState.Run_Start);
		GameObject analyticsGameObject = GameObject.Find("Analytics Manager");
		if (analyticsGameObject)
		{
			_analyticsManager = analyticsGameObject.GetComponent<AnalyticsManager>();
			if (_analyticsManager._analyticsEnabled)
			{
				EndUserConsent.SetConsentState(new ConsentState
				{
					AnalyticsIntent = ConsentStatus.Granted
				});
				Debug.Log("start Data Collection");
			}
		}
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
					if (Player.INSTANCE.transform.position.x > _destination.x) // not great check here
					{
						Player.INSTANCE.transform.position = _destination;

						if (_hasSelectedNextEvent)
						{
							SetRunState(RunState.Enter_Event);
						}
						else // if we haven't already chosen the event, we have a choice to make
						{
							SetRunState(RunState.Choice);
						}
					}
					else
					{
						Player.INSTANCE.transform.position += Vector3.right * _distanceBetweenEvents / _travelTime * Time.deltaTime;
					}
					break;

				case RunState.Choice:
					// I don't like that we're selecting the event here, might store the number and toss it into PostChoiceTravel?
					// and/or have a bool for choiceMade and kick us back into Traveling_To_Next_Event. idk. 
					if (Input.GetMouseButtonDown((int)MouseButton.Left))
					{
						SelectNextEvent(0);
						SetRunState(RunState.Post_Choice_Travel);
					}
					else if (Input.GetMouseButtonDown((int)MouseButton.Right))
					{
						SelectNextEvent(1);
						SetRunState(RunState.Post_Choice_Travel);
					}
					break;

				case RunState.Post_Choice_Travel:
					// the way we calculate a destination (or reaching the destination) in Traveling_To_Next_Event falls apart here and needs a better solution.
					break;
				case RunState.Enter_Event:
					// TODO move camera from overworld view into battle view
					SetRunState(RunState.In_Event);
					break;

				case RunState.In_Event:
					// if the event we're in is a shop, do something here?
					// otherwise we're just waiting for the battle manager to kick us into Post_Event
					break;
				case RunState.Post_Event:
					// if event was shop, I don't think we do anything

					// if this was a fight, we wait for items/relics to be purchased

					// if this was the last event, win

					if (_currentRun.Count == _runFormat.Count)
					{
						SetRunState(RunState.Win);
						break;
					}
					
					if (_currentRun[^1]._encounterKind != EncounterPoolKind.Shop)
					{
						SetRunState(RunState.Traveling_To_Next_Event);
					}
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

		//switch (_state)
		//{
		//
		//}

		_state = newState;

		switch (_state)
		{
			case RunState.Run_Start:
				SetRunState(RunState.Traveling_To_Next_Event);
				break;
			case RunState.Traveling_To_Next_Event:
				_destination = Player.INSTANCE.transform.position + Vector3.right * _distanceBetweenEvents;
				RunEvent evtNext = Event(_currentRun.Count);

				if (evtNext.EventKinds.Count == 1)
				{
					SelectNextEvent(); // we can pick the event now since there aren't multiple options.
				}
				break;

			case RunState.Choice:
				break;
			case RunState.Post_Choice_Travel:
				// currently does nothing due to the way destinations are calculated
				SetRunState(RunState.Enter_Event);
				break;
			case RunState.Enter_Event:
				// would have animations
				_hasSelectedNextEvent = false;
				break;
			case RunState.In_Event:

				EncounterPoolKind encounter = _currentRun[^1]._encounterKind;

				if (encounter == EncounterPoolKind.Shop)
				{
					_storeObject.SetActive(true);
				}
				else
				{
					Enemy enemy = Pool(encounter).EncounterPrefab(_currentRun[^1]._poolIndex);
					BattleManager.INSTANCE.SetEnemy(enemy);
					// This is starting to become a problem, will likely have to be changed later
					BattleManager.INSTANCE.transform.position = (Vector2)Camera.main.transform.position; // the cast sets the z coord to zero
					BattleManager.INSTANCE.Load();
				}
				break;
			case RunState.Post_Event:
				// if this was a fight, display items/relics screen
				BattleManager.INSTANCE.Unload();
				break;
			case RunState.Win:
				break;
			case RunState.Lose:
				SceneManager.LoadScene(_loseScene.name);
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

		SelectedEvent selectedEvent = new SelectedEvent();
		selectedEvent._eventIndex = eventIndex;
		selectedEvent._encounterKind = nextEvent.EventKinds[option];

		switch (selectedEvent._encounterKind)
		{
			case EncounterPoolKind.Shop:
				selectedEvent._poolIndex = -1;
				break;

			case EncounterPoolKind.All:
				Debug.LogError("We don't support EncounterPoolKind.All yet");
				selectedEvent._poolIndex = -1;
				break;

			default:
				EncounterPool pool = Pool(selectedEvent._encounterKind);

				if (eventIndex == 0)
				{
					selectedEvent._poolIndex = pool.GetWeightedIndex(-1, null);
				}
				else
				{
					selectedEvent._poolIndex = pool.GetWeightedIndex(-1, _currentRun[^1]);
				}
				break;
		}

		_currentRun.Add(selectedEvent);

		_hasSelectedNextEvent = true;
	}

	public RunEvent Event(int index) => _runFormat[index];
	public EncounterPool Pool(EncounterPoolKind kind) => _pools.Find(pool => pool.PoolKind == kind);
}

[Serializable]
public class RunEvent
{
	[SerializeField]
	private List<EncounterPoolKind> _eventKinds;

	public List<EncounterPoolKind> EventKinds => _eventKinds;
}

[Serializable]
public class SelectedEvent
{
	public int _eventIndex; // what RunEvent this points to
	public EncounterPoolKind _encounterKind;
	public int _poolIndex = -1; // which encounter was picked from the pool
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

	internal int GetWeightedIndex(int lastEventIndex, SelectedEvent lastSelection)
	{
		if (_canRepeat == RepeatKind.No_Consecutive && lastEventIndex >= 0)
		{
			EncounterPool lastPool = RunManager.INSTANCE.Pool(lastSelection._encounterKind);

			if (lastPool == this)
			{
				float sumNoRepeat = 0;

				for (int i = 0; i < _entries.Length; i++)
				{
					if (i == lastSelection._poolIndex)
						continue;

					sumNoRepeat += _entries[i].Weight;
				}

				float randNoRepeat = UnityEngine.Random.Range(0.0f, 1.0f) * sumNoRepeat; // long-term we should have a centralized RNG so we can have consistent test cases.

				for (int i = 0; i < _entries.Length; i++)
				{
					if (i == lastSelection._poolIndex)
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
        Debug.Assert(0 <= index && index < _entries.Length);
		return _entries[index].Prefab;
	}
}