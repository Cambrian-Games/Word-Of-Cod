using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class HoverTextBattle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    [SerializeField] 
    private TMP_Text _text;
    [SerializeField] 
    private BattleRewardManager _shopManager;

	public void OnEnable()
	{
		_text.text = "";
	}

	public void OnPointerEnter(PointerEventData eventData)
    {
        //Clean This Later
        if (_shopManager._relicChoice < Player.INSTANCE._inventory._passiveRelics.Count)
        {
            _text.text = Player.INSTANCE._inventory
                ._passiveRelics[_shopManager._relicChoice].Description;
        }
        else
        {
            int index = _shopManager._relicChoice - Player.INSTANCE._inventory._activeRelics.Count;
            _text.text = Player.INSTANCE._inventory
                ._activeRelics[index]
                .Description;
        }
        
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _text.text = "";
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        
    }
    
}
