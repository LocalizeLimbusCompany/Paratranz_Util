using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using SimpleJSON;

namespace LLC_Paratranz_Util;

public static class GTP
{
	public static void Call()
	{
		if (string.IsNullOrEmpty(Program.LocalizePath)) throw new NullReferenceException("LocalizePath is null");
		if (string.IsNullOrEmpty(Program.ParaPath))
			Program.ParaPath = new DirectoryInfo("./Localize").FullName;

		Program.LoadGitHubWroks(new DirectoryInfo(Program.LocalizePath + "/KR"), Program.KrDic);
		Program.LoadGitHubWroks(new DirectoryInfo(Program.LocalizePath + "/JP"), Program.JpDic);
		Program.LoadGitHubWroks(new DirectoryInfo(Program.LocalizePath + "/EN"), Program.EnDic);

		if (Directory.Exists(Program.ParaPath))
			Directory.Delete(Program.ParaPath, true);
		Directory.CreateDirectory(Program.ParaPath);

		foreach (var (krkey, kr) in Program.KrDic)
		{
			var isStory = krkey.StartsWith("\\StoryData");
			var en = Program.EnDic.GetValueOrDefault(krkey, kr);
			var jp = Program.JpDic.GetValueOrDefault(krkey, kr);
			JsonArray paratranzWrok = new();
			if (kr.Count == 0)
				continue;
			var krobjs = kr[0].AsArray;
			if (krobjs[0].AsObject.Dict.Count == 0)
				continue;
			Dictionary<string, JsonObject> endic = null;
			Dictionary<string, JsonObject> jpdic = null;
			JsonArray enobjs = null;
			JsonArray jpobjs = null;
			if (isStory)
			{
				enobjs = en[0].AsArray;
				jpobjs = jp[0].AsArray;
			}
			else
			{
				try
				{
					endic = en[0].AsArray.List.ToDictionaryEx(key => key[0].Value, value => value.AsObject);
					jpdic = jp[0].AsArray.List.ToDictionaryEx(key => key[0].Value, value => value.AsObject);
				}
				catch
				{
					endic = kr[0].AsArray.List.ToDictionaryEx(key => key[0].Value, value => value.AsObject);
					jpdic = endic;
				}
			}

			for (var i = 0; i < krobjs.Count; i++)
			{
				var krobj = krobjs[i].AsObject;
				string objectId = krobj[0];
				JsonObject enobj;
				JsonObject jpobj;
				if (krobj.Count < 1)
					continue;
				if (isStory)
				{
					if (objectId == "-1")
						continue;
					enobj = enobjs[i].AsObject;
					jpobj = jpobjs[i].AsObject;
				}
				else
				{
					enobj = endic.GetValueOrDefault(objectId, krobj);
					jpobj = jpdic.GetValueOrDefault(objectId, krobj);
				}

				foreach (var keyValue in krobj.Dict)
				{
					if (keyValue.Value.IsNumber) continue;
					JsonObject paratranzObject = new()
					{
						Dict =
						{
							["key"] = objectId + "-" + keyValue.Key
						}
					};
					if (keyValue.Key is "id" or "usage") continue;
					if (keyValue.Value.IsString)
					{
						string original = keyValue.Value;
						if (string.IsNullOrEmpty(original) || "-".Equals(original)) continue;

						paratranzObject.Dict["original"] = original;
						paratranzObject.Dict["context"] =
							$"EN :\n{enobj[keyValue.Key].Value}\nJP :\n{jpobj[keyValue.Key].Value}";
					}
					else if (keyValue.Value.IsArray)
					{
						var krps = Program.GetJsonPaths(JArray.Parse(keyValue.Value.ToString()));
						var enps = Program.GetJsonPaths(JArray.Parse(enobj[keyValue.Key].ToString()));
						var jpps = Program.GetJsonPaths(JArray.Parse(jpobj[keyValue.Key].ToString()));

						foreach (var paratranzObject1 in krps.Select(item => new JsonObject
						         {
							         Dict =
							         {
								         ["key"] = objectId + "-" + keyValue.Key + item.Key,
								         ["original"] = item.Value.ToString(),
								         ["context"] = $"EN :\n{enps[item.Key]}\nJP :\n{jpps[item.Key]}"
							         }
						         }))
							paratranzWrok.Add(paratranzObject1);
						continue;
					}

					paratranzWrok.Add(paratranzObject);
				}
			}

			var filePath = Program.ParaPath + krkey;
			var directoryPath = Path.GetDirectoryName(filePath);
			if (!Directory.Exists(directoryPath))
				Directory.CreateDirectory(directoryPath);
			File.WriteAllText(filePath, paratranzWrok.ToString(2));
		}
	}
}