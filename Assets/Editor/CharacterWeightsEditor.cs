using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CharacterWeights))]
public class CharacterWeightsEditor : Editor
{
	private static bool FORMAT_AS_PERCENT;

	public override void OnInspectorGUI()
	{
		CharacterWeights charweights = target as CharacterWeights;

		if (!charweights)
			return;

		if (charweights._weights.Length != 26)
		{
			charweights._weights = new float[26];
		}

		charweights._minVowelRate = EditorGUILayout.Slider(new GUIContent("Vowel Threshold", "The fraction of tiles that are vowels will never drop below this."), charweights._minVowelRate, 0, 1);

		EditorGUILayout.Separator();

		float totalWeight = charweights._weights.Sum();

		EditorGUILayout.LabelField("These are floats, not integers, so you can fine-tune these.");
		EditorGUILayout.LabelField("A higher number means that letter is more likely to appear.");
		EditorGUILayout.Separator();

		for (int charIter = 0; charIter < 26; charIter++)
		{
			string label = $"{(char)('A' + charIter)}";

			if (totalWeight > 0)
			{
				if (FORMAT_AS_PERCENT)
				{
					float uChance = charweights._weights[charIter] / totalWeight;
					float percentChance = uChance * 100;
					label += " (" + percentChance.ToString("0.00") + "% Chance)";
				}
				else
				{
					label += " (~1/" + Mathf.RoundToInt(totalWeight / charweights._weights[charIter]) + " Chance)";
				}
			   
			}

			charweights._weights[charIter] = Mathf.Max(0.0f, EditorGUILayout.FloatField(label, charweights._weights[charIter]));
		}

		EditorGUILayout.Separator();

		if (GUILayout.Button("Switch Chance Format"))
		{
			FORMAT_AS_PERCENT = !FORMAT_AS_PERCENT;
		}

		EditorGUILayout.Separator();

		GUI.enabled = false;
		EditorGUILayout.FloatField("Total Weight", totalWeight);
		GUI.enabled = true;

		// Required for the editor to save.

		EditorUtility.SetDirty(target);
	}
}
