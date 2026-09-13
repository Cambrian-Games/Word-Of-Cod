using System;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

public class LetterTweakSet
{
    private float _permaTweakStep;
    private float _playerTweakStep;

    private int _totalPlayerTweakCount = 0;
    public int TotalPlayerTweakCount => _totalPlayerTweakCount;

    private readonly int[] _permaTweakCounts = new int[26];
    public readonly ReadOnlyCollection<int> PermaTweakCounts;

    private readonly int[] _playerTweakCounts = new int[26];
    public ReadOnlyCollection<int> PlayerTweakCounts;

    private readonly float[] _weightTweaks = new float[26];
    public ReadOnlyCollection<float> WeightTweaks;

    public LetterTweakSet(float permaTweakStep, float playerTweakStep)
    {
        _permaTweakStep = permaTweakStep;
        _playerTweakStep = playerTweakStep;

        // creates a read-only interface for _permaTweakCounts
        PermaTweakCounts = new ReadOnlyCollection<int>(_permaTweakCounts);
        PlayerTweakCounts = new ReadOnlyCollection<int>(_playerTweakCounts);
        WeightTweaks = new ReadOnlyCollection<float>(_weightTweaks);
    }

    public void ChangePermaTweak(char capitalChar, int delta)
    {
        SetPermaTweak(capitalChar, _permaTweakCounts[capitalChar - 'A'] + delta);
    }

    public void SetPermaTweak(char capitalChar, int newValue)
    {
        _permaTweakCounts[capitalChar - 'A'] = newValue;
        RecalculateTweak(capitalChar);
    }

    public void ChangePlayerTweak(char capitalChar, int delta)
    {
        SetPlayerTweak(capitalChar, _playerTweakCounts[capitalChar - 'A'] + delta);
    }

    public void SetPlayerTweak(char capitalChar, int newValue)
    {
        int oldValue = _playerTweakCounts[capitalChar - 'A'];
        _playerTweakCounts[capitalChar - 'A'] = newValue;

        int absOldValue = Mathf.Abs(oldValue);
        int absNewValue = Mathf.Abs(newValue);
        
        // this works regardless of the signs of the values.
        //  2 ->  3: Increase positive tweak by 1
        //  2 -> -3: Decrease positive tweak by 2 then increase negative tweak by 3. Net increase of 1.
        // -2 -> -3: Increase negative tweak by 1
        // -2 ->  3: Decrease negative tweak by 2 then increase positive tweak by 3. Net increase of 1.

        _totalPlayerTweakCount += absNewValue - absOldValue;

        RecalculateTweak(capitalChar);
    }

    private void RecalculateTweak(char capitalChar)
    {
        _weightTweaks[capitalChar - 'A'] =
            (_permaTweakStep * _permaTweakCounts[capitalChar - 'A']) +
            (_playerTweakStep * _playerTweakCounts[capitalChar - 'A']);
    }

    // bulk operations

    public bool SetPermaTweaks(int[] newCounts)
    {
        if (newCounts.Length != 26)
            return false;

        Array.Copy(newCounts, _permaTweakCounts, 26);
        RecalculateTweaks();
        return true;
    }

    public bool SetPlayerTweaks(int[] newCounts)
    {
        if (newCounts.Length != 26)
            return false;

        Array.Copy(newCounts, _playerTweakCounts, 26);
        RecalculateTweaks();
        return true;
    }

    public bool SetAllTweaks(int[] newPermaTweaks, int[] newPlayerTweaks)
    {
        if (newPermaTweaks.Length != 26 || newPlayerTweaks.Length != 26)
            return false;

        Array.Copy(newPermaTweaks, _permaTweakCounts, 26);
        Array.Copy(newPlayerTweaks, _playerTweakCounts, 26);
        RecalculateTweaks();

        return true;
    }

    private void RecalculateTweaks()
    {
        for (char c = 'A'; c <= 'Z'; c++)
        {
            RecalculateTweak(c);
        }
    }

    public LetterTweakSet Clone()
    {
        LetterTweakSet newSet = new LetterTweakSet(_permaTweakStep, _playerTweakStep);
        newSet.SetAllTweaks(_permaTweakCounts, _playerTweakCounts);
        newSet._totalPlayerTweakCount = _totalPlayerTweakCount;
        return newSet;
    }

    public void ClearPlayerTweaks()
    {
        Array.Clear(_playerTweakCounts, 0, 26);
        _totalPlayerTweakCount = 0;
        RecalculateTweaks();
    }
}