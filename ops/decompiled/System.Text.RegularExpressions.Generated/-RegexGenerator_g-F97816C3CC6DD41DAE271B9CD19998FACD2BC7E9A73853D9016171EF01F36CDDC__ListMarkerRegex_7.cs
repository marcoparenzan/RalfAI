using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
internal sealed class _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ListMarkerRegex_7 : Regex
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
				int num3 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_6479C023D753664A17E26CF70D50E1D2DDA2FE84CA33DF23BA4D362FEB3DD5C3();
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
				int num2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				ReadOnlySpan<char> readOnlySpan = inputSpan.Slice(num);
				if (num > 0 && inputSpan[num - 1] != '\n')
				{
					UncaptureUntil(0);
					return false;
				}
				int i;
				for (i = 0; (uint)i < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[i]); i++)
				{
				}
				readOnlySpan = readOnlySpan.Slice(i);
				num += i;
				num5 = num;
				num4 = num;
				num3 = Crawlpos();
				char c;
				if (readOnlySpan.IsEmpty || ((((c = readOnlySpan[0]) | 1) != 43) & (c != '-')))
				{
					goto IL_00e6;
				}
				num2 = 0;
				num++;
				readOnlySpan = inputSpan.Slice(num);
				goto IL_01db;
				IL_01db:
				while (true)
				{
					Capture(1, num5, num);
					int j;
					for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
					{
					}
					if (j == 0)
					{
						if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
						{
							CheckTimeout();
						}
						int num6 = num2;
						int num7 = num6;
						if (num7 != 0)
						{
							if (num7 != 1)
							{
								continue;
							}
							UncaptureUntil(0);
							return false;
						}
						break;
					}
					readOnlySpan = readOnlySpan.Slice(j);
					Capture(0, start, runtextpos = num + j);
					return true;
				}
				goto IL_00e6;
				IL_00e6:
				num = num4;
				readOnlySpan = inputSpan.Slice(num);
				UncaptureUntil(num3);
				int k;
				for (k = 0; (uint)k < (uint)readOnlySpan.Length && char.IsDigit(readOnlySpan[k]); k++)
				{
				}
				if (k == 0)
				{
					UncaptureUntil(0);
					return false;
				}
				readOnlySpan = readOnlySpan.Slice(k);
				num += k;
				if (readOnlySpan.IsEmpty || readOnlySpan[0] != '.')
				{
					UncaptureUntil(0);
					return false;
				}
				num2 = 1;
				num++;
				readOnlySpan = inputSpan.Slice(num);
				goto IL_01db;
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

	internal static readonly _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ListMarkerRegex_7 Instance = new _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ListMarkerRegex_7();

	private _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__ListMarkerRegex_7()
	{
		pattern = "^\\s*([-*+]|\\d+\\.)\\s+";
		roptions = RegexOptions.Multiline;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
