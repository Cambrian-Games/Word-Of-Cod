using System.Net.NetworkInformation;
using TMPro;
using UnityEngine;

public class PlayerHealthScript : MonoBehaviour
{

    public TMP_Text Text;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Text.text = $"{Player.INSTANCE.CurrentHealth} / {Player.INSTANCE.MaxHealth}";
    }
}
