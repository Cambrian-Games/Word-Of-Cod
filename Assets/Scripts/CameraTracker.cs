using UnityEngine;

public class CameraTracker : MonoBehaviour
{
	public GameObject target;
	public Vector3 targetOffset;
	public float cameraOffset;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		this.transform.position = target.transform.position - targetOffset + Vector3.forward * cameraOffset;
    }
}
