using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class StatsHolder : MonoBehaviour
{
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
        DontDestroyOnLoad(gameObject);
        _sortedWordDamages = new List<int>();
        _sortedWordLengths = new List<int>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
