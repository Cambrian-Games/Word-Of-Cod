using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Relic : MonoBehaviour, IDisplayInfo
{
    private int _id = -1; // will be assigned by the inventory manager

	public int ID { get => _id; set => SetID(value); }

	[SerializeField]
    private string _displayName;
    public string DisplayName => _displayName;

    [SerializeField]
    private string _description;
    public string Description => _description;

    [SerializeField]
    private Sprite _icon;
    public Sprite Icon => _icon;

    [Flags]
    public enum FPASSIVEPOOL
    {
        OTHER           = 0x0,
        LETTER          = 0x1,
        PART_OF_SPEECH  = 0x2
    }

    [SerializeField]
    private FPASSIVEPOOL _passivePools;
    public FPASSIVEPOOL PassivePools => _passivePools;

    [SerializeField]
    private List<RelicEffect> _effects;

    internal List<RelicEffect> Effects => new List<RelicEffect>(_effects);

    private void SetID(int i)
    {
        if (_id != -1 && _id != i)
        {
            Debug.LogWarning($"Overwriting ID {_id} with {i}");
        }

        _id = i;
    }

    public InventoryManager.InventoryReference AsInventoryReference()
    {
        return new InventoryManager.InventoryReference(InventoryManager.InventorySection.Passive_Relic, _id);
    }

    internal RelicEffect.Result OnWordSubmit(Word word)
    {
        RelicEffect.Result res = new RelicEffect.Result();

        foreach (RelicEffect effect in _effects)
        {
            if (effect.Event != RelicEffect.EventTiming.On_Word_Submit)
                continue;

            res += effect.OnWordSubmit(word, _id);
        }

        return res;
    }

    internal RelicEffect.Result OnEnemyAttack(float baseDamage)
    {
        RelicEffect.Result res = new RelicEffect.Result();

        foreach (RelicEffect effect in _effects)
        {
            if (effect.Event != RelicEffect.EventTiming.On_Enemy_Attack)
                continue;

            res += effect.OnEnemyAttack(baseDamage, _id);
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
        S_Plural,                   // true if the word ends in S or ES, and removing those results in a valid word
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
        
        //Bubble Shield DR
        
        Bubble,
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
		public HashSet<int> _passiveRelicIDs = new HashSet<int>();
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

			resNew._passiveRelicIDs.UnionWith(lhs._passiveRelicIDs);
			resNew._passiveRelicIDs.UnionWith(rhs._passiveRelicIDs);

            return resNew;
        }
    }

    internal Result OnWordSubmit(Word word, int sourceRelicID)
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
            if (UnityEngine.Random.Range(0.0f, 1.0f) > _chanceToTrigger)
            {
                continue;
            }

            float newValue;
            if (_valueToModify == ValueToModify.Bubble && _value == -1.0f)
            {
                newValue = word.Text.Length * 2.0f;
            }
            else
            {
                newValue = res._values.GetValueOrDefault(_valueToModify) + _value;
            }

            if (newValue != 0)
            {
                res._values[_valueToModify] = newValue;
            }
        
            else
            {
                res._values.Remove(_valueToModify);
            }

			res._passiveRelicIDs.Add(sourceRelicID);
		}

        return res;
    }

    internal Result OnEnemyAttack(float baseDamage, int sourceRelicID)
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

			res._passiveRelicIDs.Add(sourceRelicID);
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
                    // Switch this to hashset and iterate over text

                    HashSet<char> letters = new HashSet<char>();

                    foreach (char c in text)
                    {
                        letters.Add(c);
                    }

                    numPasses = letters.Count;
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

                int currentChain = 1;
                char lastChar = '\0';

                // does stack overflow have a cleaner implementation of this?
                // It's O(n) so we won't have a faster one but cleaner might be possible

                for (int i = 0; i < text.Length; i++)
                {	// see if SO knows a faster way?
					if (lastChar != '\0')
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
							// reset, this is now the first letter in the chain
                            currentChain = 1;
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

                int currentChainRev = 1;
                char lastCharRev = '\0';

                for (int i = 0; i < text.Length; i++)
                {
					if (lastCharRev != '\0')
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
							// reset, this is now the first letter in the chain
							currentChain = 1;
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
            case RelicCondition.S_Plural:

                // Only consider nouns and pronouns
                if ((word.PartsOfSpeech & (FPART.NOUN | FPART.PRONOUN)) == 0)
                {
                    numPasses = 0;
                    break;
                }

                // if the word length is 1, doesn't end in S, or ends in SS, ignore
                if (text.Length < 2 || text[^1] != 'S' || text[^2] == 'S')
                {
                    numPasses = 0;
                    break;
                }
                else
                {
                    FPART depluralS = FPART.NONE;
                    FPART depluralES = FPART.NONE;

                    bool pluralS = WordChecker.INSTANCE._allWords._dict.TryGetValue(text[..^1].ToLower(), out depluralS);
                    pluralS &= (depluralS & (FPART.NOUN | FPART.PRONOUN)) != 0;
                    bool pluralES = text.Length >= 3 & text[^2] == 'E' && WordChecker.INSTANCE._allWords._dict.TryGetValue(text[..^2].ToLower(), out depluralES);
                    pluralES &= (depluralES & (FPART.NOUN | FPART.PRONOUN)) != 0;

                    if (pluralS || pluralES)
                    {
                        numPasses = 1;
                    }
                    break;
                }
            default:
                Debug.LogError($"Unsupported Condition {condition}");
                break;
        }

        return numPasses;
    }

    private int CountEnemyAttackConditionPasses(float baseDamage, RelicCondition condition)
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