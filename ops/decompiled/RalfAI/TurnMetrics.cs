using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Configuration;

namespace RalfAI;

public static class TurnMetrics
{
	public static TurnUsage? Read(object response, IConfiguration configuration, string provider, string modelId)
	{
		object obj = FindUsage(response, 0, new HashSet<object>(ReferenceEqualityComparer.Instance));
		if (obj == null)
		{
			return null;
		}
		long? num = ReadLong(obj, "InputTokenCount", "InputTokens", "PromptTokens", "PromptTokenCount");
		long? num2 = ReadLong(obj, "OutputTokenCount", "OutputTokens", "CompletionTokens", "CompletionTokenCount");
		long? totalTokens = ReadLong(obj, "TotalTokenCount", "TotalTokens") ?? ((num.HasValue && num2.HasValue) ? (num + num2) : ((long?)null));
		if (!num.HasValue && !num2.HasValue && !totalTokens.HasValue)
		{
			return null;
		}
		IConfigurationSection section = configuration.GetSection("Pricing:" + provider + ":" + modelId);
		decimal? num3 = ReadDecimal(section["InputUsdPerMillionTokens"]);
		decimal? num4 = ReadDecimal(section["OutputUsdPerMillionTokens"]);
		decimal? estimatedCostUsd = ((num.HasValue && num2.HasValue && num3.HasValue && num4.HasValue) ? new decimal?((decimal)num.Value * num3.Value / 1000000m + (decimal)num2.Value * num4.Value / 1000000m) : ((decimal?)null));
		return new TurnUsage(num, num2, totalTokens, estimatedCostUsd);
	}

	private static object? FindUsage(object? value, int depth, HashSet<object> visited)
	{
		if (value == null || depth > 4 || value is string || !visited.Add(value))
		{
			return null;
		}
		Type type = value.GetType();
		string[] array = new string[3] { "Usage", "UsageDetails", "TokenUsage" };
		foreach (string name in array)
		{
			object obj = type.GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.GetValue(value);
			if (obj != null)
			{
				return obj;
			}
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetIndexParameters().Length != 0 || propertyInfo.Name == "RawRepresentation")
			{
				continue;
			}
			object value2;
			try
			{
				value2 = propertyInfo.GetValue(value);
			}
			catch
			{
				continue;
			}
			if (value2 == null)
			{
				continue;
			}
			if (value2 is IEnumerable enumerable && !(enumerable is string))
			{
				foreach (object item in enumerable.Cast<object>().Take(20))
				{
					object obj3 = FindUsage(item, depth + 1, visited);
					if (obj3 != null)
					{
						return obj3;
					}
				}
			}
			else
			{
				if (propertyInfo.PropertyType.IsPrimitive)
				{
					continue;
				}
				string? text = propertyInfo.PropertyType.Namespace;
				if (text != null && text.StartsWith("Microsoft", StringComparison.Ordinal))
				{
					object obj4 = FindUsage(value2, depth + 1, visited);
					if (obj4 != null)
					{
						return obj4;
					}
				}
			}
		}
		return null;
	}

	private static long? ReadLong(object usage, params string[] names)
	{
		foreach (string name in names)
		{
			object obj = usage.GetType().GetProperty(name, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public)?.GetValue(usage);
			if (obj != null && long.TryParse(obj.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return result;
			}
		}
		return null;
	}

	private static decimal? ReadDecimal(string? value)
	{
		decimal result;
		return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result) ? new decimal?(result) : ((decimal?)null);
	}
}
