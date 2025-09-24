/// <summary>
/// Phase 1, strip out non-EN words and extra metadata
/// </summary>

public class SimpleWord
{
	public string word { get; set; }
	public List<string> pos { get; set; }
	public List<Sense> senses { get; set; }

	public SimpleWord()
	{
		senses = new List<Sense>();
		pos = new List<string>();
	}
}

public class FlaggedWord
{
    public string word { get; set; }
    public string pos { get; set; }

    public FlaggedWord(string word, string pos)
    {
        this.word = word;
        this.pos = pos;
    }
}

public class DerivedWord
{
    public string word { get; set; }
    public string source { get; set; }

    public DerivedWord(string word, string source)
    {
        this.word = word;
        this.source = source;
    }
}

public class Word
{
	public Sense[] senses { get; set; }
	public string pos { get; set; }
	public Head_Templates[] head_templates { get; set; }
	public string[] categories { get; set; }
	public Form[] forms { get; set; }
	public Hyponym[] hyponyms { get; set; }
	public Derived[] derived { get; set; }
	public Translation[] translations { get; set; }
	public Sound[] sounds { get; set; }
	public int etymology_number { get; set; }
	public string etymology_text { get; set; }
	public Etymology_Templates[] etymology_templates { get; set; }
	public string word { get; set; }
	public string lang { get; set; }
	public string lang_code { get; set; }
}

public class Sense
{
	public Example[] examples { get; set; }
	public string[] wikidata { get; set; }
	public string[] senseid { get; set; }
	public string[][] links { get; set; }
	public string[] categories { get; set; }
	public string[] glosses { get; set; }
	public Synonym[] synonyms { get; set; }
	public string[] raw_glosses { get; set; }
	public string[] tags { get; set; }
	public string qualifier { get; set; }
	public string[] topics { get; set; }
}

public class Example
{
	public string text { get; set; }
	public string _ref { get; set; }
	public string type { get; set; }
	public int[][] bold_text_offsets { get; set; }
}

public class Synonym
{
	public string word { get; set; }
	public string source { get; set; }
	public string[] tags { get; set; }
}

public class Head_Templates
{
	public string name { get; set; }
	public Args args { get; set; }
	public string expansion { get; set; }
}

public class Args
{
}

public class Form
{
	public string form { get; set; }
	public string[] tags { get; set; }
}

public class Hyponym
{
	public string taxonomic { get; set; }
	public string word { get; set; }
	public string alt { get; set; }
	public string english { get; set; }
	public string translation { get; set; }
}

public class Derived
{
	public string word { get; set; }
	public string english { get; set; }
	public string translation { get; set; }
}

public class Translation
{
	public string lang { get; set; }
	public string code { get; set; }
	public string lang_code { get; set; }
	public string sense { get; set; }
	public string roman { get; set; }
	public string word { get; set; }
	public string[] tags { get; set; }
	public string alt { get; set; }
	public string note { get; set; }
}

public class Sound
{
	public string[] tags { get; set; }
	public string ipa { get; set; }
	public string enpr { get; set; }
	public string audio { get; set; }
	public string ogg_url { get; set; }
	public string mp3_url { get; set; }
	public string rhymes { get; set; }
	public string homophone { get; set; }
}

public class Etymology_Templates
{
	public string name { get; set; }
	public Args1 args { get; set; }
	public string expansion { get; set; }
}

public class Args1
{
	public string _1 { get; set; }
	public string _2 { get; set; }
	public string _3 { get; set; }
	public string sc { get; set; }
}
