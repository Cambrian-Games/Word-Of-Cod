using System;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager INSTANCE;

    [SerializeField]
    private Shop _eventShop;
    [SerializeField]
    private Shop _postBossShop;

    private GameObject _activeShop;

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
    }

    public bool IsShopOpen()
    {
        return _activeShop;
    }
}
