using System.Collections.Generic;
using UnityEngine;

public class LetterWeightMenu : MonoBehaviour
{
    [SerializeField]
    private LetterWeightRow _letterWeightRowPrefab;
    [SerializeField]
    private GameObject _letterWeightParent;

    [SerializeField]
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

    [ContextMenu("Fix")]
    public void FixNamesAndPositions()
    {
        for (int i = 0; i < _letterWeightRows.Count; i++)
        {
            _letterWeightRows[i].name = "Letter Weight (" + (char)('A' + i) + ")";
            _letterWeightRows[i].transform.localPosition = new Vector3(_letterWeightRows[i].transform.localPosition.x, -10 - (20 * i), _letterWeightRows[i].transform.localPosition.z);
        }
    }
}
