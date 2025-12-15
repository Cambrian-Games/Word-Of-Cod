using UnityEngine;

public class CameraTracker : MonoBehaviour
{
	public GameObject _target;
	public Vector3 _targetOffset;
	public float _cameraOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		// snap position for now. This will become more elaborate later.
		this.transform.position = _target.transform.position - _targetOffset + Vector3.forward * _cameraOffset;
    }
}
