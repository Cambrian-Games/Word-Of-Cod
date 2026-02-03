using TMPro;
using UnityEngine;

public class DamagePopupScript : MonoBehaviour
{

    private float _displayedTime = 0.0f;

    private TMP_Text _text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _text = GetComponent<TMP_Text>();
        _text.enabled = false;
    }

    public void Popup(int damage)
    {
        _displayedTime = 0.0f;
        _text.text = damage.ToString();
        _text.enabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        _displayedTime += Time.deltaTime;
        if (_displayedTime >= 3.0f)
        {
            _text.enabled = false;
        }
    }
}
