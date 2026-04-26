using System;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterWeights", menuName = "Scriptable Objects/Character Weights")]
public class CharacterWeights : ScriptableObject
{
	/// <summary>
	/// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
	/// </summary>
	public float[] _weights;

	/// <summary>
	/// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
	/// </summary>
	public float _minVowelRate = 0.2f;

    /// <summary>
    /// DO NOT MODIFY THIS AT RUNTIME. It has to be public for the custom editor to work, but modifying it via code during gameplay will save those changes.
    /// </summary>
    public int[] _baseScores;

	public AnimationCurve _vowelCurve;
	public bool _reducePercent;
	public int _defaultDecayThreshold;
	public int _defaultZeroThreshold;

    public int Score(char c) => _baseScores[c - 'A'];

	private static readonly char[] VOWELS = { 'A', 'E', 'I', 'O', 'U' };



	public char[] FreshBoard(BoardState state)
	{
		Vector2Int dims = state._layout.Dims();
		char[] newChars = new char[dims.x * dims.y];

		byte[] charCounts = new byte[26];

		for (int i = 0; i < newChars.Length; i++)
		{
			newChars[i] = RandomLetter(ref charCounts);
			charCounts[newChars[i] - 'A']++;
		}

		EnforceVowelMinimums(ref newChars, newChars.Length, ref charCounts);

		ShuffleChars(ref newChars);

		return newChars;
	}

	internal char[] RandomChars(int count, BoardState state)
	{
		Vector2Int bottomRight = state._layout.BottomRight();

		byte[] charCounts = new byte[26];
		int charsInBoardState = 0;

		foreach (Vector2Int coord in new Vector2IntIterator(bottomRight))
		{
			if (state[coord] != ' ')
			{
				charCounts[state[coord] - 'A']++;
				charsInBoardState++;
			}
		}

		Debug.Assert(charsInBoardState + count == (bottomRight.x + 1) * (bottomRight.y + 1));

		if (charsInBoardState == 0)
		{
			return FreshBoard(state);
		}

		char[] newChars = new char[count];

		for (int i = 0; i < newChars.Length; i++)
		{
			newChars[i] = RandomLetter(ref charCounts);
			charCounts[newChars[i] - 'A']++;
		}

		// if we want to enforce vowel minimum here, we can
		// EnforceVowelMinimums(ref newChars, charsInBoardState + count, ref charCounts);

		return newChars;
	}

	public char RandomLetter(ref byte[] charCounts)
	{
		float[] modifiedWeights = GetCurrentModifiedWeights(ref charCounts);

		float sum = modifiedWeights.Sum();
		float rand = UnityEngine.Random.Range(0.0f, 1.0f) * sum;

		int charIter = 0;

		while (rand > modifiedWeights[charIter])
		{
			rand -= modifiedWeights[charIter];
			charIter++;
		}

		return (char) ('A' + charIter);
	}

	public char RandomVowel(ref byte[] charCounts)
	{
		float[] modifiedWeights = GetCurrentModifiedWeights(ref charCounts);

		float sum = VOWELS.Sum(vowel => modifiedWeights[vowel - 'A']);
		float rand = UnityEngine.Random.Range(0.0f, 1.0f) * sum;

		int vowelIter = 0;

		while (rand > modifiedWeights[VOWELS[vowelIter]])
		{
			rand -= modifiedWeights[VOWELS[vowelIter]];
			vowelIter++;
		}

		return (char)('A' + VOWELS[vowelIter]);
	}

	public void EnforceVowelMinimums(ref char[] newChars, int totalChars, ref byte[] charCounts)
	{
		int vowelCount = 0;
		foreach (char vowel in VOWELS)
		{
			vowelCount += charCounts[vowel - 'A'];
		}
		
		float vowelRate = vowelCount / (float)totalChars;

		int charIter = 0;

		while (vowelRate < _minVowelRate && charIter < newChars.Length)
		{
			if (VOWELS.Contains(newChars[charIter]))
				continue;

			// if the char is not a vowel, subtract it from the char counts and get a vowel

			charCounts[newChars[charIter] - 'A']--;

			newChars[charIter] = RandomVowel(ref charCounts);

			charCounts[newChars[charIter] - 'A']++;

			vowelCount++;
			vowelRate = vowelCount / (float)newChars.Length;
			charIter++;
		}
	}

	public void ShuffleChars(ref char[] chars)
	{
		for (int charIndex = chars.Length; charIndex > 1; charIndex--)
		{
			int rand = UnityEngine.Random.Range(0, charIndex); // charIndex is excluded

			if (rand == charIndex - 1)
				continue;

			(chars[rand], chars[charIndex - 1]) = (chars[charIndex - 1], chars[rand]);
		}
	}

	private float[] GetCurrentModifiedWeights(ref byte[] charCounts)
	{
		float[] newWeights = new float[_weights.Length];
		Array.Copy(_weights, newWeights, _weights.Length);

		foreach (char vowel in VOWELS)
		{
			byte charCount = charCounts[vowel - 'A'];

			if (charCount >= _defaultZeroThreshold)
			{
				newWeights[vowel - 'A'] = 0;
				continue;
			}

			if (charCount > _defaultDecayThreshold)
			{
				// a(1-u) + b(u) = x
				// a = _defaultDecayThreshold
				// b = _defaultZeroThreshold - 1
				// x = numPresent

				// u = (a-x) / (a-b)

				float u = (_defaultDecayThreshold - charCount) / (float)(_defaultDecayThreshold - (_defaultZeroThreshold - 1));
				newWeights[vowel - 'A'] *= _vowelCurve.Evaluate(u);
				continue;
			}
		}

		return newWeights;
	}
}