using System.Collections.Generic;
using UnityEngine;

public class Shop : MonoBehaviour
{
    [SerializeField]
    private List<RewardButton> _relicRewardButtons;

    [SerializeField, Min(0)]
    private int _relicsToGrant;

    [SerializeField]
    private List<RewardButton> _itemRewardButtons;

    [SerializeField, Min(0)]
    private int _itemsToGrant;
    [SerializeField, Min(0)]
    private int _quantityPerItem;

    private void OnValidate()
    {
        _relicsToGrant = Mathf.Min(_relicRewardButtons.Count, _relicsToGrant);
        _itemsToGrant = Mathf.Min(_itemRewardButtons.Count, _itemsToGrant);
    }

    private void OnEnable()
    {
        List<InventoryManager.InventoryReference> relics = Player.INSTANCE._inventory.GenerateRelicReferences(_relicsToGrant);

        if (relics.Count < _relicsToGrant)
        {
            Debug.LogWarning("Couldn't generate enough relics!");
        }

        for (int i = 0; i < relics.Count; i++)
        {
            _relicRewardButtons[i].Initialize(relics[i], callback: OnRelicPicked);
        }

        for (int i = relics.Count; i < _relicRewardButtons.Count; i++)
        {
            _relicRewardButtons[i].InitializeEmpty();
        }

        // TODO initialize items
        for (int i = 0; i < _itemRewardButtons.Count; i++)
        {
            _itemRewardButtons[i].InitializeEmpty();
        }
    }

    public void OnRelicPicked()
    {
        foreach (RewardButton button in _relicRewardButtons)
        {
            button.DisableReward();
        }

        ShopManager.INSTANCE.QueueFullLetterWeightMenu();
    }
}
