using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
internal sealed class _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__LinkRegex_9 : Regex
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
					int num2 = inputSpan.Slice(num).IndexOfAny('!', '[');
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
				ReadOnlySpan<char> span = inputSpan.Slice(num);
				if (!span.IsEmpty && span[0] == '!')
				{
					span = span.Slice(1);
					num++;
				}
				if (span.IsEmpty || span[0] != '[')
				{
					UncaptureUntil(0);
					return false;
				}
				num++;
				span = inputSpan.Slice(num);
				num2 = num;
				int num3 = span.IndexOf(']');
				if (num3 < 0)
				{
					num3 = span.Length;
				}
				span = span.Slice(num3);
				num += num3;
				Capture(1, num2, num);
				if (!span.StartsWith("](".AsSpan()))
				{
					UncaptureUntil(0);
					return false;
				}
				int num4 = span.Slice(2).IndexOf(')');
				if (num4 < 0)
				{
					num4 = span.Length - 2;
				}
				span = span.Slice(num4);
				num += num4;
				if ((uint)span.Length < 3u || span[2] != ')')
				{
					UncaptureUntil(0);
					return false;
				}
				Capture(0, start, runtextpos = num + 3);
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

	internal static readonly _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__LinkRegex_9 Instance = new _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__LinkRegex_9();

	private _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__LinkRegex_9()
	{
		pattern = "!?\\[([^\\]]*)\\]\\([^)]*\\)";
		roptions = RegexOptions.None;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
