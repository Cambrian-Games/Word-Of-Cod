using odin.serialize.OdinSerializer;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WordChecker : MonoBehaviour
{
    //the dictionary
    public SerializedDict _allWords;

#if UNITY_EDITOR
	private bool _hasRunTest = false;
#endif

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

    //checks if the word is in the dict, if yes returns true, if no return false
    public bool CheckWord(string word, out FPART pOS)
    {
        //returns true if the word is in the dictionary, and puts the parts of speech in pOS
        //otherwise returns false
        Debug.Log("Checking: " + word);
		Debug.Log("");

		if (!_allWords || _allWords._dict == null)
		{
			pOS = FPART.NONE;
			return true;
		}

		if (word.Length == 1)
		{
			pOS = FPART.NONE;
			string wordTest = word.ToLower();
			// hard coding this for now
			return wordTest[0] == 'a' || wordTest[0] == 'i' || wordTest[0] == 'o';
		}

        return _allWords._dict.TryGetValue(word.ToLower(), out pOS);
	}

	// Update is called once per frame
	void Update()
	{
#if UNITY_EDITOR
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
	}
}
