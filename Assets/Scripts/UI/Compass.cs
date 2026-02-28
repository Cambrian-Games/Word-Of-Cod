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
        UpdateNeedle(eventData);
        Debug.Log("mouse Down");
    }

    public void OnDrag(PointerEventData eventData)
    {
        //Debug.Log("drag");
        UpdateNeedle(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        UpdateNeedle(eventData);
        Debug.Log("mouse up");
    }
    
    private void UpdateNeedle(PointerEventData eventData)
    {
        Vector2 screenpos = Camera.main.WorldToScreenPoint(transform.position);
        ////Debug.Log("update needle");
        //Vector2 posDiff = eventData.position - screenpos;
        //posDiff.Normalize();
        //Debug.Log(posDiff);
        //float angle = Vector3.Angle(Input.mousePosition, screenpos);
        //Debug.Log(angle);
        //transform.eulerAngles= new Vector3(0, 0, angle);
        //transform.Rotate(new Vector3(0,0,1), angle);
        //transform.LookAt(eventData.position);
        Vector2 mouseScreenPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Debug.Log("moustPos: " + eventData.position + "\nneedlepos: " + transform.position);
        Vector2 difference = eventData.position - new Vector2(transform.position.x, transform.position.y);
        float rotationZ = Mathf.Atan2(difference.x, difference.y) * Mathf.Rad2Deg;
        float clampedAngle = MathF.Round(rotationZ / 90) * 90;
        transform.rotation = Quaternion.Euler(0.0f, 0.0f, -clampedAngle);
    }
    
}
