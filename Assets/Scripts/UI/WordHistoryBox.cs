using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class WordHistoryBox : MonoBehaviour
{
    public GameObject _wordHistoryPrefab;

    public GameObject _prefabParent;
    private List<GameObject> _wordHistoryObjects = new List<GameObject>();

    public void AddWordToHistory(Word word)
    {
        //create word prefab
        //setting defaults because in testing it defaulted to weird things
        GameObject wordEntry = Instantiate(_wordHistoryPrefab, new Vector3(0f, 0f, 0f), Quaternion.identity, _prefabParent.transform);
        wordEntry.transform.localScale = new Vector3(1f, 1f, 1f);
        //set word and part of speech
        wordEntry.GetComponentInChildren<TMP_Text>().text = word.Text + ParseFirstPOS(word.PartsOfSpeech);
        //set icons for relics that triggered on the word
        int relicCount = word.PassiveRelicsTriggered.Count;
        for (int i = 0; i < 3; i++)
        {
            if (i < relicCount)
            {
                wordEntry.transform.GetChild(i+1).gameObject.GetComponent<Image>().sprite = Player.INSTANCE._inventory._passiveRelics[word.PassiveRelicsTriggered[i]].Icon;
            }
            else
            {
                wordEntry.transform.GetChild(i+1).gameObject.SetActive(false);
            }
        }
        _wordHistoryObjects.Add(wordEntry);
    }

    public void ClearHistoryBox()
    {
        foreach (GameObject wordHistoryObject in _wordHistoryObjects)
        {
            Destroy(wordHistoryObject);
        }
    }

    private string ParseFirstPOS(FPART pos)
    {
        //returns the first part of speech the word has
        if (pos.HasFlag(FPART.NOUN))
        {
            return ": Noun";
        }
        if (pos.HasFlag(FPART.VERB))
        {
            return ": Verb";
        }
        if (pos.HasFlag(FPART.ADJECTIVE))
        {
            return ": Adj.";
        }
        if (pos.HasFlag(FPART.ADVERB))
        {
            return ": Adv.";
        }
        if (pos.HasFlag(FPART.PREPOSITION))
        {
            return ": Prep.";
        }
        if (pos.HasFlag(FPART.PRONOUN))
        {
            return ": Prn.";
        }
        if (pos.HasFlag(FPART.CONJUNCTION))
        {
            return ": Conj.";
        }

        return "";
    }
}
