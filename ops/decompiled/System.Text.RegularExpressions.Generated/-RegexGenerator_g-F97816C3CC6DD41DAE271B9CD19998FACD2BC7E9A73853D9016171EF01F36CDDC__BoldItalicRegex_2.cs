using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
internal sealed class _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldItalicRegex_2 : Regex
{
	private sealed class RunnerFactory : RegexRunnerFactory
	{
		private sealed class Runner : RegexRunner
		{
			protected override void Scan(ReadOnlySpan<char> inputSpan)
			{
				while (TryFindNextPossibleStartingPosition(inputSpan) && !TryMatchAtCurrentPosition(inputSpan) && runtextpos != inputSpan.Length)
				{
					runtextpos++;
					if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
				}
			}

			private bool TryFindNextPossibleStartingPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				if (num <= inputSpan.Length - 4)
				{
					int num2 = inputSpan.Slice(num).IndexOfAny(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_indexOfAnyStrings_OrdinalIgnoreCase_95C07221C305490CA5D357E1A182E699F0123B3059968AE3F78FF94ED758C94E);
					if (num2 >= 0)
					{
						runtextpos = num + num2;
						return true;
					}
				}
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int num6 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				num2 = num;
				if (readOnlySpan.IsEmpty)
				{
					UncaptureUntil(0);
					return false;
				}
				switch (readOnlySpan[0])
				{
				case '*':
					if (!readOnlySpan.Slice(1).StartsWith("**".AsSpan()))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 3;
					readOnlySpan = inputSpan.Slice(num);
					break;
				case '_':
					if (!readOnlySpan.Slice(1).StartsWith("__".AsSpan()))
					{
						UncaptureUntil(0);
						return false;
					}
					num += 3;
					readOnlySpan = inputSpan.Slice(num);
					break;
				default:
					UncaptureUntil(0);
					return false;
				}
				Capture(1, num2, num);
				num3 = num;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] == '\n')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				readOnlySpan = inputSpan.Slice(num);
				num5 = num;
				while (true)
				{
					num4 = Crawlpos();
					Capture(2, num3, num);
					if (IsMatched(1))
					{
						num6 = MatchLength(1);
						if (readOnlySpan.Length >= num6 && inputSpan.Slice(MatchIndex(1), num6).SequenceEqual(readOnlySpan.Slice(0, num6)))
						{
							break;
						}
					}
					UncaptureUntil(num4);
					if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
					{
						CheckTimeout();
					}
					num = num5;
					readOnlySpan = inputSpan.Slice(num);
					if (readOnlySpan.IsEmpty || readOnlySpan[0] == '\n')
					{
						UncaptureUntil(0);
						return false;
					}
					num++;
					readOnlySpan = inputSpan.Slice(num);
					num5 = num;
				}
				num += num6;
				readOnlySpan = inputSpan.Slice(num);
				runtextpos = num;
				Capture(0, start, num);
				return true;
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				void UncaptureUntil(int capturePosition)
				{
					while (Crawlpos() > capturePosition)
					{
						Uncapture();
					}
				}
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldItalicRegex_2 Instance = new _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldItalicRegex_2();

	private _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__BoldItalicRegex_2()
	{
		pattern = "(\\*\\*\\*|___)(.+?)\\1";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 3;
	}
}
