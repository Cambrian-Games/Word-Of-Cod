using System.Diagnostics;
using System.IO;
using System.Text.Json;

class ParserEntry
{
	public static string PREFIX = "..\\..\\..\\";

	static FirstPhaseParser parserPhase1 = new FirstPhaseParser();
	static SecondPhaseParser parserPhase2 = new SecondPhaseParser();
	static ThirdPhaseParser parserPhase3 = new ThirdPhaseParser();
	static FourthPhaseParser parserPhase4 = new FourthPhaseParser();

	static void Main(string[] args)
	{
		parserPhase1.Parse(args);
	}
}
class FirstPhaseParser
{
	string _fulldictPath = "raw-wiktextract-data.jsonl";
	
	FileStream stream = null;
	
	Dictionary<string, SimpleWord> _wordDict = new Dictionary<string, SimpleWord>();
    Dictionary<string, List<FlaggedWord>> _flaggedWords = new Dictionary<string, List<FlaggedWord>>();
    Dictionary<string, List<DerivedWord>> _derivedWords = new Dictionary<string, List<DerivedWord>>();

	public void Parse(string[] args)
	{
		try
		{
			stream = File.OpenRead(ParserEntry.PREFIX + _fulldictPath);

			if (stream.CanRead)
			{
				StreamReader reader = new StreamReader(stream);

				string? jsonString = null;

				SimpleWord? simpleWord = null;

				while ((jsonString = reader.ReadLine()) != null)
				{
					Word? jsonWord = JsonSerializer.Deserialize<Word>(jsonString);

					// filter out null and non-english words

					if (jsonWord == null)
						continue;

					if (jsonWord.lang != "English")
						continue;

                    // what happens when an invalid word would fail the profanity filter? We still need its derived terms

                    if (!IsValidWord(jsonWord))
                        continue;

                    if (IsBlacklisted(jsonWord))
                        continue;

                    if (!IsWhitelisted(jsonWord))
                    {
                        if (!HasValidPartOfSpeech(jsonWord))
                            continue;

                        if (!PassesProfanityFilter(jsonWord))
                            continue;

                        if (IsVariantSpelling(jsonWord))
                            continue;
                    }

                    // this section has to be done later. We can't afford to merge words while filtering is going on.

					if (simpleWord == null)
					{
						simpleWord = new SimpleWord();
						simpleWord.word = jsonWord.word;
					}
					else if (simpleWord.word != jsonWord.word) // we're switching to this word
					{
						if (_wordDict.ContainsKey(simpleWord.word))
						{
							if (simpleWord.pos != null)
							{
								_wordDict[simpleWord.word].pos.AddRange(simpleWord.pos);
							}
							if (simpleWord.senses != null)
							{
								_wordDict[simpleWord.word].senses.AddRange(simpleWord.senses);
							}
							Console.WriteLine("How did we get here?");
						}

						_wordDict.TryAdd(simpleWord.word, simpleWord);
						simpleWord = new SimpleWord();
						simpleWord.word = jsonWord.word;

						if (_wordDict.Count % 10000 == 0)
							Console.WriteLine($"{_wordDict.Count}");
					}

					simpleWord.pos.Add(jsonWord.pos);

					if (jsonWord.senses != null)
					{
						simpleWord.senses.AddRange(jsonWord.senses.ToList());
					}
				}
				reader.Close();
				stream.Close();

                // check if words with flagged parts-of-speech exist in the dictionary already, and remove it from "incorrect part-of-speech" flag list

                for (int i = _flaggedWords["Part of Speech"].Count - 1; i >= 0; i--)
                {
                    if (_wordDict.ContainsKey(_flaggedWords["Part of Speech"][i].word))
                        _flaggedWords["Part of Speech"].RemoveAt(i);
                }

                List<string> wordsToRemove = _derivedWords["Part of Speech"].Where(derived => !_wordDict.ContainsKey(derived.source)).Select(derived => derived.word).ToList();

                // if a word is in one sense derived from a word that was filtered out, but in another exists on its own as a non-filtered word, we want to keep it.

                //foreach (string remove in wordsToRemove)
                //{
                //    _wordDict.Remove(remove);
                //}

                FileStream flaggedWordsStream = File.OpenWrite(ParserEntry.PREFIX + "flagged.txt");
                StreamWriter flaggedWordsWriter = new StreamWriter(flaggedWordsStream);

                string flaggedJson = JsonSerializer.Serialize(_flaggedWords);
                flaggedWordsWriter.Write(flaggedJson);

                flaggedWordsWriter.Close();
                flaggedWordsStream.Close();

				FileStream firstFilterStream = File.OpenWrite(ParserEntry.PREFIX + "firstpass.txt");
				StreamWriter firstFilterWriter = new StreamWriter(firstFilterStream);

				string strJson = JsonSerializer.Serialize(_wordDict);

				firstFilterWriter.Write(strJson);
				firstFilterWriter.Close();
                firstFilterStream.Close();
			}
		}
		catch
		{
			Console.WriteLine("File failed to load, possibly missing?");
		}
	}

    // force word to be included if in this list
    private bool IsWhitelisted(Word jsonWord)
    {
        if (jsonWord.word.ToLower() == "dictionary")
            return true;

        return false;
    }

    // exclude word if it's in this list
    private bool IsBlacklisted(Word jsonWord)
    {
        return false;
    }

    public static bool IsValidWord(Word jsonWord)
    {
        if (jsonWord.word == null || jsonWord.word.Length == 0)
            return false;

        string word = jsonWord.word;
        string lower = word.ToLower();

        // only allow A, I, and O 

        if (lower.Length == 1)
        {
            switch (lower[0])
            {
                case 'a':
                case 'i':
                case 'o': // still iffy on this
                    return true;
                default:
                    return false;
            }
        }

        // no need to output here

        if (lower.IndexOfAny(new char[] { '-', ' ' }) != -1)
        {
            return false;
        }

        // word must contain at least one vowel

        if (lower.IndexOfAny(new char[] { 'a', 'e', 'i', 'o', 'u', 'y'}) == -1)
        {
            //Console.WriteLine($"{word} does not contain any vowels.");
            return false;
        }
        
        if (lower.IndexOfAny(new char[] {'&', '+', '.', ',', '\'', '"', '/', '*'}) != -1)
        {
            //Console.WriteLine($"{word} has a non-alphanumeric symbol.");
            return false;
        }

        if (lower.IndexOfAny(new char[] {'0', '1', '2', '3', '4', '5', '6', '7', '8', '9'}) != -1)
        {
            //Console.WriteLine($"{word} contains a number.");
            return false;
        }

        return true;
    }

    public bool HasValidPartOfSpeech(Word jsonWord)
    {
        string pOS = jsonWord.pos;

        if (pOS == null || pOS.Length == 0)
        {
            //Console.WriteLine($"{jsonWord.word} has no part of speech?");
            return false;
        }

        switch (pOS)
        {
            // these are all tracked
            case "noun":
            case "verb":
            case "adj":
            case "adv":
            case "prep":
            case "pron":
            case "conj":
                return true;

            // these should all be discarded entirely
            case "proverb":
            case "prep_phrase":
            case "phrase":
            case "name":
                return false;

            default:

                // add these to flagged words. Will run a second pass to clear out any with valid parts of speech
                if (!_flaggedWords.ContainsKey("Part of Speech"))
                {
                    _flaggedWords["Part of Speech"] = new List<FlaggedWord>();
                }

                _flaggedWords["Part of Speech"].Add(new FlaggedWord(jsonWord.word, pOS));

                if (!_derivedWords.ContainsKey("Part of Speech"))
                {
                    _derivedWords["Part of Speech"] = new List<DerivedWord>();
                }

                jsonWord.forms?.ToList().ForEach(form => _derivedWords["Part of Speech"].Add(new DerivedWord(form.form, jsonWord.word)));
                jsonWord.hyponyms?.ToList().ForEach(hyponym => _derivedWords["Part of Speech"].Add(new DerivedWord(hyponym.word, jsonWord.word)));
                jsonWord.hyponyms?.ToList().ForEach(derived => _derivedWords["Part of Speech"].Add(new DerivedWord(derived.word, jsonWord.word)));
                jsonWord.senses?.ToList().ForEach(sense => sense.synonyms?.ToList().ForEach(synonym => _derivedWords["Part of Speech"].Add(new DerivedWord(synonym.word, jsonWord.word))));

                //Console.WriteLine($"{jsonWord.word} Flagged: Part of Speech is {pOS}");
                return false;
        }
    }

    public bool PassesProfanityFilter(Word jsonWord)
    {
        bool passes = true;

        List<string> glosses = new List<string>();

        jsonWord.senses?.ToList().ForEach(sense =>
        {
            if (sense.raw_glosses != null)
            {
                glosses.AddRange(sense.raw_glosses);
            }
        });

        int glossesWithKeyword = glosses.Count(gloss =>
        {
            string lowerGloss = gloss.ToLower();

            return
                lowerGloss.Contains("derogatory") ||
                lowerGloss.Contains("slur") ||
                lowerGloss.Contains("offensive") ||
                lowerGloss.Contains("ethnic") ||
                lowerGloss.Contains("slang") ||
                lowerGloss.Contains("vulgar");
        });

        if (glossesWithKeyword > 0)
        {
            if (!_flaggedWords.ContainsKey("Flagged Keyword in Definition"))
            {
                _flaggedWords["Flagged Keyword in Definition"] = new List<FlaggedWord>();
            }

            _flaggedWords["Flagged Keyword in Definition"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));

            passes = false;
        }

        List<string> tags = new List<string>();

        jsonWord.senses?.ToList().ForEach(sense =>
        {
            if (sense.tags != null)
            {
                tags.AddRange(sense.tags);
            }
        });

        int tagsWithKeyword = tags.Count(tag =>
        {
            string lowerTag = tag.ToLower();

            return
                lowerTag.Contains("derogatory") ||
                lowerTag.Contains("slur") ||
                lowerTag.Contains("offensive") ||
                lowerTag.Contains("ethnic") ||
                lowerTag.Contains("slang") ||
                lowerTag.Contains("vulgar");
        });

        if (tagsWithKeyword > 0)
        {
            if (!_flaggedWords.ContainsKey("Flagged Keyword in Tags"))
            {
                _flaggedWords["Flagged Keyword in Tags"] = new List<FlaggedWord>();
            }

            _flaggedWords["Flagged Keyword in Tags"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));

            passes = false;
        }

        return passes;
    }

    public bool IsVariantSpelling(Word jsonWord)
    {
        bool hasNonVariantSpelling = true;

        List<string> glosses = new List<string>();

        jsonWord.senses?.ToList().ForEach(sense =>
        {
            if (sense.raw_glosses != null)
            {
                glosses.AddRange(sense.raw_glosses);
            }
        });

        int variantGlosses = glosses.Count(gloss =>
        {
            string lowerGloss = gloss.ToLower();
            return
                gloss.Contains("spelling") ||
                gloss.Contains("abbrev") ||
                gloss.Contains("alternative") ||
                gloss.Contains("alternate") ||
                gloss.Contains("variant") ||
                gloss.Contains("acryonm") ||
                gloss.Contains("initial");
        });

        if (variantGlosses == glosses.Count && glosses.Count > 0)
        {
            hasNonVariantSpelling = false;

            if (!_flaggedWords.ContainsKey("Probable Variant Spelling from Definitions"))
            {
                _flaggedWords["Probable Variant Spelling from Definitions"] = new List<FlaggedWord>();
            }

            _flaggedWords["Probable Variant Spelling from Definitions"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));
        }
        else if (variantGlosses > 0)
        {
            if (!_flaggedWords.ContainsKey("Possible Variant Spelling from Definitions"))
            {
                _flaggedWords["Possible Variant Spelling from Definitions"] = new List<FlaggedWord>();
            }

            _flaggedWords["Possible Variant Spelling from Definitions"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));
        }

        List<string> tags = new List<string>();

        jsonWord.senses?.ToList().ForEach(sense =>
        {
            if (sense.tags != null)
            {
                tags.AddRange(sense.tags);
            }
        });

        int variantTags = tags.Count(tag =>
        {
            string lowerTag = tag.ToLower();
            return
                tag.Contains("spelling") ||
                tag.Contains("abbrev") ||
                tag.Contains("alternative") ||
                tag.Contains("alternate") ||
                tag.Contains("variant") ||
                tag.Contains("acryonm") ||
                tag.Contains("initial");
        });

        if (variantTags == tags.Count && tags.Count > 0)
        {
            hasNonVariantSpelling = false;

            if (!_flaggedWords.ContainsKey("Probable Variant Spelling from Tags"))
            {
                _flaggedWords["Probable Variant Spelling from Tags"] = new List<FlaggedWord>();
            }

            _flaggedWords["Probable Variant Spelling from Tags"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));
        }
        else if (variantTags > 0)
        {
            if (!_flaggedWords.ContainsKey("Possible Variant Spelling from Tags"))
            {
                _flaggedWords["Possible Variant Spelling from Tags"] = new List<FlaggedWord>();
            }

            _flaggedWords["Possible Variant Spelling from Tags"].Add(new FlaggedWord(jsonWord.word, jsonWord.pos));
        }

        if (!hasNonVariantSpelling)
        {
            //Console.WriteLine($"{jsonWord.word} may be a variant spelling.");
        }

        return !hasNonVariantSpelling;
    }
}

class SecondPhaseParser
{
	Dictionary<string, SimpleWord>? _inputDict = new Dictionary<string, SimpleWord>();
	Dictionary<string, SimpleWord> _midDict = new Dictionary<string, SimpleWord>();
	Dictionary<string, SimpleWord> _outputDict = new Dictionary<string, SimpleWord>();

	Dictionary<string, SimpleWord> _misspelledOrAbbrevDict = new Dictionary<string, SimpleWord>();

	FileStream _stream = null;

	string _path = "firstpass.txt";

	public void Parse(string[] args)
	{
		try
		{
			_stream = File.OpenRead(ParserEntry.PREFIX + _path);
			StreamReader reader = new StreamReader(_stream);

			string dictText = reader.ReadToEnd();

			_inputDict = JsonSerializer.Deserialize<Dictionary<string, SimpleWord>>(dictText);

			if (_inputDict == null)
				return;

			// first step merge for case sensitivity

			foreach (var kvp in _inputDict)
			{
				string lowerKey = kvp.Key.ToLower();
				if (_midDict.TryGetValue(lowerKey, out SimpleWord? value))
				{
					Console.WriteLine("Deduped " + lowerKey);
					if (value.pos != null)
					{
						_midDict[lowerKey].pos.AddRange(kvp.Value.pos);
					}
					if (value.senses != null)
					{
						_midDict[lowerKey].senses.AddRange(kvp.Value.senses);
					}
				}
				else
				{
					_midDict[lowerKey] = kvp.Value;
					_midDict[lowerKey].word = _midDict[lowerKey].word.ToLower();
				}
			}

			// second step: remove misspellings and abbreviations

			foreach (var kvp in _midDict)
			{
				SimpleWord word = kvp.Value;
				if (word != null)
				{
					List<string> glosses = new List<string>();
					word.senses?.ForEach(sense =>
					{
						if (sense.raw_glosses != null)
						{
							glosses.AddRange(sense.raw_glosses);
						}
					});

					if (glosses.Count(gloss =>
					{
						string lowerGloss = gloss.ToLower();
						return !lowerGloss.Contains("misspell") && !lowerGloss.Contains("abbrev");
					}) > 0)
					{
						_outputDict[kvp.Key] = kvp.Value;
					}
					else
					{
						Console.WriteLine(kvp.Key + " might be only a misspelling or an abbreviation.");
						_misspelledOrAbbrevDict[kvp.Key] = kvp.Value;
					}
				}
			}

			// third step: deduplicate parts of speech

			foreach (var kvp in _outputDict)
			{
				if (kvp.Value != null && kvp.Value.pos != null)
				{
					kvp.Value.pos = kvp.Value.pos.Distinct().ToList();
				}
			}

			foreach (var kvp in _misspelledOrAbbrevDict)
			{
				if (kvp.Value != null && kvp.Value.pos != null)
				{
					kvp.Value.pos = kvp.Value.pos.Distinct().ToList();
				}
			}

			reader.Close();
			_stream.Close();

			FileStream misspelledStream = File.OpenWrite(ParserEntry.PREFIX + "misspelled.txt");
			StreamWriter misspelledWriter = new StreamWriter(misspelledStream);
			string misspelledJson = JsonSerializer.Serialize(_misspelledOrAbbrevDict);
			misspelledWriter.Write(misspelledJson);
			misspelledWriter.Close();
			misspelledStream.Close();

			FileStream secondPassStream = File.OpenWrite(ParserEntry.PREFIX + "secondpass.txt");
			StreamWriter secondPassWriter = new StreamWriter(secondPassStream);
			string secondPassJson = JsonSerializer.Serialize(_outputDict);
			secondPassWriter.Write(secondPassJson);
			secondPassWriter.Close();
			secondPassStream.Close();

			// next pass: flag derogatory/offensive/slur. Moved into separate file to copy-paste back in
			// second-to-last step: strip definitions. This is what we share with the public
			// last step: combine parts of speech and send to game for Odin Serialization
		}
		catch
		{

		}
	}
}

class ThirdPhaseParser
{
	Dictionary<string, SimpleWord>? _inputDict = new Dictionary<string, SimpleWord>();
	Dictionary<string, SimpleWord> _outputDict = new Dictionary<string, SimpleWord>();

	Dictionary<string, SimpleWord> _maybeOffensiveDict = new Dictionary<string, SimpleWord>();

	FileStream _stream = null;

	string _path = "secondpass.txt";

	public void Parse(string[] args)
	{
		try
		{
			_stream = File.OpenRead(ParserEntry.PREFIX + _path);
			StreamReader reader = new StreamReader(_stream);

			string dictText = reader.ReadToEnd();

			_inputDict = JsonSerializer.Deserialize<Dictionary<string, SimpleWord>>(dictText);

			if (_inputDict == null)
				return;

			// second step: filter out slurs, derogatory, and offensive

			foreach (var kvp in _inputDict)
			{
				SimpleWord word = kvp.Value;
				if (word != null)
				{
					List<string> tags = new List<string>();
					word.senses?.ForEach(sense =>
					{
						if (sense.tags != null)
						{
							tags.AddRange(sense.tags);
						}
					});

					List<string> glosses = new List<string>();
					word.senses?.ForEach(sense =>
					{
						if (sense.glosses != null)
						{
							glosses.AddRange(sense.glosses);
						}
					});

					if (tags.Count(tag =>
					{
						string lowerTag = tag.ToLower();
						return lowerTag.Contains("derogatory") || lowerTag.Contains("slur") || lowerTag.Contains("offensive") || lowerTag.Contains("ethnic");
					}) > 0 || glosses.Count (gloss =>
					{
						string lowerGloss = gloss.ToLower();
						return lowerGloss.Contains("derogatory") || lowerGloss.Contains("slur") || lowerGloss.Contains("offensive");
					}) > 0)
					{
						Console.WriteLine(kvp.Key + " might be offensive.");
						_maybeOffensiveDict[kvp.Key] = kvp.Value;
					}
					else
					{
						_outputDict[kvp.Key] = kvp.Value;
					}
				}
			}

			reader.Close();
			_stream.Close();

			FileStream maybeOffensiveStream = File.OpenWrite(ParserEntry.PREFIX + "maybe-offensive.txt");
			StreamWriter maybeOffensiveWriter = new StreamWriter(maybeOffensiveStream);
			string misspelledJson = JsonSerializer.Serialize(_maybeOffensiveDict);
			maybeOffensiveWriter.Write(misspelledJson);
			maybeOffensiveWriter.Close();
			maybeOffensiveStream.Close();

			FileStream thirdPassStream = File.OpenWrite(ParserEntry.PREFIX + "thirdpass.txt");
			StreamWriter thirdPassWriter = new StreamWriter(thirdPassStream);
			string thirdPassJson = JsonSerializer.Serialize(_outputDict);
			thirdPassWriter.Write(thirdPassJson);
			thirdPassWriter.Close();
			thirdPassStream.Close();

			// second-to-last step: strip definitions. This is what we share with the public
			// last step: combine parts of speech and send to game for Odin Serialization
		}
		catch
		{

		}
	}
}

class FourthPhaseParser
{
	Dictionary<string, SimpleWord>? _inputDict = new Dictionary<string, SimpleWord>();
	Dictionary<string, List<string>> _publicDict = new Dictionary<string, List<string>>();

	FileStream _stream = null;

	string _path = "thirdpass.txt";

	public void Parse(string[] args)
	{
		try
		{
			_stream = File.OpenRead(ParserEntry.PREFIX + _path);
			StreamReader reader = new StreamReader(_stream);

			string dictText = reader.ReadToEnd();

			_inputDict = JsonSerializer.Deserialize<Dictionary<string, SimpleWord>>(dictText);

			if (_inputDict == null)
				return;

			// second step: filter out slurs, derogatory, and offensive

			foreach (var kvp in _inputDict)
			{
				SimpleWord word = kvp.Value;
				if (word != null)
				{
					_publicDict[kvp.Key] = word.pos;
				}
			}

			reader.Close();
			_stream.Close();

			FileStream fourthPassStream = File.OpenWrite(ParserEntry.PREFIX + "fourthpass.txt");
			StreamWriter fourthPassWriter = new StreamWriter(fourthPassStream);
			string fourthPassJson = JsonSerializer.Serialize(_publicDict);
			fourthPassWriter.Write(fourthPassJson);
			fourthPassWriter.Close();
			fourthPassStream.Close();

			// last step: combine parts of speech and send to game for Odin Serialization
		}
		catch
		{

		}
	}
}