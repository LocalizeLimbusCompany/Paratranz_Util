using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SimpleJSON;

namespace LLC_Paratranz_Util;

public static class PTG
{
	public static Dictionary<string, JsonArray> PtDic = [];

	public static void Call()
	{
		if (string.IsNullOrEmpty(Program.LocalizePath)) throw new NullReferenceException("LocalizePath is null");
		if (string.IsNullOrEmpty(Program.ParaPath))
			Program.ParaPath = new DirectoryInfo("../utf8/Localize").FullName;
		if (string.IsNullOrEmpty(Program.CustomLanguageName)) Program.CustomLanguageName = "LLC_zh-CN";
		Program.CustomLanguagePath = Path.Combine(Program.LocalizePath, Program.CustomLanguageName);

		Program.LoadGitHubWroks(new DirectoryInfo(Program.LocalizePath + "/KR"), Program.KrDic);
		Program.LoadParatranzWroks(new DirectoryInfo(Program.ParaPath), PtDic);

		if (Directory.Exists(Program.CustomLanguagePath))
			Directory.Delete(Program.CustomLanguagePath, true);
		Directory.CreateDirectory(Program.CustomLanguagePath);

		foreach (var ptKvs in PtDic)
		{
			var pt = ptKvs.Value.List.ToDictionary(key => key[0].Value, value => value.AsObject);
			if (!Program.KrDic.TryGetValue(ptKvs.Key, out var kr)) continue;
			var krobjs = kr[0].AsArray;
			for (var i = 0; i < krobjs.Count; i++)
			{
				var krobj = krobjs[i].AsObject;
				string objectId = ptKvs.Key.StartsWith("\\StoryData") ? i.ToString() : krobj[0];
				foreach (var keyValue in krobj.Dict.ToArray())
				{
					if (keyValue.Value.IsNumber || keyValue.Key is "id" or "model" or "usage") continue;
					if (keyValue.Value.IsString)
					{
						if (!pt.TryGetValue($"{objectId}-{keyValue.Key}", out var ptobj) ||
						    !ptobj.Dict.TryGetValue("translation", out var translation) ||
						    string.IsNullOrEmpty(translation))
							continue;
						krobj[keyValue.Key].Value = translation.Value.Replace("\\n", "\n");
					}
					else if (keyValue.Value.IsArray)
					{
						var token = JArray.Parse(keyValue.Value.ToString());
						var jps = Program.GetJsonPaths(token);
						foreach (var item in jps)
						{
							if (!pt.TryGetValue($"{objectId}-{keyValue.Key}{item.Key}", out var ptobj) ||
							    !ptobj.Dict.TryGetValue("translation", out var translation) ||
							    string.IsNullOrEmpty(translation))
								continue;
							item.Value.Replace(translation.Value.Replace("\\n", "\n"));
						}

						krobj.Dict[keyValue.Key] = Json.Parse(token.ToString());
					}
				}
			}

			var krjson = kr.ToString();
			var filePath = Program.CustomLanguagePath + ptKvs.Key;
			var directoryPath = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath!);
			File.WriteAllText(filePath, JObject.Parse(krjson).ToString());
		}
	}
}