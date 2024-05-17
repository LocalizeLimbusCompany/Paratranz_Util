using Newtonsoft.Json.Linq;
using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace LLC_Paratranz_Util
{
    public static class Program
    {
        static string Localize_Path;
        static string ParatranzWrok_Path;
        static int Localize_Path_Length;
        public static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((object o, UnhandledExceptionEventArgs e) => { File.WriteAllText("./Error.txt", o.ToString() + e.ToString()); });
            try
            {
                Localize_Path = new DirectoryInfo(File.ReadAllLines("./LLC_GitHubWrokLocalize_Path.txt")[0]).FullName;
                ParatranzWrok_Path = new DirectoryInfo("./Localize").FullName;
#else
            Localize_Path = new DirectoryInfo(File.ReadAllLines("./LLC_GitHubWrokLocalize_Path.txt")[0]).FullName;
            ParatranzWrok_Path = new DirectoryInfo("./Localize").FullName;
#endif
            Localize_Path_Length = Localize_Path.Length + 3;
            LoadGitHubWroks(new DirectoryInfo(Localize_Path + "/KR"), kr_dic);
            LoadGitHubWroks(new DirectoryInfo(Localize_Path + "/JP"), jp_dic);
            LoadGitHubWroks(new DirectoryInfo(Localize_Path + "/EN"), en_dic);
            var RawNickNameObj = JSON.Parse(File.ReadAllText(Localize_Path + "/NickName.json")).AsObject;
            cn_dic["/RawNickName.json"] = RawNickNameObj;
            var NickNameObj = JSON.Parse(File.ReadAllText(Localize_Path + "/CN/NickName.json")).AsObject;
            cn_dic["/NickName.json"] = NickNameObj;

            ToParatranzWrok();

#if !DEBUG
            }
            catch (Exception ex)
            {
                File.WriteAllText("./Error.txt", ex.ToString());
            }
#endif
        }
        public static Dictionary<string, JSONObject> cn_dic = [];
        public static Dictionary<string, JSONObject> en_dic = [];
        public static Dictionary<string, JSONObject> jp_dic = [];
        public static Dictionary<string, JSONObject> kr_dic = [];
        public static void LoadGitHubWroks(DirectoryInfo directory, Dictionary<string, JSONObject> dic)
        {
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                var value = File.ReadAllText(fileInfo.FullName);
                string fileName = fileInfo.DirectoryName.Remove(0, Localize_Path_Length) + "/" + fileInfo.Name;
                dic[fileName] = JSON.Parse(value).AsObject;
            }
            foreach (DirectoryInfo directoryInfo in directory.GetDirectories())
                LoadGitHubWroks(directoryInfo, dic);
        }
        public static void ToParatranzWrokNickName()
        {
            var RawNickNames = cn_dic["/RawNickName.json"][0].AsArray;
            JSONArray PT_NickName = new();
            foreach (JSONObject rnnobj in RawNickNames.List.Cast<JSONObject>())
            {
                var NameKey = rnnobj[0].Value;
                bool kr2has = rnnobj.Dict.TryGetValue("nickName", out var krnickName);
                bool enhas = rnnobj.Dict.TryGetValue("enname", out var enname);
                bool en2has = rnnobj.Dict.TryGetValue("enNickName", out var ennickName);
                bool jphas = rnnobj.Dict.TryGetValue("jpname", out var jpname);
                bool jp2has = rnnobj.Dict.TryGetValue("jpNickName", out var jpnickName);

                JSONObject krnameobj = new();
                krnameobj.Dict["key"] = NameKey + "-krname";
                krnameobj.Dict["original"] = NameKey;
                krnameobj.Dict["context"] = "EN :\n" + (enhas ? enname.Value : string.Empty) + "\nJP :\n" + (jphas ? jpname.Value : string.Empty);
                PT_NickName.Add(krnameobj);
                if (kr2has)
                {
                    if (!string.IsNullOrEmpty(krnickName))
                    {
                        JSONObject nickNameobj = new();
                        nickNameobj.Dict["key"] = NameKey + "-nickName";
                        nickNameobj.Dict["original"] = krnickName;
                        nickNameobj.Dict["context"] = "EN :\n" + (en2has ? ennickName.Value : string.Empty) + "\nJP :\n" + (jp2has ? jpnickName.Value : string.Empty);
                        PT_NickName.Add(nickNameobj);
                    }
                }
            }
            File.WriteAllText(ParatranzWrok_Path + "/NickName.json", PT_NickName.ToString(2));
        }
        public static Dictionary<TKey, TElement> ToDictionaryEX<TSource, TKey, TElement>(
            this IEnumerable<TSource> source,
            Func<TSource, TKey> keySelector,
            Func<TSource, TElement> elementSelector)
        {
            var dictionary = new Dictionary<TKey, TElement>();
            foreach (var item in source)
                dictionary[keySelector(item)] = elementSelector(item);
            return dictionary;
        }
        public static void ToParatranzWrokNone(Dictionary<string, JSONObject> NickNames)
        {
            foreach (var kr_kvp in kr_dic)
            {
                var kr = kr_kvp.Value;
                string kr_kvp_key = kr_kvp.Key;
                bool isStory = kr_kvp_key.StartsWith("\\StoryData");
                cn_dic.TryGetValue(kr_kvp_key, out var cn);
                if (!en_dic.TryGetValue(kr_kvp_key, out var en))
                    en = kr;
                if (!jp_dic.TryGetValue(kr_kvp_key, out var jp))
                    jp = kr;
                JSONArray ParatranzWrok = new();
                if (kr.Count == 0)
                    continue;
                var krobjs = kr[0].AsArray;
                if (krobjs[0].AsObject.Dict.Count == 0)
                    continue;
                Dictionary<string, JSONObject> endic = null;
                Dictionary<string, JSONObject> jpdic = null;
                JSONArray enobjs = null;
                JSONArray jpobjs = null;
                if (isStory)
                {
                    enobjs = en[0].AsArray;
                    jpobjs = jp[0].AsArray;
                }
                else
                {
                    try
                    {
                        endic = en[0].AsArray.List.ToDictionaryEX(key => key[0].Value, value => value.AsObject);
                        jpdic = jp[0].AsArray.List.ToDictionaryEX(key => key[0].Value, value => value.AsObject);
                    }
                    catch
                    {
                        endic = kr[0].AsArray.List.ToDictionaryEX(key => key[0].Value, value => value.AsObject);
                        jpdic = endic;
                    }
                }
                for (int i = 0; i < krobjs.Count; i++)
                {
                    var krobj = krobjs[i].AsObject;
                    string ObjectId = krobj[0];
                    JSONObject enobj = null;
                    JSONObject jpobj = null;
                    if (krobj.Count < 1)
                        continue;
                    if (isStory)
                    {
                        if (ObjectId == "-1")
                            continue;
                        enobj = enobjs[i].AsObject;
                        jpobj = jpobjs[i].AsObject;
                    }
                    else
                    {
                        if (!endic.TryGetValue(ObjectId, out enobj))
                            enobj = krobj;
                        if (!jpdic.TryGetValue(ObjectId, out jpobj))
                            jpobj = krobj;
                    }
                    foreach (var keyValue in krobj.Dict)
                    {
                        if (!keyValue.Value.IsNumber)
                        {
                            JSONObject ParatranzObject = new();
                            ParatranzObject.Dict["key"] = ObjectId + "-" + keyValue.Key;
                            if (keyValue.Key == "model")
                            {
                                if (NickNames.TryGetValue(keyValue.Value.Value, out var NickName))
                                {
                                    ParatranzObject.Dict["original"] = NickName[0];
                                    ParatranzObject.Dict["translation"] = NickName[1];
                                    ParatranzObject.Dict["context"] = "这是当前说话人物的默认名称,仅供参考\nEN :\n" + NickName[3] + "\nJP :\n" + NickName[2];
                                }
                                else
                                {
                                    ParatranzObject.Dict["original"] = keyValue.Value.Value;
                                    ParatranzObject.Dict["context"] = "这是当前说话人物的默认名称,仅供参考\n但是,令人震惊的是,此版本并没有相关翻译,请前往NickName条目查看相关内容";
                                }
                                ParatranzWrok.Add(ParatranzObject);
                            }
                            else if (keyValue.Key != "id" && keyValue.Key != "usage")
                            {
                                if (keyValue.Value.IsString)
                                {
                                    string original = keyValue.Value;
                                    if (string.IsNullOrEmpty(original) || "-".Equals(original))
                                    {
                                        continue;
                                    }
                                    else
                                    {
                                        ParatranzObject.Dict["original"] = original;
                                        ParatranzObject.Dict["context"] = "EN :\n" + enobj[keyValue.Key].Value + "\nJP :\n" + jpobj[keyValue.Key].Value;
                                    }
                                }
                                else
                                {
                                    var krps = GetJsonPaths(JArray.Parse(keyValue.Value.ToString()));
                                    var enps = GetJsonPaths(JArray.Parse(enobj[keyValue.Key].ToString()));
                                    var jpps = GetJsonPaths(JArray.Parse(jpobj[keyValue.Key].ToString()));

                                    foreach (var item in krps)
                                    {
                                        JSONObject ParatranzObject1 = new();
                                        ParatranzObject1.Dict["key"] = ObjectId + "-" + keyValue.Key + item.Key;
                                        ParatranzObject1.Dict["original"] = item.Value.ToString();
                                        ParatranzObject1.Dict["context"] = "EN :\n" + enps[item.Key] + "\nJP :\n" + jpps[item.Key];
                                        ParatranzWrok.Add(ParatranzObject1);
                                    }
                                    continue;
                                }
                                ParatranzWrok.Add(ParatranzObject);
                            }
                        }
                    }
                }
                string filePath = ParatranzWrok_Path + kr_kvp_key;
                string directoryPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);
                File.WriteAllText(filePath, ParatranzWrok.ToString(2));
            }
        }
        public static Dictionary<string, JToken> GetJsonPaths(JToken token, string currentPath = "$")
        {
            var paths = new Dictionary<string, JToken>();
            switch (token)
            {
                case JObject obj when obj.Count > 0:
                    {
                        foreach (var childPath in from property in obj.Properties()
                                                  let path = $"{currentPath}.{property.Name}"
                                                  from childPath in GetJsonPaths(property.Value, path)
                                                  select childPath)
                        {
                            paths[childPath.Key] = childPath.Value;
                        }
                        break;
                    }
                case JArray array when array.Count > 0:
                    {
                        for (int i = 0; i < array.Count; i++)
                        {
                            foreach (var childPath in GetJsonPaths(array[i], $"{currentPath}[{i}]"))
                            {
                                paths[childPath.Key] = childPath.Value;
                            }
                        }
                        break;
                    }
                default:
                    if (!IsEmpty(token))
                    {
                        paths[currentPath] = token;
                    }
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
        public static void ToParatranzWrok()
        {
            if (Directory.Exists(ParatranzWrok_Path))
                Directory.Delete(ParatranzWrok_Path, true);
            Directory.CreateDirectory(ParatranzWrok_Path);

            ToParatranzWrokNickName();
            ToParatranzWrokNone(cn_dic["/NickName.json"][0].AsArray.List.ToDictionary(key => key[0].Value, value => value.AsObject));
        }
    }
}
