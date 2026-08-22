using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager INSTANCE;

    [SerializeField]
    private Shop _eventShop;
    [SerializeField]
    private Shop _postBossShop;
    private GameObject _activeShop;

    [SerializeField]
    private LetterWeightMenu _letterWeightMenu;



    private bool _isLetterWeightMenuQueued;

    void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            // this script lives on the shop canvas object, we don't want to destroy the whole gameobject
            Destroy(this);
            return;
        }

        INSTANCE = this;
    }

    public void OpenEventShop()
    {
        if (_activeShop == null)
        {
            _eventShop.gameObject.SetActive(true);
            _activeShop = _eventShop.gameObject;
        }
    }

    public void OpenPostBossShop()
    {
        if (_activeShop == null)
        {
            _postBossShop.gameObject.SetActive(true);
            _activeShop = _postBossShop.gameObject;
        }
    }

    public void CloseShop()
    {
        _activeShop.SetActive(false);
        _activeShop = null;

        if (_isLetterWeightMenuQueued)
        {
            OpenFullLetterWeightMenu();
        }
    }

    public bool IsShopOpen()
    {
        return _activeShop;
    }

    public void OpenFullLetterWeightMenu()
    {

    }

    public void CloseLetterWeightMenu()
    {
        _isLetterWeightMenuQueued = false;
    }

    public bool IsLetterWeightMenuOpen()
    {
        return false;
    }

    public void QueueFullLetterWeightMenu()
    {
        _isLetterWeightMenuQueued = true;
    }
}
