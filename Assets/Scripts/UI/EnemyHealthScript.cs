using System;
using TMPro;
using UnityEngine;

public class EnemyHealthScript : MonoBehaviour
{
    public TMP_Text Text;

    private Enemy currEnemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnEnable()
    {
        //long term this is right
        //currEnemy = BattleManager.INSTANCE.CurrentEnemy;
    }

    // Update is called once per frame
    void Update()
    {
        //short term this is right
        currEnemy = BattleManager.INSTANCE.CurrentEnemy;
        if (currEnemy != null)
        {
            Text.text = $"{currEnemy.CurrentHealth} / {currEnemy.MaxHealth}";
        }
    }
}
