using System.Text.Json;

class ParserEntry
{
	public static string PREFIX = "../../../";

	static FirstPhaseParser parserPhase1 = new FirstPhaseParser();
	static SecondPhaseParser parserPhase2 = new SecondPhaseParser();
	static ThirdPhaseParser parserPhase3 = new ThirdPhaseParser();
	static FourthPhaseParser parserPhase4 = new FourthPhaseParser();

	static void Main(string[] args)
	{
		//parserPhase1.Parse(args);
		//parserPhase2.Parse(args);
		parserPhase4.Parse(args);
	}
}
class FirstPhaseParser
{
	string _fulldictPath = "raw-wiktextract-data.jsonl";
	
	FileStream stream = null;
	
	FileStream listStream = null;
	
	List<string> wordList = new List<string>();
	
	Dictionary<string, SimpleWord> _wordDict = new Dictionary<string, SimpleWord>();
	
	HashSet<string> _partsOfSpeech = new HashSet<string>();
	
	HashSet<string> _otherWords = new HashSet<string>();
	
	public void Parse(string[] args)
	{
		try
		{

			//string path = ParserEntry.PREFIX + "twl06.txt";
			listStream = File.OpenRead(ParserEntry.PREFIX + "all_words.txt");

			if (listStream.CanRead)
			{
				StreamReader listReader = new StreamReader(listStream);

				string word = null;

				while ((word = listReader.ReadLine()) != null)
				{
					wordList.Add(word);
				}

				listReader.Close();
			}

			listStream.Close();
			
			try
			{
				//stream = File.OpenRead(ParserEntry.PREFIX + _fulldictPath);
				stream = File.OpenRead("/run/media/system/F/wiktionary/raw-wiktextract-data.jsonl");
	
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
	
						// store other words that weren't expected
	
						if (jsonWord.word.Contains('-') ||
							jsonWord.word.Contains(' ') ||
							jsonWord.word.Contains('&') ||
							jsonWord.word.Contains('+') ||
							jsonWord.word.Contains('.') ||
							jsonWord.word.Contains(',') ||
							jsonWord.word.Contains('\'') ||
							jsonWord.word.Contains('"') ||
							(jsonWord.pos != null && jsonWord.pos == "name"))
						{
							_otherWords.Add(jsonWord.word);
							continue;
						}

						if (!wordList.Contains(jsonWord.word))
						{
							continue;
						}
						
						// store all found parts of speech
	
						_partsOfSpeech.Add(jsonWord.pos);
	
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
	
							//check wordlist here
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
	
					File.WriteAllLines(ParserEntry.PREFIX + "names.txt", _otherWords.ToList());
	
					FileStream firstFilterStream = File.OpenWrite(ParserEntry.PREFIX + "firstpass.txt");
					StreamWriter firstFilterWriter = new StreamWriter(firstFilterStream);
	
					string strJson = JsonSerializer.Serialize(_wordDict);
	
					firstFilterWriter.Write(strJson);
					firstFilterWriter.Close();
	
					File.WriteAllLines(ParserEntry.PREFIX + "partsofspeech.txt", _partsOfSpeech.ToList());
				}
			}
			catch
			{
				Console.WriteLine("File failed to load, possibly missing?");
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
			Console.WriteLine("Word File failed to load, possibly missing?");
		}
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
			/*
			foreach (var kvp in _midDict)
			{
				SimpleWord word = kvp.Value;
				if (word != null)
				{
					List<string> glosses = new List<string>();
					word.senses?.ForEach(sense =>
					{
						if (sense.glosses != null)
						{
							glosses.AddRange(sense.glosses);
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
			*/
			// third step: deduplicate parts of speech

			foreach (var kvp in _midDict)
			{
				if (kvp.Value != null && kvp.Value.pos != null)
				{
					kvp.Value.pos = kvp.Value.pos.Distinct().ToList();
				}
			}
/*
			foreach (var kvp in _misspelledOrAbbrevDict)
			{
				if (kvp.Value != null && kvp.Value.pos != null)
				{
					kvp.Value.pos = kvp.Value.pos.Distinct().ToList();
				}
			}
*/
			reader.Close();
			_stream.Close();

			/*
			FileStream misspelledStream = File.OpenWrite(ParserEntry.PREFIX + "misspelled.txt");
			StreamWriter misspelledWriter = new StreamWriter(misspelledStream);
			string misspelledJson = JsonSerializer.Serialize(_misspelledOrAbbrevDict);
			misspelledWriter.Write(misspelledJson);
			misspelledWriter.Close();
			misspelledStream.Close();

			*/
			FileStream secondPassStream = File.OpenWrite(ParserEntry.PREFIX + "secondpass.txt");
			StreamWriter secondPassWriter = new StreamWriter(secondPassStream);
			string secondPassJson = JsonSerializer.Serialize(_midDict);
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
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}
	}
}