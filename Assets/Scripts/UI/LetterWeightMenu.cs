using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LetterWeightMenu : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    private LetterWeightRow _letterWeightRowPrefab;

    [SerializeField]
    private GameObject _letterWeightParent;
#endif

    [SerializeField]
    private List<LetterWeightRow> _letterWeightRows = new List<LetterWeightRow>();

    public CharacterSet _charset;

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (_letterWeightRows.Count > 26)
        {
            _letterWeightRows.RemoveRange(26, _letterWeightRows.Count - 26);
        }
        while (_letterWeightRows.Count < 26)
        {
            _letterWeightRows.Add(Instantiate(_letterWeightRowPrefab, _letterWeightParent.transform));
        }
        
        for (int i = 0; i < _letterWeightRows.Count; i++)
        {
            _letterWeightRows[i].BaseWeightText.text = $"{BoardConfig.INSTANCE.Weights._weights[i]}";
        }
#endif
    }

#if UNITY_EDITOR
    [ContextMenu("Fix")]
    public void FixNamesAndPositions()
    {
        for (int i = 0; i < _letterWeightRows.Count; i++)
        {
            _letterWeightRows[i].name = "Letter Weight (" + (char)('A' + i) + ")";
            _letterWeightRows[i].transform.localPosition = new Vector3(_letterWeightRows[i].transform.localPosition.x, -10 - (20 * i), _letterWeightRows[i].transform.localPosition.z);
            _letterWeightRows[i].LetterIcon.sprite = _charset._letterSprites[i];
            _letterWeightRows[i].Letter = (char)('A' + i);
            _letterWeightRows[i].Parent = this;
            UnityEditor.EditorUtility.SetDirty(_letterWeightRows[i].LetterIcon);
            UnityEditor.EditorUtility.SetDirty(_letterWeightRows[i]);
        }
    }
#endif
    public void OnClickPlus(LetterWeightRow row)
    {
        Debug.Log(row.Parent);
        //throw new NotImplementedException();
    }

    public void OnClickMinus(LetterWeightRow row)
    {
        //throw new NotImplementedException();
    }
}
