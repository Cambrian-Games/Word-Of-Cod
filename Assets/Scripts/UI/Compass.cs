using System;
using UnityEngine;
using UnityEngine.EventSystems;
public class Compass : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
    {
        if (GameBoard.INSTANCE.IsSettled())
        {
            UpdateNeedle(eventData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameBoard.INSTANCE.IsSettled())
        {
            UpdateNeedle(eventData);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (GameBoard.INSTANCE.IsSettled())
        {
            UpdateNeedle(eventData);
        }
        
        if (transform.eulerAngles.z == 0)
        {
            BoardConfig.INSTANCE.SetOverrideSettlekUp();
            Debug.Log("current up");
        }
        else if (transform.eulerAngles.z == 90)
        {
            BoardConfig.INSTANCE.SetOverrideSettlekRight();
            Debug.Log("current left");

        }
        else if (transform.eulerAngles.z == -90 || transform.eulerAngles.z == 270)
        {
            BoardConfig.INSTANCE.SetOverrideSettlekLeft();
            Debug.Log("current right");
        }
        else if (transform.eulerAngles.z == 180 || transform.eulerAngles.z == -180)
        {
            BoardConfig.INSTANCE.SetOverrideSettlekDown();
            Debug.Log("current down");
        }
        
    }
    
    private void UpdateNeedle(PointerEventData eventData)
    {
        Vector2 difference = eventData.position - new Vector2(transform.position.x, transform.position.y);
        float rotationZ = Mathf.Atan2(difference.x, difference.y) * Mathf.Rad2Deg;
        float clampedAngle = Mathf.Round(rotationZ / 90) * 90;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, -clampedAngle);
    }
    
}
