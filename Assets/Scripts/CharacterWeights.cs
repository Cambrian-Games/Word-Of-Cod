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
				newChars[charIndex] = RandomVowel(state, newChars, charIndex);
				vowelCount++;
			}
			else
			{
				newChars[charIndex] = RandomLetter(state, newChars, charIndex);

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

	public char RandomLetter(BoardState state = null, char[] previouslyAddedChars = null, int numPreviouslyAddedChars = -1)
	{
		if (state == null)
		{
			float sum = _weights.Sum();

			float rand = Random.Range(0.0f, 1.0f) * sum; // long-term we should have a centralized RNG so we can have consistent test cases.

			for (int charIter = 0; charIter < 26; charIter++)
			{
				if (rand < _weights[charIter])
					return (char)('A' + charIter);

				rand -= _weights[charIter];
			}
		}

		else
		{
			float[] modifiedWeights = GetCurrentModifiedWeights(state, previouslyAddedChars, numPreviouslyAddedChars);

			float sum = modifiedWeights.Sum();
			float rand = Random.Range(0.0f, 1.0f) * sum;

			for (int charIter = 0; charIter < 26; charIter++)
			{
				if (rand < modifiedWeights[charIter])
					return (char)('A' + charIter);

				rand -= modifiedWeights[charIter];
			}
		}


		return 'Z';
	}

	private float[] GetCurrentModifiedWeights(BoardState state, char[] previouslyAddedChars, int numPreviouslyAddedChars)
	{
		float[] result = new float[26];

		for (int charIter = 0; charIter < 26; charIter ++)
		{
			char c = (char) ('A' + charIter);
			switch (c)
			{
				case 'A':
				case 'E':
				case 'I':
				case 'O':
				case 'U':
					int numPresent = state.CountLetter(c);

					for (int i = 0; i < numPreviouslyAddedChars; i++)
					{
						if (previouslyAddedChars[i] == c)
							numPresent++;
					}

					if (numPresent >= _defaultZeroThreshold)
					{
						result[charIter] = 0;
						break;
					}

					if (numPresent > _defaultDecayThreshold && !_reducePercent)
					{
						// a(1-u) + b(u) = x
						// a = _defaultDecayThreshold
						// b = _defaultZeroThreshold - 1
						// x = numPresent

						// u = (a-x) / (a-b)

						float u = (_defaultDecayThreshold - numPresent) / (float)(_defaultDecayThreshold - (_defaultZeroThreshold - 1));
						result[charIter] = _weights[charIter] * _vowelCurve.Evaluate(u);
						break;
					}

					result[charIter] = _weights[charIter];
					break;

				default:
					result[charIter] = _weights[charIter];
					break;
			}
		}

		if (_reducePercent && false)
		{
			float totalDefaultWeight = _weights.Sum();

			float[] percentScalars = new float[VOWELS.Length];
			float[] defaultPercents = new float[VOWELS.Length];

			for (int vowelIter = 0; vowelIter < VOWELS.Length; vowelIter++)
			{
				defaultPercents[vowelIter] = _weights[VOWELS[vowelIter] - 'A'] / totalDefaultWeight;
			}

			for (int vowelIter = 0; vowelIter < VOWELS.Length; vowelIter++)
			{
				int numPresent = state.CountLetter(VOWELS[vowelIter]);

				for (int i = 0; i < numPreviouslyAddedChars; i++)
				{
					if (previouslyAddedChars[i] == VOWELS[vowelIter])
						numPresent++;
				}

				if (numPresent >= _defaultZeroThreshold)
				{
					percentScalars[vowelIter] = 0;
					continue;
				}

				if (numPresent > _defaultDecayThreshold && !_reducePercent)
				{
					// a(1-u) + b(u) = x
					// a = _defaultDecayThreshold
					// b = _defaultZeroThreshold - 1
					// x = numPresent

					// u = (a-x) / (a-b)

					float u = (_defaultDecayThreshold - numPresent) / (float)(_defaultDecayThreshold - (_defaultZeroThreshold - 1));
					percentScalars[vowelIter] = _vowelCurve.Evaluate(u);
					continue;
				}

				percentScalars[vowelIter] = 1.0f;
			}

			// how on earth do we do this math...
			// target percents are known, it's the defaultPercent * the percentScalar,
			// but reducing the percent of any vowel necessarily increases the percents of the others
		}

		return result;
	}

	public char RandomVowel(BoardState state = null, char[] previouslyAddedChars = null, int numPreviouslyAddedChars = -1)
	{
		if (state == null)
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
		else
		{
			float[] modifiedWeights = GetCurrentModifiedWeights(state, previouslyAddedChars, numPreviouslyAddedChars);

			float sum = VOWELS.Select(vowel => modifiedWeights[vowel - 'A']).Sum();

			float rand = Random.Range(0.0f, 1.0f) * sum;

			for (int charIter = 0; charIter < VOWELS.Count(); charIter++)
			{
				if (rand < modifiedWeights[VOWELS[charIter] - 'A'])
					return VOWELS[charIter];

				rand -= modifiedWeights[VOWELS[charIter] - 'A'];
			}

			return 'U';
		}

	}
}
