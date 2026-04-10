using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class RelicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
	public enum InventorySection
	{
		Passive_Relic,
		Active_Relic,
		Consumable_Item,
	}

	public InventorySection _section = InventorySection.Passive_Relic;

	public int _inventoryIndex;

    public TMP_Text _tooltipText;

    private InventoryManager _inventoryManager;

    void Start()
    {
        _inventoryManager = Player.INSTANCE._inventory;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
		_tooltipText.text = _section switch
		{
			InventorySection.Passive_Relic => _inventoryManager._passiveRelics[_inventoryManager._passiveRelicInventory[_inventoryIndex]].Description,
			InventorySection.Active_Relic => _inventoryManager._activeRelics[_inventoryManager._activeRelicInventory[_inventoryIndex]].Description,
			InventorySection.Consumable_Item => _inventoryManager._consumables[_inventoryIndex].Description,
			_ => throw new System.NotImplementedException(),
		};
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipText.text = "";
    }

	public void OnPointerDown(PointerEventData eventData)
	{
		switch (_section)
		{
			case InventorySection.Active_Relic:
				_inventoryManager.OnActiveRelicClicked(_inventoryIndex);
				break;
			case InventorySection.Consumable_Item:
				_inventoryManager.OnConsumableClicked(_inventoryIndex);
				break;
		}
	}

	// Update is called once per frame
	void Update()
    {
        
    }
}
