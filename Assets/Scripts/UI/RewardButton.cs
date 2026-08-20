using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RewardButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private Image _rewardIcon;
    [SerializeField]
    private TMP_Text _tooltip;

    private InventoryManager.InventoryReference? _reward;
    private int _consumableQuantity = 1;

    [Header("Do Not Modify This!")]
    public bool _canGiveReward;

    [SerializeField]
    private Sprite _defaultSprite;
    [SerializeField]
    private Color _defaultColor;

    private Action _callback;

    public void Initialize(InventoryManager.InventoryReference reward, int quantity = 1, Action callback = null)
    {
        _reward = reward;
        _consumableQuantity = quantity;
        _rewardIcon.sprite = reward.Sprite();
        _rewardIcon.color = new Color(1, 1, 1, 1);
        _canGiveReward = true;
        _callback = callback;
    }

    public void InitializeEmpty()
    {
        _canGiveReward = false;
        _rewardIcon.sprite = _defaultSprite;
        _rewardIcon.color = _defaultColor;
    }

    public void GrantReward()
    {
        if (!_canGiveReward)
            return;

        switch (_reward.Value._section)
        {
            case InventoryManager.InventorySection.Active_Relic:
            case InventoryManager.InventorySection.Passive_Relic:
                Player.INSTANCE._inventory.GrantRelic(_reward.Value);
                break;
            case InventoryManager.InventorySection.Consumable_Item:
                _reward.Value.ConsumableItem()._currentCount += _consumableQuantity;
                break;
            default:
                throw new System.InvalidOperationException();
        }

        _rewardIcon.sprite = null;
        _rewardIcon.color = new Color(0, 0, 0, 0);

        _canGiveReward = false;
        _tooltip.text = "";

        _callback?.Invoke();
    }

    public void DisableReward()
    {
        if (_canGiveReward)
        {
            _canGiveReward = false;
            _rewardIcon.color = _defaultColor;
        }
    }

    public void EnableReward()
    {
        if (!_reward.HasValue)
        {
            Debug.LogError("Trying to enable reward on button that doesn't have one!");
            return;
        }

        _canGiveReward = true;
        _rewardIcon.color = new Color(1, 1, 1, 1);
    }

    // Interface implementation

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!_canGiveReward)
            return;

        Debug.Assert(_tooltip);
        _tooltip.text = _reward.Value.Description();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Assert(_tooltip);
        _tooltip.text = "";
    }
}