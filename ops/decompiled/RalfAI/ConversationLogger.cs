using System;
using System.Collections.Generic;
using System.IO;

namespace RalfAI;

public sealed class ConversationLogger
{
	private readonly string _logPath;

	private bool _headerWritten;

	public string LogPath => _logPath;

	public ConversationLogger(string targetFolder)
	{
		string text = Path.Combine(targetFolder, ".ralfai");
		Directory.CreateDirectory(text);
		_logPath = Path.Combine(text, "session.md");
		_headerWritten = File.Exists(_logPath);
	}

	public void LogUser(string message)
	{
		Append("USER", message);
	}

	public void LogAssistant(string? thinking, string message)
	{
		string content = (string.IsNullOrWhiteSpace(thinking) ? message : ("<!-- thinking\n" + thinking + "\n-->\n\n" + message));
		Append("AGENT", content);
	}

	public void LogTool(string name, string args, string result)
	{
		Append("TOOL — " + name, "args: " + args + "\nresult: " + result);
	}

	private void Append(string header, string content)
	{
		if (!_headerWritten)
		{
			File.WriteAllText(_logPath, $"# RalfAI Session\n\n> Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n\n---\n\n");
			_headerWritten = true;
		}
		string value = DateTime.Now.ToString("HH:mm:ss");
		string contents = $"### {header} — {value}\n\n{content}\n\n---\n\n";
		File.AppendAllText(_logPath, contents);
	}

	public List<(string Role, string Content)> LoadExistingHistory()
	{
		if (!File.Exists(_logPath))
		{
			return new List<(string, string)>();
		}
		List<(string, string)> list = new List<(string, string)>();
		string[] array = File.ReadAllLines(_logPath);
		string text = null;
		List<string> list2 = new List<string>();
		string[] array2 = array;
		foreach (string text2 in array2)
		{
			if (text2.StartsWith("### USER") || text2.StartsWith("### AGENT") || text2.StartsWith("### TOOL"))
			{
				FlushBuffer(list, text, list2);
				list2.Clear();
				text = (text2.StartsWith("### USER") ? "user" : (text2.StartsWith("### AGENT") ? "assistant" : "tool"));
			}
			else if (text2 == "---" && text != null)
			{
				FlushBuffer(list, text, list2);
				list2.Clear();
				text = null;
			}
			else if (text != null)
			{
				list2.Add(text2);
			}
		}
		FlushBuffer(list, text, list2);
		return list;
	}

	private static void FlushBuffer(List<(string, string)> list, string? role, List<string> lines)
	{
		if (role != null && !(role == "tool"))
		{
			List<string> values = ((role == "assistant") ? StripThinkingBlock(lines) : lines);
			string text = string.Join("\n", values).Trim();
			if (!string.IsNullOrWhiteSpace(text))
			{
				list.Add((role, text));
			}
		}
	}

	private static List<string> StripThinkingBlock(List<string> lines)
	{
		List<string> list = new List<string>();
		bool flag = false;
		foreach (string line in lines)
		{
			if (!flag && line.StartsWith("<!-- thinking", StringComparison.Ordinal))
			{
				flag = true;
			}
			else if (flag)
			{
				if (line == "-->")
				{
					flag = false;
				}
			}
			else
			{
				list.Add(line);
			}
		}
		return list;
	}
}
