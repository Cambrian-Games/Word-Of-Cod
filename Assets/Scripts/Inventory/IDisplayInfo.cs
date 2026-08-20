using UnityEngine;

public interface IDisplayInfo
{
    string DisplayName { get; }
    string Description { get; }
    Sprite Icon { get; }
    int ID { get; set; }
}