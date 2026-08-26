using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace System.Text.RegularExpressions.Generated;

[GeneratedCode("System.Text.RegularExpressions.Generator", "10.0.14.27113")]
internal sealed class _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HorizontalRuleRegex_8 : Regex
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
				if (num <= inputSpan.Length - 3)
				{
					if (num <= 0 || inputSpan[num - 1] == '\n')
					{
						goto IL_0085;
					}
					int num2 = inputSpan.Slice(num).IndexOf('\n');
					if ((uint)num2 <= inputSpan.Length - num - 1)
					{
						num += num2 + 1;
						if (num <= inputSpan.Length - 3)
						{
							goto IL_0085;
						}
					}
				}
				goto IL_00b0;
				IL_0085:
				int num3 = inputSpan.Slice(num).IndexOfNonAsciiOrAny_109316C0A762494C975623F554F2D41AA8A94881C103C9E1B2D678DAE54267F1();
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
				int arg = 0;
				int arg2 = 0;
				int num3 = 0;
				int num4 = 0;
				int num5 = 0;
				int pos = 0;
				int num6 = 0;
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
				num6 = pos;
				num5 = 0;
				while (true)
				{
					_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.StackPush(ref runstack, ref pos, Crawlpos(), num);
					num5++;
					int num7 = num;
					char c;
					if (!readOnlySpan.IsEmpty && !(((c = readOnlySpan[0]) != '*') & (c != '-') & (c != '_')))
					{
						num++;
						readOnlySpan = inputSpan.Slice(num);
						arg = num;
						if (!readOnlySpan.IsEmpty && char.IsWhiteSpace(readOnlySpan[0]))
						{
							readOnlySpan = readOnlySpan.Slice(1);
							num++;
						}
						arg2 = num;
					}
					else
					{
						do
						{
							if (--num5 < 0)
							{
								UncaptureUntil(0);
								return false;
							}
							num = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							readOnlySpan = inputSpan.Slice(num);
							if (num5 == 0)
							{
								UncaptureUntil(0);
								return false;
							}
							if (num5 >= 3)
							{
								num3 = num;
								int j;
								for (j = 0; (uint)j < (uint)readOnlySpan.Length && char.IsWhiteSpace(readOnlySpan[j]); j++)
								{
								}
								readOnlySpan = readOnlySpan.Slice(j);
								num += j;
								num4 = num;
								while (true)
								{
									num2 = Crawlpos();
									if ((uint)num < (uint)inputSpan.Length && inputSpan[num] != '\n')
									{
										UncaptureUntil(num2);
										if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
										{
											CheckTimeout();
										}
										if (num3 >= num4)
										{
											break;
										}
										num = --num4;
										readOnlySpan = inputSpan.Slice(num);
										continue;
									}
									runtextpos = num;
									Capture(0, start, num);
									return true;
								}
								if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
								{
									CheckTimeout();
								}
								if (num5 == 0)
								{
									UncaptureUntil(0);
									return false;
								}
							}
							num7 = runstack[--pos];
							UncaptureUntil(runstack[--pos]);
							_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.StackPop(runstack, ref pos, out arg2, out arg);
							if (_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_hasTimeout)
							{
								CheckTimeout();
							}
						}
						while (arg >= arg2);
						num = --arg2;
						readOnlySpan = inputSpan.Slice(num);
					}
					_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.StackPush(ref runstack, ref pos, arg, arg2, Crawlpos());
					Capture(1, num7, num);
					_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.StackPush(ref runstack, ref pos, num7);
				}
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

	internal static readonly _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HorizontalRuleRegex_8 Instance = new _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HorizontalRuleRegex_8();

	private _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__HorizontalRuleRegex_8()
	{
		pattern = "^\\s*([-*_]\\s?){3,}\\s*$";
		roptions = RegexOptions.Multiline;
		Regex.ValidateMatchTimeout(_003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout);
		internalMatchTimeout = _003CRegexGenerator_g_003EF97816C3CC6DD41DAE271B9CD19998FACD2BC7E9A73853D9016171EF01F36CDDC__Utilities.s_defaultTimeout;
		factory = new RunnerFactory();
		capsize = 2;
	}
}
