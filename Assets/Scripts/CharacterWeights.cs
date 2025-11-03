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

	private static readonly char[] VOWELS = { 'A', 'E', 'I', 'O', 'U' };

	public char[] RandomChars(int count, BoardState state = null)
	{
		char[] newChars = new char[count];
		int charIndex = 0;

		if (state == null)
		{
			for (; charIndex < count; charIndex++)
			{
				newChars[charIndex] = RandomLetter();
			}
			return newChars;
		}
		
		int vowelCount = 0;
		int playableTileCount = 0;

		foreach (Vector2Int coord in new Vector2IntIterator(state._layout.BottomRight()))
		{
			switch (state[coord])
			{
				case 'A':
				case 'E':
				case 'I':
				case 'O':
				case 'U':
					vowelCount++;
					break;
			}

			switch (state._layout[coord])
			{
				case CellKind.Standard:
					playableTileCount++;
					break;
			}
		}

		// generate characters

		for (; charIndex < count; charIndex++)
		{
			if (((float)vowelCount / playableTileCount) < _minVowelRate)
			{
				newChars[charIndex] = RandomVowel();
				vowelCount++;
			}
			else
			{
				newChars[charIndex] = RandomLetter();

				switch (newChars[charIndex])
				{
					case 'A':
					case 'E':
					case 'I':
					case 'O':
					case 'U':
						vowelCount++;
						break;
				}
			}
		}

		// shuffle characters

		for (charIndex = count; charIndex > 1; charIndex--)
		{
			int rand = Random.Range(0, charIndex); // charIndex is excluded

			if (rand == charIndex - 1)
				continue;

			(newChars[rand], newChars[charIndex - 1]) = (newChars[charIndex - 1], newChars[rand]);
		}

		return newChars;
	}

	public char RandomChar(BoardState state = null)
	{
		return RandomChars(1, state)[0];
	}

	public char RandomLetter()
	{
		float sum = _weights.Sum();

		float rand = Random.Range(0.0f, 1.0f) * sum; // long-term we should have a centralized RNG so we can have consistent test cases.

		for (int charIter = 0; charIter < 26; charIter++)
		{
			if (rand < _weights[charIter])
				return (char)('A' + charIter);

			rand -= _weights[charIter];
		}

		return 'Z';
	}

	public char RandomVowel()
	{
		float sum = VOWELS.Select(vowel => _weights[vowel - 'A']).Sum();

		float rand = Random.Range(0.0f, 1.0f) * sum;

		for (int charIter = 0; charIter < VOWELS.Count(); charIter++)
		{
			if (rand < _weights[VOWELS[charIter] - 'A'])
				return VOWELS[charIter];

			rand -= _weights[VOWELS[charIter] - 'A'];
		}

		return 'U';
	}
}
