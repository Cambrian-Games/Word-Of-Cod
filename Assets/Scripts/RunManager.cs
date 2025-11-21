using System;
using System.Collections.Generic;
using UnityEngine;

public class RunManager : MonoBehaviour
{
	[SerializeField]
	private List<EncounterPool> _pools;

	[SerializeField]
	private List<EncounterPoolKind> _runFormat;

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
		Never,
		No_Consecutive,
		Allowed
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
	}
}