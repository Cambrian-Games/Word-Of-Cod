using UnityEngine;

[CreateAssetMenu(fileName = "CharacterSet", menuName = "Scriptable Objects/CharacterSet")]
public class CharacterSet : ScriptableObject
{
    /// <summary>
    /// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
    /// </summary>
    public Sprite[] _letterSprites;

    /// <summary>
    /// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
    /// </summary>
    public Vector3[] _letterOffsets;

    /// <summary>
    /// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
    /// </summary>
    public Sprite _quSprite;

    /// <summary>
    /// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
    /// </summary>
    public Vector3 _quOffset;

    public Sprite GetSprite(char c) => _letterSprites[c - 'A'];

    public Vector3 GetOffset(char c) => _letterOffsets[c - 'A'];

    public Sprite GetQuSprite() => _quSprite;
    public Vector3 GetQuOffset() => _quOffset;
}
