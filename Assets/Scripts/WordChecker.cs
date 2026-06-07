#if UNITY_EDITOR
//#define DICTIONARY_TESTING // comment out to turn off this logging
//#define PLURAL_TESTING
#endif

using odin.serialize.OdinSerializer;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


public class WordChecker : MonoBehaviour
{
    //the dictionary
    public SerializedDict _allWords;

#if UNITY_EDITOR && (DICTIONARY_TESTING || PLURAL_TESTING)
	private bool _hasRunTest = false;
#endif

#if UNITY_EDITOR && PLURAL_TESTING
    private Dictionary<string, (string, string)> _plurals = new Dictionary<string, (string, string)>();
#endif

    public static WordChecker INSTANCE;

    private void Awake()
    {
        // set up singleton

        if (INSTANCE != null && INSTANCE != this)
        {
            Destroy(gameObject);
            return;
        }

        INSTANCE = this;
    }

    void Start()
    {
        _allWords = ScriptableObject.CreateInstance<SerializedDict>();
        byte[] dictbytes = File.ReadAllBytes(Application.streamingAssetsPath + "/odinDict");
        _allWords._dict = SerializationUtility.DeserializeValue<Dictionary<string, FPART>>(dictbytes, DataFormat.Binary);
		if (!_allWords || _allWords._dict == null)
		{
			Debug.LogError("Dictionary could not be found, word checking will default to true.");
		}

	}

#if UNITY_EDITOR && DICTIONARY_TESTING
    //checks if the word is in the dict, if yes returns true, if no return false
    public bool CheckWord(string word, out FPART pOS)
    {
        //returns true if the word is in the dictionary, and puts the parts of speech in pOS
        //otherwise returns false

		if (!_allWords || _allWords._dict == null)
		{
			pOS = FPART.NONE;

			Debug.Log($"Word list not found. Treating {word} as a word.");
			return true;
		}

		if (word.Length == 1)
		{
			pOS = FPART.NONE;
			string wordTest = word.ToLower();
			// hard coding this for now

			Debug.Log($"Word {word} has length 1");
			return wordTest[0] == 'a' || wordTest[0] == 'i' || wordTest[0] == 'o';
		}

		bool result = _allWords._dict.TryGetValue(word.ToLower(), out pOS);
		Debug.Log($"{word} is {(result ? "" : " not ")} a word");
		return result;
	}
#endif

#if UNITY_EDITOR && PLURAL_TESTING
    // checks if the word is a valid plural
    public bool CheckPlural(string text, out string minusS, out string minusES)
    {
        FPART partsOfSpeech = _allWords._dict.GetValueOrDefault(text);

        // Only consider nouns and pronouns
        if ((partsOfSpeech & (FPART.NOUN | FPART.PRONOUN)) == 0)
        {
            minusS = null;
            minusES = null;
            return false;
        }

        // if the word length is 1, doesn't end in S, or ends in SS, ignore
        if (text.Length < 2 || text[^1] != 's' || text[^2] == 's')
        {
            minusS = null;
            minusES = null;
            return false;
        }
        else
        {
            FPART depluralS = FPART.NONE;
            FPART depluralES = FPART.NONE;

            bool pluralS = _allWords._dict.TryGetValue(text[..^1].ToLower(), out depluralS);
            pluralS &= (depluralS & (FPART.NOUN | FPART.PRONOUN)) != 0;
            bool pluralES = text.Length >= 3 & text[^2] == 'e' && _allWords._dict.TryGetValue(text[..^2].ToLower(), out depluralES);
            pluralES &= (depluralES & (FPART.NOUN | FPART.PRONOUN)) != 0;

            minusS = pluralS ? text[..^1] : null;
            minusES = pluralES ? text[..^2] : null;
            return pluralS || pluralES;
        }
    }
#endif
    internal bool TryGetWord(string text, List<Tile> tilesUsed, out Word word)
    {
        Debug.Log("Checking: " + text);

        word = null;

        if (!_allWords || _allWords._dict == null)
        {
            word = new Word(text, FPART.NONE, tilesUsed);
            return true;
        }

        if (text.Length == 1)
        {
            char letter = text.ToLower()[0];
            bool isWord = letter == 'a' || letter == 'i' || letter == 'o';

            if (isWord)
            {
                word = new Word(text, FPART.NONE, tilesUsed);
                return true;
            }

            return false;
        }

        if (_allWords._dict.TryGetValue(text.ToLower(), out FPART partsOfSpeech))
        {
            word = new Word(text, partsOfSpeech, tilesUsed);
            return true;
        }

        return false;
    }

    // Update is called once per frame
    void Update()
	{
#if UNITY_EDITOR && DICTIONARY_TESTING
		//runs once to test dict, after start, so every script inits first. No lateStart sadly
		if (!_hasRunTest)
		{
			_hasRunTest = true;
			Debug.Log("check: test\n");
			FPART pOS;

			//each of these calls CheckWord to see if its in the dictionary
			if (CheckWord("test", out pOS))
			{
				//if in the dictionary, print the parts of speech
				Debug.Log("true: " + pOS.ToString());
			}
			else
			{
				//otherwise print "Not a Word"
				Debug.Log("Not a Word");
			}

			Debug.Log("check: run\n");
			if (CheckWord("run", out pOS))
			{
				Debug.Log("true: " + pOS.ToString());
			}
			else
			{
				Debug.Log("Not a Word");
			}

			Debug.Log("check: defenestrate\n");
			if (CheckWord("defenestrate", out pOS))
			{
				Debug.Log("true: " + pOS.ToString());
			}
			else
			{
				Debug.Log("Not a Word");
			}

			Debug.Log("check: hell\n");
			if (CheckWord("hell", out pOS))
			{
				Debug.Log("true: " + pOS.ToString());
			}
			else
			{
				Debug.Log("Not a Word");
			}

			_hasRunTest = true;
		}
#endif

#if UNITY_EDITOR && PLURAL_TESTING
        if (!_hasRunTest)
        {
            foreach (string word in _allWords._dict.Keys)
            {
                if (CheckPlural(word, out string minusS, out string minusES))
                {
                    _plurals.Add(word, (minusS, minusES));
                }
            }

            FileStream outStream = File.OpenWrite("./Utils/Plurals.txt");
            StreamWriter writer = new StreamWriter(outStream);
            foreach (var kvp in _plurals)
            {
                writer.Write(kvp.Key);
                if (kvp.Value.Item1 != null)
                {
                    writer.Write(", " + kvp.Value.Item1);
                }
                if (kvp.Value.Item2 != null)
                {
                    writer.Write(", " + kvp.Value.Item2);
                }
                writer.Write('\n');
            }
            writer.Close();
            outStream.Close();

            _hasRunTest = true;
        }
#endif
    }
}
