using Unity.Services.Core;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public bool _analyticsEnabled;
    
    void Start()
    {
        _analyticsEnabled = false;
        DontDestroyOnLoad(gameObject);
        UnityServices.InitializeAsync();
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

public class WordFailedEvent : Unity.Services.Analytics.Event
{
    public WordFailedEvent() : base("failToSubmit")
    {

    }

    public string _failedWord { set { SetParameter("failedWord" , value);}}

}

public class ShuffleEvent : Unity.Services.Analytics.Event
{
    public ShuffleEvent() : base("shuffle")
    {
        
    }
    
    public int _enemyIndex { set { SetParameter("enemyIndex" , value);}}
    public string _enemyName { set { SetParameter("enemyName" , value);}}

}