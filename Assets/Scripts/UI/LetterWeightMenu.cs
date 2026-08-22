using System.Collections.Generic;
using UnityEngine;

public class LetterWeightMenu : MonoBehaviour
{
    [SerializeField]
    private LetterWeightRow _letterWeightRowPrefab;
    [SerializeField]
    private GameObject _letterWeightParent;

    private List<LetterWeightRow> _letterWeightRows = new List<LetterWeightRow>();

    private void OnEnable()
    {
        if (_letterWeightRows.Count > 26)
        {
            _letterWeightRows.RemoveRange(26, _letterWeightRows.Count - 26);
        }
        while (_letterWeightRows.Count < 26)
        {
            _letterWeightRows.Add(Instantiate(_letterWeightRowPrefab, _letterWeightParent.transform));
        }
    }
}
