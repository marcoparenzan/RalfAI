using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Text.RegularExpressions.Generated;

namespace RalfAI;

public static class MarkdownText
{
	[GeneratedRegex("```[a-zA-Z0-9]*\\n?")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex CodeFenceRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__CodeFenceRegex_0.Instance;
	}

	[GeneratedRegex("`([^`]*)`")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex InlineCodeRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__InlineCodeRegex_1.Instance;
	}

	[GeneratedRegex("(\\*\\*\\*|___)(.+?)\\1")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex BoldItalicRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldItalicRegex_2.Instance;
	}

	[GeneratedRegex("(\\*\\*|__)(.+?)\\1")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex BoldRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldRegex_3.Instance;
	}

	[GeneratedRegex("(?<!\\w)(\\*|_)(.+?)\\1(?!\\w)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex ItalicRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ItalicRegex_4.Instance;
	}

	[GeneratedRegex("^\\s{0,3}#{1,6}\\s+", RegexOptions.Multiline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex HeadingRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HeadingRegex_5.Instance;
	}

	[GeneratedRegex("^\\s{0,3}>\\s?", RegexOptions.Multiline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex BlockquoteRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BlockquoteRegex_6.Instance;
	}

	[GeneratedRegex("^\\s*([-*+]|\\d+\\.)\\s+", RegexOptions.Multiline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex ListMarkerRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ListMarkerRegex_7.Instance;
	}

	[GeneratedRegex("^\\s*([-*_]\\s?){3,}\\s*$", RegexOptions.Multiline)]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex HorizontalRuleRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HorizontalRuleRegex_8.Instance;
	}

	[GeneratedRegex("!?\\[([^\\]]*)\\]\\([^)]*\\)")]
	[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
	private static Regex LinkRegex()
	{
		return _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__LinkRegex_9.Instance;
	}

	public static string StripFormatting(string text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return text;
		}
		string input = text;
		input = CodeFenceRegex().Replace(input, "");
		input = LinkRegex().Replace(input, "$1");
		input = BoldItalicRegex().Replace(input, "$2");
		input = BoldRegex().Replace(input, "$2");
		input = ItalicRegex().Replace(input, "$2");
		input = InlineCodeRegex().Replace(input, "$1");
		input = HorizontalRuleRegex().Replace(input, "");
		input = HeadingRegex().Replace(input, "");
		input = BlockquoteRegex().Replace(input, "");
		input = ListMarkerRegex().Replace(input, "");
		return input.Trim();
	}
}
