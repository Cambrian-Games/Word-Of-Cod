using UnityEngine;

public class OverworldManager : MonoBehaviour
{
	public GameObject _overworldBasePrefab;

	public Vector3 _spacing;
	public Vector3 _baseSpawnPoint;

	public Vector3 _eulerBaseAngle;
	[Min(0)]
	public float _maxRandomTilt;

	private GameObject _lastSpawned;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < RunManager.INSTANCE.RunFormat.Count; i++)
		{
			SpawnNext();
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	private void SpawnNext()
	{
		if (!_lastSpawned)
		{
			_lastSpawned = Instantiate(_overworldBasePrefab, _baseSpawnPoint, Quaternion.Euler(_eulerBaseAngle.x, _eulerBaseAngle.y, _eulerBaseAngle.z), this.transform);
		}
		else
		{
			_lastSpawned = Instantiate(_overworldBasePrefab, _lastSpawned.transform.position + _spacing, Quaternion.Euler(_eulerBaseAngle.x, _eulerBaseAngle.y, _eulerBaseAngle.z), this.transform);
		}
	}
}
