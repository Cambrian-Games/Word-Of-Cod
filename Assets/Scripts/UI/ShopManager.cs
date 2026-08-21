using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public static ShopManager INSTANCE;

    [SerializeField]
    private EventShop _eventShop;
    [SerializeField]
    private PostBossShop _postBossShop;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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
