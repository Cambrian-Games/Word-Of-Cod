using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Encounter Pool", menuName = "Scriptable Objects/Encounter Pool")]
public class EncounterPool : ScriptableObject
{
    public static Dictionary<EncounterPool, EncounterPool.SpawnHistory> SPAWN_HISTORIES = new Dictionary<EncounterPool, SpawnHistory>();

    public enum RepeatKind
	{
		Allowed,
        [InspectorName("No Consecutive")]
		No_Consecutive,
		Never
	}

	[SerializeField]
	private RepeatKind _canRepeat;
	public RepeatKind CanRepeat => _canRepeat;

	[SerializeField]
	private List<PoolEntry> _entries;

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

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PoolEntry))]
    public class PoolEntryDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.MultiPropertyField(position, new GUIContent[] { new GUIContent("Prefab  "), new GUIContent("Weight  ") }, property.FindPropertyRelative("_prefab"));
        }
    }
#endif

    public Enemy GetNextPrefab(SpawnHistory history)
    {
        if (!history.MatchesSource(this))
        {
            Debug.LogError("Incompatible spawn history!");
            return null;
        }

        List<PoolEntry> spawnCandidates;

        switch (_canRepeat)
        {
            case RepeatKind.Never:
                if (history.SpawnCount() >= _entries.Count)
                {
                    Debug.LogError("Out of unique spawns!");
                    return null;
                }

                spawnCandidates = _entries.Where(entry => !history.MatchesAnySpawn(entry.Prefab)).ToList();
                break;

            case RepeatKind.No_Consecutive:
                if (_entries.Count == 1)
                {
                    Debug.LogError("No spawn options!");
                    return null;
                }

                spawnCandidates = _entries.Where(entry => !history.MatchesLastSpawn(entry.Prefab)).ToList();
                break;

            default:
                spawnCandidates = _entries;
                break;
        }

        float sum = spawnCandidates.Sum(entry => entry.Weight);
        float rand = UnityEngine.Random.Range(0.0f, 1.0f) * sum;

        int spawnIter = 0;

        while ((spawnIter < spawnCandidates.Count - 1) && rand > spawnCandidates[spawnIter].Weight)
        {
            rand -= spawnCandidates[spawnIter].Weight;
            spawnIter++;
        }

        return spawnCandidates[spawnIter].Prefab;
    }

    public SpawnHistory CreateSpawnHistory() => new SpawnHistory(this);

    public class SpawnHistory
    {
        private readonly EncounterPool _sourcePool;
        private readonly List<Enemy> _previousSpawns;

        public SpawnHistory(EncounterPool source)
        {
            _sourcePool = source;
            _previousSpawns = new List<Enemy>();
        }

        public bool TryAddEntry(EncounterPool source, Enemy spawnPrefab)
        {
            if (source != _sourcePool)
            {
                Debug.LogError($"This SpawnHistory does not belong to {source}");
                return false;
            }

            _previousSpawns.Add(spawnPrefab);
            return true;
        }

        public bool MatchesSource(EncounterPool source) => _sourcePool == source;

        public bool MatchesLastSpawn(Enemy spawnPrefab)
        {
            if (spawnPrefab == null || _previousSpawns.Count == 0)
                return false;

            return spawnPrefab == _previousSpawns.Last();
        }

        public bool MatchesAnySpawn(Enemy spawnPrefab)
        {
            if (spawnPrefab == null || _previousSpawns.Count == 0)
                return false;

            foreach (Enemy previousSpawn in _previousSpawns)
            {
                if (previousSpawn == spawnPrefab)
                    return true;
            }

            return false;
        }

        public int SpawnCount() => _previousSpawns.Count();
    }
}