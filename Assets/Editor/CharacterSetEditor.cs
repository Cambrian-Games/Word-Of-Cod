using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterSet))]
public class CharacterSetEditor : Editor
{
	public override void OnInspectorGUI()
	{
		CharacterSet charset = target as CharacterSet;

		if (!charset)
			return;

		if (charset._letterSprites == null || charset._letterSprites.Length != 26)
		{
            charset._letterSprites = new Sprite[26];
		}

        if (charset._letterOffsets == null || charset._letterOffsets.Length != 26)
        {
            charset._letterOffsets = new Vector3[26];
        }

		for (int charIter = 0; charIter < 26; charIter++)
		{
			string label = $"{(char)('A' + charIter)}";

            charset._letterOffsets[charIter] = EditorGUILayout.Vector3Field(label + " Offset", charset._letterOffsets[charIter]);
            charset._letterSprites[charIter] = (Sprite) EditorGUILayout.ObjectField(
                                                            label + " Sprite",
                                                            charset._letterSprites[charIter],
                                                            typeof(Sprite),
                                                            allowSceneObjects: false);

		}

        EditorGUILayout.Separator();

        charset._quSprite = (Sprite) EditorGUILayout.ObjectField("Qu Sprite", charset._quSprite, typeof(Sprite), allowSceneObjects: false);
        charset._quOffset = EditorGUILayout.Vector3Field("Qu Offset", charset._quOffset);

        // Required for the editor to save.

        EditorUtility.SetDirty(target);
	}
}
