using System.Collections.Generic;
using UnityEngine;

public class PostBossShop : MonoBehaviour
{
    public RewardButton _rewardButton;
    
    private void OnEnable()
    {
        List<InventoryManager.InventoryReference> relics = Player.INSTANCE._inventory.GenerateRelicItemReferences(1);
        if (relics.Count == 0)
        {
            Debug.LogError("Player has all relics already!");
            return;
        }

        _rewardButton.Initialize(relics[0]);
    }
}
