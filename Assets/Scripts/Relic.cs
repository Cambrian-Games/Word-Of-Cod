using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Relic : MonoBehaviour
{
    private int _id = -1; // will be assigned by the inventory manager

    public int ID => _id;

    [SerializeField]
    private string _displayName;

    [SerializeField]
    private Sprite _icon;

    [SerializeField]
    private List<RelicEffect> _effects;

    internal List<RelicEffect> Effects => new List<RelicEffect>(_effects);

    public void SetID(int i)
    {
        if (_id != -1 && _id != i)
        {
            Debug.LogWarning($"Overwriting ID {_id} with {i}");
        }

        _id = i;
    }

    internal RelicEffect.Result OnWordSubmit(Word word)
    {
        RelicEffect.Result res = new RelicEffect.Result();

        foreach (RelicEffect effect in _effects)
        {
            if (effect.Event != RelicEffect.EventTiming.On_Word_Submit)
                continue;

            res += effect.OnWordSubmit(word);
        }

        return res;
    }

    internal RelicEffect.Result OnEnemyAttack(int baseDamage)
    {
        RelicEffect.Result res = new RelicEffect.Result();

        foreach (RelicEffect effect in _effects)
        {
            if (effect.Event != RelicEffect.EventTiming.On_Enemy_Attack)
                continue;

            res += effect.OnEnemyAttack(baseDamage);
        }

        return res;
    }
}

[Serializable]
public class RelicEffect
{
    public enum EventTiming
    {
        On_Word_Submit,         // This is where damage modifiers should be applied
        On_Deal_Damage,         // Happens immediately after damage is dealt
        On_Enemy_Attack,        // This is where resist modifiers should be applied
        On_Receive_Damage,      // Happens immediately after damage is received
        On_Heal,
        Tile_Fall,              // Spawn weight modifications
        After_Tile_Fall,
        On_Use_Item,            // Modifications to item effects, chance to not consume, etc
        On_Tile_Change,         // When a tile changes TileKind
    }

    public enum RelicCondition
    {
        Always_Active,
        Contains_All_Letters,       // true if all of the letters in _filterString are present
        Contains_Any_Letters,       // true if any of the letters in _filterString are present
        Contains_Unique_Letters,    // true if any of the letters in _filterString are present,
                                    //		each letter can only be counted once. Empty string = all characters
        Contains_Sequence,          // true if _filterString appears as a full substring
        Does_Not_Contain_Letter,    // true if no letters in _filterString are present

        Contains_All_POS,           // true if all of the FPARTs in _partsOfSpeech are present
        Contains_Any_POS,           // true if any of the FPARTs in _partsOfSpeech are present
        Contains_No_POS,            // true if none of the FPARTs in _partsOfSpeech are present

        Double_Letter,
        Middle_Letter,              // true if the word has any of the letters in _filterString as its central letter (requires odd number of letters)
        Palindrome,

        Alphabetical_Chain,         // finds the longest chain of alphabetical letters
        Fully_Alphabetized_Word,    // true if word is fully alphabetized

        Rev_Alphabetical_Chain,         // finds the longest chain of reverse-alphabetical letters
        Fully_Rev_Alphabetized_Word,    // true if word is fully reverse-alphabetized
    }

    public enum ValueToModify
    {
        // outgoing damage boost. Formula is (outgoing * (1 + damageMult)) + damageBonus

        Damage_Percent_Increase,
        Damage_Bonus,

        // global damage reduction. Formula is (incoming * (1 - resistMult)) - resistBonus

        Resist_Percent_Increase,
        Resist_Bonus,

        Enemy_Damage_Resist_Percent_Increase,
        Enemy_Damage_Resist_Bonus,

        // self damage reduction

        Self_Damage_Resist_Percent_Increase,
        Self_Damage_Resist_Bonus,

        Self_Heal,
    }

    [SerializeField]
    private EventTiming _event;
    internal EventTiming Event => _event;

    [SerializeField]
    private RelicCondition _condition;

    [SerializeField]
    private string _filterString;

    [SerializeField]
    private FPART _filterPOS;

    // cap on how many times the effect can trigger at once
    [SerializeField]
    private int _numTimesToApply = 0;

    // if condition is met, chance to trigger
    [SerializeField]
    private float _chanceToTrigger = 1;

    [SerializeField]
    private ValueToModify _valueToModify;

    [SerializeField]
    private float _value;

    internal class Result
    {
        public Dictionary<ValueToModify, float> _values = new Dictionary<ValueToModify, float>();

        public static Result operator +(Result lhs, Result rhs)
        {
            Result resNew = new Result();

            foreach (ValueToModify val in Enum.GetValues(typeof(ValueToModify)))
            {
                float summedVal = lhs._values.GetValueOrDefault(val) + rhs._values.GetValueOrDefault(val);

                if (summedVal != 0)
                {
                    resNew._values[val] = summedVal;
                }
            }

            return resNew;
        }
    }

    internal Result OnWordSubmit(Word word)
    {
        int numPasses = CountWordSubmitConditionPasses(word, _condition);

        if (numPasses == 0)
            return new Result();

        if (_numTimesToApply > 0)
        {
            numPasses = Math.Min(numPasses, _numTimesToApply);
        }

        Result res = new Result();

        // this is a bit sketchy. Do we want multiple rolls to apply the thing once, or should it apply multiple times?
        // Should this be a setting?

        for (int i = 0; i < numPasses; i++)
        {
            if (UnityEngine.Random.Range(0, 1) > _chanceToTrigger)
            {
                continue;
            }

            float newValue = res._values.GetValueOrDefault(_valueToModify) + _value;

            if (newValue != 0)
            {
                res._values[_valueToModify] = newValue;
            }
            else
            {
                res._values.Remove(_valueToModify);
            }
            
        }

        return res;
    }

    internal Result OnEnemyAttack(int baseDamage)
    {
        int numPasses = CountEnemyAttackConditionPasses(baseDamage, _condition);

        if (numPasses == 0)
            return new Result();

        if (_numTimesToApply > 0)
        {
            numPasses = Math.Min(numPasses, _numTimesToApply);
        }

        Result res = new Result();

        for (int i = 0; i < numPasses; i++)
        {
            if (UnityEngine.Random.Range(0, 1) > _chanceToTrigger)
            {
                continue;
            }

            float newValue = res._values.GetValueOrDefault(_valueToModify) + _value;

            if (newValue != 0)
            {
                res._values[_valueToModify] = _value;
            }
            else
            {
                res._values.Remove(_valueToModify);
            }
        }

        return res;
    }

    private int CountWordSubmitConditionPasses(Word word, RelicCondition condition)
    {
        int numPasses = 0;

        string text = word.Text;

        switch (condition)
        {
            case RelicCondition.Always_Active:
                numPasses = 1;
                break;
            case RelicCondition.Contains_All_Letters:
                if (_filterString.Length == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    numPasses = int.MaxValue;
                    foreach (char filterChar in _filterString)
                    {
                        // check how many times each character is contained in the word, and take the lowest value
                        numPasses = Mathf.Min(numPasses, text.Count(letter => letter == filterChar));
                    }
                }
                break;
            case RelicCondition.Contains_Any_Letters:
                if (_filterString.Length == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    foreach (char filterChar in _filterString)
                    {
                        // check how many times each character is contained in the word
                        numPasses += text.Count(letter => letter == filterChar);
                    }
                }
                break;
            case RelicCondition.Contains_Unique_Letters:
                if (_filterString.Length == 0)
                {
                    for (char c = 'A'; c <= 'Z'; c++)
                    {
                        numPasses += text.Contains(c) ? 1 : 0;
                    }
                }
                else
                {
                    // check if each character is contained in the word
                    numPasses = _filterString.Count(filterChar => text.Contains(filterChar));
                }
                break;
            case RelicCondition.Contains_Sequence:
                if (_filterString.Length == 0)
                {
                    numPasses = 1;
                }
                else if (text.Length < _filterString.Length)
                {
                    numPasses = 0;
                }
                else
                {
                    int matchIndex = text.IndexOf(_filterString);

                    while (matchIndex != -1)
                    {
                        numPasses++;
                        text = text.Substring(matchIndex + _filterString.Length);
                        matchIndex = text.IndexOf(_filterString);
                    }
                }
                break;
            case RelicCondition.Does_Not_Contain_Letter:
                if (_filterString.Length == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    numPasses = !_filterString.Any(filterChar => text.Contains(filterChar)) ? 1 : 0;
                }
                break;
            case RelicCondition.Contains_All_POS:
                if (_filterPOS == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    numPasses = (word.PartsOfSpeech & _filterPOS) == _filterPOS ? 1 : 0;
                }
                break;
            case RelicCondition.Contains_Any_POS:
                if (_filterPOS == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    FPART sharedFlags = (word.PartsOfSpeech & _filterPOS);

                    foreach (FPART pOS in Enum.GetValues(typeof(FPART)))
                    {
                        numPasses += sharedFlags.HasFlag(pOS) ? 1 : 0;
                    }
                }
                break;
            case RelicCondition.Contains_No_POS:
                if (_filterPOS == 0)
                {
                    numPasses = 1;
                }
                else
                {
                    numPasses = (word.PartsOfSpeech & _filterPOS) == 0 ? 1 : 0;
                }
                break;
            case RelicCondition.Double_Letter:
                if (text.Length < 2)
                {
                    numPasses = 0;
                }
                else
                {
                    for (int i = 0; i < text.Length - 1; i++)
                    {
                        if (text[i] == text[i + 1])
                        {
                            numPasses++;
                            i++; //triple letters, if they exist, don't double count.
                        }
                    }
                }
                break;
            case RelicCondition.Middle_Letter:
                if (text.Length % 2 == 0)
                {
                    numPasses = 0;
                }
                else
                {
                    numPasses = _filterString.Contains(text[text.Length / 2]) ? 1 : 0;
                }
                break;
            case RelicCondition.Palindrome:
                numPasses = 1;

                for (int i = 0; i < text.Length / 2; i++) // we can ignore the middle character if this is odd
                {
                    if (text[i] != text[^(i + 1)])
                    {
                        numPasses = 0;
                        break;
                    }
                }
                break;
            case RelicCondition.Alphabetical_Chain:

                if (text.Length <= 2)
                {
                    numPasses = 0;
                    break;
                }

                int currentChain = 0;
                char lastChar = '\0';

                for (int i = 0; i < text.Length; i++)
                {// see if SO knows a faster way?
                    if (currentChain == 0)
                    {
                        currentChain = 1;
                    }
                    else
                    {
                        if (text[i] >= lastChar)
                        {
                            currentChain++;

                            if (currentChain > numPasses)
                            {
                                numPasses = currentChain;
                            }
                        }
                        else
                        {
                            currentChain = 0;
                        }
                    }

                    lastChar = text[i];
                }

                // don't allow chains less than 3

                if (numPasses <= 2)
                {
                    numPasses = 0;
                }

                break;
            case RelicCondition.Fully_Alphabetized_Word:
                int longestChain = CountWordSubmitConditionPasses(word, RelicCondition.Alphabetical_Chain);
                numPasses = longestChain == text.Length ? longestChain : 0;
                break;
            case RelicCondition.Rev_Alphabetical_Chain:
                if (text.Length <= 2)
                {
                    numPasses = 0;
                    break;
                }

                int currentChainRev = 0;
                char lastCharRev = '\0';

                for (int i = 0; i < text.Length; i++)
                {
                    if (currentChainRev == 0)
                    {
                        currentChainRev = 1;
                    }
                    else
                    {
                        if (text[i] <= lastCharRev)
                        {
                            currentChainRev++;

                            if (currentChainRev > numPasses)
                            {
                                numPasses = currentChainRev;
                            }
                        }
                        else
                        {
                            currentChain = 0;
                        }
                    }

                    lastChar = text[i];
                }

                // don't allow chains less than 3

                if (numPasses <= 2)
                {
                    numPasses = 0;
                }
                break;
            case RelicCondition.Fully_Rev_Alphabetized_Word:
                int longestChainRev = CountWordSubmitConditionPasses(word, RelicCondition.Rev_Alphabetical_Chain);
                numPasses = longestChainRev == text.Length ? longestChainRev : 0;
                break;

            default:
                Debug.LogError($"Unsupported Condition {condition}");
                break;
        }

        return numPasses;
    }

    private int CountEnemyAttackConditionPasses(int baseDamage, RelicCondition condition)
    {
        int numPasses = 0;

        switch (condition)
        {
            case RelicCondition.Always_Active:
                numPasses = 1;
                break;

            default:
                Debug.LogError($"Unsupported Condition {condition}");
                break;
        }

        return numPasses;
    }
}