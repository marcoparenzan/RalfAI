using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RalfAI.Plugins;

public sealed class FileSystemPlugin(string rootFolder)
{
	[Description("Create or overwrite a file in the target project folder. Parent directories are created automatically.")]
	public Task<string> WriteFile([Description("Relative path from project root, e.g. 'src/MyApp/Program.cs'")] string relativePath, [Description("Complete file content to write")] string content)
	{
		string path = ResolveSafe(relativePath);
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		File.WriteAllText(path, content, Encoding.UTF8);
		return Task.FromResult($"OK — written {relativePath} ({content.Length} chars)");
	}

	[Description("Read the full content of a file in the target project folder.")]
	public Task<string> ReadFile([Description("Relative path from project root")] string relativePath)
	{
		string path = ResolveSafe(relativePath);
		if (!File.Exists(path))
		{
			return Task.FromResult("ERROR: file not found — " + relativePath);
		}
		return Task.FromResult(File.ReadAllText(path));
	}

	[Description("List all files and directories inside the target project folder (recursive).")]
	public Task<string> ListFiles([Description("Relative subdirectory to list, or empty string for the project root")] string subdirectory = "")
	{
		string path = (string.IsNullOrWhiteSpace(subdirectory) ? rootFolder : ResolveSafe(subdirectory));
		if (!Directory.Exists(path))
		{
			return Task.FromResult("ERROR: directory not found — " + subdirectory);
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (string item in from e in Directory.EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)
			orderby e
			select e)
		{
			string relativePath = Path.GetRelativePath(rootFolder, item);
			stringBuilder.AppendLine(Directory.Exists(item) ? ("[DIR]  " + relativePath) : ("[FILE] " + relativePath));
		}
		return Task.FromResult((stringBuilder.Length == 0) ? "(empty folder)" : stringBuilder.ToString().TrimEnd());
	}

	[Description("Delete a file from the target project folder.")]
	public Task<string> DeleteFile([Description("Relative path from project root")] string relativePath)
	{
		string path = ResolveSafe(relativePath);
		if (!File.Exists(path))
		{
			return Task.FromResult("File not found — nothing deleted: " + relativePath);
		}
		File.Delete(path);
		return Task.FromResult("OK — deleted " + relativePath);
	}

	private string ResolveSafe(string relativePath)
	{
		string fullPath = Path.GetFullPath(Path.Combine(rootFolder, relativePath));
		if (!fullPath.StartsWith(rootFolder, StringComparison.OrdinalIgnoreCase))
		{
			throw new UnauthorizedAccessException("Path traversal blocked: " + relativePath);
		}
		return fullPath;
	}
}
