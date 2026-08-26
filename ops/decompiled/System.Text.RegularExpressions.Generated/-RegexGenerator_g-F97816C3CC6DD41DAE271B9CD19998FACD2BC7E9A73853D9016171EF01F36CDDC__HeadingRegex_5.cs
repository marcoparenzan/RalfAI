using System.CodeDom.Compiler;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
internal sealed class _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HeadingRegex_5 : Regex
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
				if (num <= inputSpan.Length - 2)
				{
					if (num <= 0 || inputSpan[num - 1] == '\n')
					{
						goto IL_0085;
					}
					int num2 = inputSpan.Slice(num).IndexOf('\n');
					if ((uint)num2 <= inputSpan.Length - num - 1)
					{
						num += num2 + 1;
						if (num <= inputSpan.Length - 2)
						{
							goto IL_0085;
						}
					}
				}
				goto IL_00b0;
				IL_0085:
				int num3 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_3EFF8D20A696162D70580D8E3B8BACB00C2D80E8EFFB25C70545B36F32AE229A();
				if (num3 >= 0)
				{
					runtextpos = num + num3;
					return true;
				}
				goto IL_00b0;
				IL_00b0:
				runtextpos = inputSpan.Length;
				return false;
			}

			private bool TryMatchAtCurrentPosition(ReadOnlySpan<char> inputSpan)
			{
				int num = runtextpos;
				int start = num;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num > 0 && inputSpan[num - 1] != '\n')
				{
					return false;
				}
				int i;
				for (i = 0; i < 3 && (uint)i < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[i]); i++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				int j;
				for (j = 0; j < 6 && (uint)j < (uint)readOnlySpan.Length && readOnlySpan[j] == '#'; j++)
				{
				}
				if (j == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(j);
				num += j;
				int k;
				for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[k]); k++)
				{
				}
				if (k == 0)
				{
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(k);
				Capture(0, start, runtextpos = num + k);
				return true;
			}
		}

		protected override RegexRunner CreateInstance()
		{
			return new Runner();
		}
	}

	internal static readonly _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HeadingRegex_5 Instance = new _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HeadingRegex_5();

	private _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HeadingRegex_5()
	{
		pattern = "^\\s{0,3}#{1,6}\\s+";
		roptions = RegexOptions.Multiline;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 1;
	}
}
