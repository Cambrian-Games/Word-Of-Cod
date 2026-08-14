using UnityEngine;

public struct SaveData
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
    }
    
    

    // Update is called once per frame
    void Update()
    {
        
    }
}
