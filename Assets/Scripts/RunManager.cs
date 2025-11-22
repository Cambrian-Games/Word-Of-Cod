using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

		_currentRun.Add(new Vector2Int(option, pool.GetWeightedIndex(eventIndex - 1, _currentRun[eventIndex - 1])));
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
		private GameObject _prefab;
		[SerializeField, Min(0.1f)]
		private float _weight = 1.0f;
		public float Weight => _weight;
	}

	internal int GetWeightedIndex(int lastRunIndex, Vector2Int lastOption)
	{
		if (_canRepeat == RepeatKind.No_Consecutive)
		{
			RunEvent lastEvent = RunManager.INSTANCE.Event(lastRunIndex);
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
}