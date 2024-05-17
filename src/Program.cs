using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SimpleJSON;

namespace LLC_Paratranz_Util;

public static class Program
{
	public static string LocalizePath;
	public static string ParaPath;
	public static string CustomLanguageName;
	public static string CustomLanguagePath;
	public static int LocalizePathLength;
	public static Dictionary<string, JsonObject> KrDic = [];

	private static readonly Logger Logger = new("./Error.txt");

	public static void Main(string[] args)
	{
#if !DEBUG
		AppDomain.CurrentDomain.UnhandledException += (o, e) => { Logger.Log(o + e.ToString()); };
#endif
		try
		{
			foreach (var arg in args)
				if (arg.StartsWith("-localize-path=", StringComparison.InvariantCultureIgnoreCase))
				{
					LocalizePath = new DirectoryInfo(arg[(arg.IndexOf('=') + 1)..].Trim('"')).FullName;
					LocalizePathLength = LocalizePath?.Length + 3 ?? 0;
				}
				else if (arg.StartsWith("-para-path=", StringComparison.InvariantCultureIgnoreCase))
				{
					ParaPath = new DirectoryInfo(arg[(arg.IndexOf('=') + 1)..].Trim('"')).FullName;
				}
				else if (arg.StartsWith("-custom-language-name=", StringComparison.InvariantCultureIgnoreCase))
				{
					CustomLanguageName = arg[(arg.IndexOf('=') + 1)..].Trim('"');
				}
				else if ("GTP".Equals(arg, StringComparison.InvariantCultureIgnoreCase))
				{
					GTP.Call();
				}
				else if ("PTG".Equals(arg, StringComparison.InvariantCultureIgnoreCase))
				{
					PTG.Call();
				}
		}
		catch (Exception ex)
		{
			Logger.Log(ex.ToString());
		}

		Logger.Dispose();
	}

	public static void LoadGitHubWroks(DirectoryInfo directory, Dictionary<string, JsonObject> dic)
	{
		foreach (var fileInfo in directory.GetFiles())
		{
			var value = File.ReadAllText(fileInfo.FullName);
			var fileName = $"{fileInfo.DirectoryName!.Remove(0, LocalizePathLength)}/{fileInfo.Name}";
			dic[fileName] = Json.Parse(value).AsObject;
		}

		foreach (var directoryInfo in directory.GetDirectories())
			LoadGitHubWroks(directoryInfo, dic);
	}

	public static void LoadParatranzWroks(DirectoryInfo directory, Dictionary<string, JsonArray> dic)
	{
		foreach (var fileInfo in directory.GetFiles())
		{
			var value = File.ReadAllText(fileInfo.FullName);
			var fileName = $"{fileInfo.DirectoryName!.Remove(0, ParaPath.Length)}/{fileInfo.Name}";
			dic[fileName] = Json.Parse(value).AsArray;
		}

		foreach (var directoryInfo in directory.GetDirectories())
			LoadParatranzWroks(directoryInfo, dic);
	}

	public static Dictionary<TKey, TElement> ToDictionaryEx<TSource, TKey, TElement>(
		this IEnumerable<TSource> source,
		Func<TSource, TKey> keySelector,
		Func<TSource, TElement> elementSelector)
	{
		var dictionary = new Dictionary<TKey, TElement>();
		foreach (var item in source)
			dictionary[keySelector(item)] = elementSelector(item);
		return dictionary;
	}

	public static Dictionary<string, JToken> GetJsonPaths(JToken token, string currentPath = "$")
	{
		var paths = new Dictionary<string, JToken>();
		switch (token)
		{
			case JObject { Count: > 0 } obj:
			{
				foreach (var childPath in from property in obj.Properties()
				         let path = $"{currentPath}.{property.Name}"
				         from childPath in GetJsonPaths(property.Value, path)
				         select childPath)
					paths[childPath.Key] = childPath.Value;
				break;
			}
			case JArray { Count: > 0 } array:
			{
				for (var i = 0; i < array.Count; i++)
					foreach (var childPath in GetJsonPaths(array[i], $"{currentPath}[{i}]"))
						paths[childPath.Key] = childPath.Value;

				break;
			}
			default:
				if (!IsEmpty(token)) paths[currentPath] = token;
				break;
		}

		return paths;
	}

	public static bool IsEmpty(JToken token)
	{
		return token.Type switch
		{
			JTokenType.Null => true,
			JTokenType.String => token.ToString() == string.Empty,
			_ => !token.HasValues
		};
	}
}