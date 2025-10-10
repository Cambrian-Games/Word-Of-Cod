using UnityEngine;

public class EntityDisplay : MonoBehaviour
{
    private Entity _entity;
    public Entity Entity
    {
        get => _entity;
        set => SetEntity(value);
    }

    internal SpriteRenderer _renderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _renderer = GetComponent<SpriteRenderer>();
        Debug.Assert(_renderer);

        if (_entity != null)
        {
            _renderer.sprite = _entity._sprite;
        }
    }

    // Update is called once per frame
    void Update()
    {
    }

    private void SetEntity(Entity entity)
    {
        _entity = entity;

        if (_renderer && _entity != null)
        {
            _renderer.sprite = _entity._sprite;
        }
    }
}
