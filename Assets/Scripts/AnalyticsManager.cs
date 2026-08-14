using Unity.Services.Core;
using UnityEngine;

public class AnalyticsManager : MonoBehaviour
{
    public bool _analyticsEnabled;

    public static AnalyticsManager INSTANCE;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Debug.LogError("Attempted to create second player!");
            Destroy(gameObject);
            return;
        }
        
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
    //can't rename in dashboard, so this one is capitalized
    public ShuffleEvent() : base("Shuffle")
    {
        
    }
    
    public int _enemyIndex { set { SetParameter("enemyIndex" , value);}}
    public string _enemyName { set { SetParameter("enemyName" , value);}}

}

public class WinEvent : Unity.Services.Analytics.Event
{
    public WinEvent() : base("win")
    {
        
    }
    
    public string _longestWord { set { SetParameter("longestWord" , value);}}
    public string _mostDamagingWord { set { SetParameter("mostDamagingWord" , value);}}
    public int _highestDamage { set { SetParameter("highestDamage" , value);}}
    public float _meanDamage { set { SetParameter("meanDamage" , value);}}
    public float _medianDamage { set { SetParameter("medianDamage" , value);}}
    public float _meanLength { set { SetParameter("meanLength" , value);}}
    public float _medianLength { set { SetParameter("medianLength" , value);}}
    public string _relicList { set { SetParameter("relicList" , value);}}
    public int _numWords { set { SetParameter("numWords" , value);}}
    
}

public class LoseEvent : Unity.Services.Analytics.Event
{
    public LoseEvent() : base("lose")
    {
        
    }
    
    public string _longestWord { set { SetParameter("longestWord" , value);}}
    public string _mostDamagingWord { set { SetParameter("mostDamagingWord" , value);}}
    public int _highestDamage { set { SetParameter("highestDamage" , value);}}
    public float _meanDamage { set { SetParameter("meanDamage" , value);}}
    public float _medianDamage { set { SetParameter("medianDamage" , value);}}
    public float _meanLength { set { SetParameter("meanLength" , value);}}
    public float _medianLength { set { SetParameter("medianLength" , value);}}
    public string _relicList { set { SetParameter("relicList" , value);}}
    public int _enemyIndex { set { SetParameter("enemyIndex" , value);}}
    public string _enemyName { set { SetParameter("enemyName" , value);}}
    public int _numWords { set { SetParameter("numWords" , value);}}

}