using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // it's debatable whether reward_granted needs to be a separate state from empty.
    // There is no SetRewardState function because the list of possible state transitions
    // is highly context-dependent.

    public enum RewardState
    {
        Empty,              // reward == null, button is disabled, no icon
        Reward_Enabled,     // reward != null, button is enabled,  has icon
        Reward_Disabled,    // reward != null, button is disabled, has icon
        Reward_Granted      // reward != null, button is disabled, no icon
    }

    [SerializeField]
    private Image _rewardIcon;
    [SerializeField]
    private Button _button;
    [SerializeField]
    private TMP_Text _tooltip;

    private InventoryManager.InventoryReference _reward;
    private RewardState _state;
    private int _consumableQuantity = 1;

    [SerializeField]
    private Sprite _defaultSprite;

    private Action _callback;

    public void Initialize(InventoryManager.InventoryReference reward, int quantity = 1, Action callback = null)
    {
        _state = RewardState.Reward_Enabled;
        _reward = reward;
        _button.interactable = true;
        _rewardIcon.sprite = reward.DisplayInfo().Icon;

        _consumableQuantity = quantity;
        _callback = callback;
    }

    public void InitializeEmpty()
    {
        _state = RewardState.Empty;
        _reward = new InventoryManager.InventoryReference((InventoryManager.InventorySection)(-1), -1);
        _button.interactable = false;
        _rewardIcon.sprite = _defaultSprite;
    }

    public void GrantReward()
    {
        Debug.Assert(_state == RewardState.Reward_Enabled);
        switch (_reward._section)
        {
            case InventoryManager.InventorySection.Active_Relic:
            case InventoryManager.InventorySection.Passive_Relic:
                Player.INSTANCE._inventory.GrantRelic(_reward);
                break;
            case InventoryManager.InventorySection.Consumable_Item:
                _reward.ConsumableItem()._currentCount += _consumableQuantity;
                break;
            default:
                throw new System.InvalidOperationException();
        }

        _state = RewardState.Reward_Granted;
        // reward != null
        _button.interactable = false;
        // this prevents a weird flash of white
        _rewardIcon.color = _button.colors.disabledColor;
        _rewardIcon.sprite = _defaultSprite;

        _tooltip.text = "";

        _callback?.Invoke();
    }

    public void DisableReward()
    {
        if (_state == RewardState.Reward_Enabled)
        {
            _state = RewardState.Reward_Disabled;
            // reward != null
            _button.interactable = false;
            // icon = reward.Sprite
        }
    }

    public void EnableReward()
    {
        if (_state == RewardState.Reward_Disabled)
        {
            _state = RewardState.Reward_Enabled;
            // reward != null
            _button.interactable = true;
            // icon = reward.Sprite
        }
        else if (_state != RewardState.Reward_Enabled)
        {
            Debug.LogError("Tried to enable a reward button that was in an invalid state");
        }

    }

    // Interface implementation

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_state == RewardState.Reward_Enabled || _state == RewardState.Reward_Disabled)
        {
            Debug.Assert(_tooltip);
            _tooltip.text = _reward.DisplayInfo().Description;

        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Assert(_tooltip);
        _tooltip.text = "";
    }
}