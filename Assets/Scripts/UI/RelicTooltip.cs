using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class RelicTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int _inventoryIndex;

    public TMP_Text _tooltipText;

    private InventoryManager _inventoryManager;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _inventoryManager = Player.INSTANCE._inventory;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _tooltipText.text = _inventoryManager._passiveRelics[_inventoryManager._passiveRelicInventory[_inventoryIndex]].Description;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipText.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
