using System;
using UnityEngine;
using odin.serialize.OdinSerializer;
using System.IO;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public bool _analyticsState;
    
    public (string, int) _longestWord;
    public (string, int) _bestWord;
    public int _runsWon;
    public int _totalRuns;
}



public class SaveManager : MonoBehaviour
{
    public static SaveManager INSTANCE;
    
    [SerializeField]
    public SaveData _saveData;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            Debug.LogError("Attempted to create second player!");
            Destroy(gameObject);
            return;
        }

        INSTANCE = this;
        
        DontDestroyOnLoad(gameObject);
        
        ReadSaveData();
    }
    
    

    // Update is called once per frame
    void Update()
    {
        
    }

    private void ReadSaveData()
    {
        if (File.Exists(Application.streamingAssetsPath + "/saveData"))
        {
            byte[] savebytes = File.ReadAllBytes(Application.streamingAssetsPath + "/saveData");
            _saveData = SerializationUtility.DeserializeValue<SaveData>(savebytes, DataFormat.Binary);
            Debug.Log(_saveData._longestWord.Item1 +", " + _saveData._longestWord.Item2);
            Debug.Log(_saveData._bestWord.Item1 +", " + _saveData._bestWord.Item2);
        }
        else
        {
            _saveData = new SaveData();
        }

    }

    public void WriteSaveData()
    {
        byte[] outBytes = SerializationUtility.SerializeValue(_saveData, DataFormat.Binary);
        File.WriteAllBytes(Application.streamingAssetsPath + "/saveData", outBytes);
    }
}
