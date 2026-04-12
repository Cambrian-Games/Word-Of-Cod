using UnityEngine;

public class InstantiateWordDisplay : MonoBehaviour
{
    public GameObject _location;

    public GameObject _prefab;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateNewWordDisplay()
    {
        (Instantiate (_prefab, new Vector3(0f,0f,0f), Quaternion.identity, _location.transform)).transform.localScale = new Vector3(1f,1f,1f);
    }
}
