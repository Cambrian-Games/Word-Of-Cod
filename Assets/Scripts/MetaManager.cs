using TMPro;
using UnityEngine;

public class MetaManager : MonoBehaviour
{
    //4 incrementors, 4 decrementors, 4 reset buttons, 1 reset all button, 1 disable button
    public int _maxHPPoints = 5;
    public int _maxDamagePoints = 5;
    public int _maxResistPoints = 5;
    public int _maxLetterPoints = 5;

    public SaveManager _saveManager;

    public TMP_Text _HPText, _damageText, _resistText, _LetterText, _spentText;

    private int _spentPoints;
    private int _maxPoints;
    

    void OnEnable()
    {
        _maxPoints = _maxHPPoints + _maxDamagePoints + _maxResistPoints + _maxResistPoints;
        
        SaveData saveData = _saveManager._saveData;
        
        _spentPoints = saveData._metaHealthPoints + saveData._metaDamagePoints + saveData._metaResistPoints +
                       saveData._metaLetterCurrencyPoints;
        
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(saveData._runsWon, _maxPoints);
        _HPText.text = "HP Upgrade\nCount: " + saveData._metaHealthPoints + "/" + _maxHPPoints;
        _damageText.text = "Damage Upgrade\nCount: " + saveData._metaDamagePoints + "/" + _maxDamagePoints;
        _resistText.text = "Damage Resist Upgrade \nCount: " + saveData._metaResistPoints + "/" + _maxResistPoints;
        _LetterText.text = "Letter Weight Currency Upgrade \nCount: " + saveData._metaLetterCurrencyPoints + "/" + _maxLetterPoints;
    }

    // Update is called once per frame
    void Update()
    {
    }

    private bool CanSpend()
    {
        SaveData saveData = _saveManager._saveData;
        int pointsSpent = saveData._metaHealthPoints + saveData._metaDamagePoints + saveData._metaResistPoints +
                          saveData._metaLetterCurrencyPoints;
        return (pointsSpent < saveData._runsWon);
    }

    public void IncrementHPPoints()
    {
        if (!CanSpend())
            return;
        _saveManager._saveData._metaHealthPoints = Mathf.Min(_maxHPPoints, _saveManager._saveData._metaHealthPoints + 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _HPText.text = "HP Upgrade\nCount: " + _saveManager._saveData._metaHealthPoints + "/" + _maxHPPoints;

    }
    
    public void DecrementHPPoints()
    {
        _saveManager._saveData._metaHealthPoints = Mathf.Max(0, _saveManager._saveData._metaHealthPoints - 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _HPText.text = "HP Upgrade\nCount: " + _saveManager._saveData._metaHealthPoints + "/" + _maxHPPoints;

    }

    public void ResetHPPoints()
    {
        _saveManager._saveData._metaHealthPoints = 0;
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _HPText.text = "HP Upgrade\nCount: " + _saveManager._saveData._metaHealthPoints + "/" + _maxHPPoints;

    }
    
    public void IncrementDamagePoints()
    {
        if (!CanSpend())
            return;
        _saveManager._saveData._metaDamagePoints = Mathf.Min(_maxDamagePoints, _saveManager._saveData._metaDamagePoints + 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _damageText.text = "Damage Upgrade\nCount: " + _saveManager._saveData._metaDamagePoints + "/" + _maxDamagePoints;

    }
    
    public void DecrementDamagePoints()
    {
        _saveManager._saveData._metaDamagePoints = Mathf.Max(0, _saveManager._saveData._metaDamagePoints - 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _damageText.text = "Damage Upgrade\nCount: " + _saveManager._saveData._metaDamagePoints + "/" + _maxDamagePoints;

    }

    public void ResetDamagePoints()
    {
        _saveManager._saveData._metaDamagePoints = 0;
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _damageText.text = "Damage Upgrade\nCount: " + _saveManager._saveData._metaDamagePoints + "/" + _maxDamagePoints;

    }
    
    public void IncrementResistPoints()
    {
        if (!CanSpend())
            return;
        _saveManager._saveData._metaResistPoints = Mathf.Min(_maxResistPoints, _saveManager._saveData._metaResistPoints + 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _resistText.text = "Damage Resist Upgrade \nCount: " + _saveManager._saveData._metaResistPoints + "/" + _maxResistPoints;

    }
    
    public void DecrementResistPoints()
    {
        _saveManager._saveData._metaResistPoints = Mathf.Max(0, _saveManager._saveData._metaResistPoints - 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _resistText.text = "Damage Resist Upgrade \nCount: " + _saveManager._saveData._metaResistPoints + "/" + _maxResistPoints;

    }

    public void ResetResistPoints()
    {
        _saveManager._saveData._metaResistPoints = 0;
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _resistText.text = "Damage Resist Upgrade \nCount: " + _saveManager._saveData._metaResistPoints + "/" + _maxResistPoints;

    }
    
    public void IncrementLetterPoints()
    {
        if (!CanSpend())
            return;
        _saveManager._saveData._metaLetterCurrencyPoints = Mathf.Min(_maxLetterPoints, _saveManager._saveData._metaLetterCurrencyPoints + 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _LetterText.text = "Letter Weight Currency Upgrade \nCount: " + _saveManager._saveData._metaLetterCurrencyPoints + "/" + _maxLetterPoints;

    }
    
    public void DecrementLetterPoints()
    {
        _saveManager._saveData._metaLetterCurrencyPoints = Mathf.Max(0, _saveManager._saveData._metaLetterCurrencyPoints - 1);
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _LetterText.text = "Letter Weight Currency Upgrade \nCount: " + _saveManager._saveData._metaLetterCurrencyPoints + "/" + _maxLetterPoints;

    }

    public void ResetLetterPoints()
    {
        _saveManager._saveData._metaLetterCurrencyPoints = 0;
        _spentPoints = _saveManager._saveData._metaHealthPoints + _saveManager._saveData._metaDamagePoints + _saveManager._saveData._metaResistPoints +
                       _saveManager._saveData._metaLetterCurrencyPoints;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _LetterText.text = "Letter Weight Currency Upgrade \nCount: " + _saveManager._saveData._metaLetterCurrencyPoints + "/" + _maxLetterPoints;

    }

    public void ResetAllPoints()
    {
        ResetHPPoints();
        ResetDamagePoints();
        ResetResistPoints();
        ResetLetterPoints();
        _spentPoints = 0;
        _spentText.text = "Points Used: " + _spentPoints + "/" + Mathf.Min(_saveManager._saveData._runsWon, _maxPoints);
        _HPText.text = "HP Upgrade\nCount: " + _saveManager._saveData._metaHealthPoints + "/" + _maxHPPoints;
        _damageText.text = "Damage Upgrade\nCount: " + _saveManager._saveData._metaDamagePoints + "/" + _maxDamagePoints;
        _resistText.text = "Damage Resist Upgrade \nCount: " + _saveManager._saveData._metaResistPoints + "/" + _maxResistPoints;
        _LetterText.text = "Letter Weight Currency Upgrade \nCount: " + _saveManager._saveData._metaLetterCurrencyPoints + "/" + _maxLetterPoints;
    }
}
