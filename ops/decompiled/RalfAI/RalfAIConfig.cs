using System;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace RalfAI;

public static class RalfAIConfig
{
	public static IConfiguration Load(string? explicitConfigPath = null)
	{
		string text = explicitConfigPath ?? Environment.GetEnvironmentVariable("RALFAI_CONFIG_PATH");
		ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
		if (!string.IsNullOrWhiteSpace(text))
		{
			configurationBuilder.AddJsonFile(Path.GetFullPath(text), optional: false, reloadOnChange: false);
		}
		else
		{
			configurationBuilder.SetBasePath(AppContext.BaseDirectory).AddJsonFile("appsettings.json", optional: false, reloadOnChange: false);
		}
		return configurationBuilder.AddEnvironmentVariables("RALFAI_").Build();
	}
}
