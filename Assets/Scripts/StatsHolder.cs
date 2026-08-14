using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StatsHolder : MonoBehaviour
{
    public static StatsHolder INSTANCE;
    
    [Header("Analytics")]
    public string _longestWord = "";
    public string _mostDamagingWord = "";
    public List<int> _sortedWordLengths;
    public List<int> _sortedWordDamages;
    public float _medianWordLength;
    public float _meanWordLength;
    public float _medianWordDamage;
    public float _meanWordDamage;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Debug.LogError("Attempted to create second player!");
            Destroy(gameObject);
            return;
        }

        INSTANCE = this;
        
        DontDestroyOnLoad(gameObject);
        _sortedWordDamages = new List<int>();
        _sortedWordLengths = new List<int>();
    }
    
}
