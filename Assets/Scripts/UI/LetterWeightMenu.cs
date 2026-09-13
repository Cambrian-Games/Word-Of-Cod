using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterWeightMenu : MonoBehaviour
{
#if UNITY_EDITOR
    [SerializeField]
    private LetterWeightRow _letterWeightRowPrefab;

    [SerializeField]
    private GameObject _letterWeightParent;
#endif

    [Header("Temporary")]
    public int _maxPlayerTweaks;
    private bool _atMaxPlayerTweaks;
    private bool _atMaxPlayerTweaksPrev;

    private LetterTweakSet _tweaks;

    [SerializeField]
    private List<LetterWeightRow> _letterWeightRows = new List<LetterWeightRow>();
    [SerializeField]
    private TMP_Text _remainingTweakText;

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
#endif

        _tweaks = RunManager.INSTANCE._letterTweakset.Clone();

        UpdateRowDisplaysAndButtonStates(updateText: true, forceButtonStateUpdate: true);
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
        _tweaks.ChangePlayerTweak(row.Letter, 1);
        UpdatePlayerTweakTextSingle(row);
        UpdateRowDisplaysAndButtonStates();
    }

    public void OnClickMinus(LetterWeightRow row)
    {
        _tweaks.ChangePlayerTweak(row.Letter, -1);
        UpdatePlayerTweakTextSingle(row);
        UpdateRowDisplaysAndButtonStates();
    }

    private void UpdatePlayerTweakTextSingle(LetterWeightRow row)
    {
        int index = row.Letter - 'A';
        row.PlayerTweakText.text = $"{_tweaks.PlayerTweakCounts[index]}";
        float finalWeight = BoardConfig.INSTANCE.Weights._weights[index] + _tweaks.WeightTweaks[index];
        row.FinalWeightText.text = $"{finalWeight.ToString("0.00")}";
    }

    private void UpdateRowDisplaysAndButtonStates(bool updateText = false, bool forceButtonStateUpdate = false)
    {
        ReadOnlyCollection<int> playerTweakCounts = _tweaks.PlayerTweakCounts;
        Debug.Assert(_tweaks.TotalPlayerTweakCount <= _maxPlayerTweaks);

        _atMaxPlayerTweaksPrev = _atMaxPlayerTweaks;
        _atMaxPlayerTweaks = _tweaks.TotalPlayerTweakCount == _maxPlayerTweaks;

        bool updateButtonState = forceButtonStateUpdate || (_atMaxPlayerTweaksPrev != _atMaxPlayerTweaks);

        _remainingTweakText.text = $"{_maxPlayerTweaks - _tweaks.TotalPlayerTweakCount}";
        if (!updateText && !updateButtonState)
            return;

        for (int i = 0; i < _letterWeightRows.Count; i++)
        {
            if (updateText)
            {
                _letterWeightRows[i].BaseWeightText.text = $"{BoardConfig.INSTANCE.Weights._weights[i].ToString("0.00")}";
                UpdatePlayerTweakTextSingle(_letterWeightRows[i]);
            }

            if (updateButtonState)
            {
                _letterWeightRows[i].MinusButton.interactable = !_atMaxPlayerTweaks || playerTweakCounts[i] > 0;
                _letterWeightRows[i].PlusButton.interactable = !_atMaxPlayerTweaks || playerTweakCounts[i] < 0;
            }
        }
    }

    public void ClearPlayerTweaks()
    {
        _tweaks.ClearPlayerTweaks();
        UpdateRowDisplaysAndButtonStates(updateText: true, forceButtonStateUpdate: true);
    }

    public void RevertPlayerTweaks()
    {
        _tweaks = RunManager.INSTANCE._letterTweakset.Clone();
        UpdateRowDisplaysAndButtonStates(updateText: true, forceButtonStateUpdate: true);
    }

    public void SavePlayerTweaks()
    {
        RunManager.INSTANCE._letterTweakset = _tweaks.Clone();
    }
}
