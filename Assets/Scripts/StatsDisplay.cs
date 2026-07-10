using System.Linq;
using UnityEngine;
using TMPro;
 
public class StatsDisplay : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _longestWordText;
    [SerializeField]
    private TMP_Text _mostDamageText;
    [SerializeField]
    private TMP_Text _mostDamageNumText;
    [SerializeField]
    private TMP_Text _meanLengthText;
    [SerializeField]
    private TMP_Text _medianLengthText;
    [SerializeField]
    private TMP_Text _meanDamageText;
    [SerializeField]
    private TMP_Text _medianDamageText;

    private StatsHolder _statsHolder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _statsHolder = GameObject.Find("StatsHolder").GetComponent<StatsHolder>();
        _longestWordText.text = "Longest Word: " + _statsHolder._longestWord;
        _mostDamageText.text = "Most Damaging Word: " + _statsHolder._mostDamagingWord;
        _mostDamageNumText.text = "Highest Damage: " + _statsHolder._sortedWordDamages.Last();
        _meanLengthText.text = "Mean Length: " + _statsHolder._meanWordLength;
        _medianLengthText.text = "Median Length: " + _statsHolder._medianWordLength;
        _meanDamageText.text = "Mean Damage: " + _statsHolder._meanWordDamage;
        _medianDamageText.text = "Median Damage: " + _statsHolder._medianWordDamage;

    }

    // Update is called once per frame
    void Update()
    {
    }
}
