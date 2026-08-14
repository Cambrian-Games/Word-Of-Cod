using UnityEngine;
using odin.serialize.OdinSerializer;
using System.IO;


public class SaveData
{
    public bool _analyticsState;
}



public class SaveManager : MonoBehaviour
{
    public static SaveManager INSTANCE;

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
