using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool _analyticsEnabled;
    
    void Start()
    {
        _analyticsEnabled = false;
        DontDestroyOnLoad(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void AnalyticsOptIn()
    {
        _analyticsEnabled = true;
    }

    public void AnalyticsOptOut()
    {
        _analyticsEnabled = false;
    }
    
}
