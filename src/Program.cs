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
        static int ParatranzWrok_Path_Length;

        static readonly Error_logger logger = new("./Error.txt");
        public static void Main(string[] args)
        {
#if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler((object o, UnhandledExceptionEventArgs e) => { logger.LogError(o.ToString() + e.ToString()); });
            try
            {
#endif
            Localize_Path = new DirectoryInfo(File.ReadAllLines("./LLC_GitHubWrokLocalize_Path.txt")[0]).FullName;
            Localize_Path_Length = Localize_Path.Length + 3;
            ParatranzWrok_Path = new DirectoryInfo("./utf8/Localize").FullName;
            ParatranzWrok_Path_Length = ParatranzWrok_Path.Length;
            LoadGitHubWroks(new DirectoryInfo(Localize_Path + "/KR"), kr_dic);
            var RawNickNameObj = JSON.Parse(File.ReadAllText(Localize_Path + "/NickName.json")).AsObject;
            cn_dic["/RawNickName.json"] = RawNickNameObj;
            var ReadmeObj = JSON.Parse(File.ReadAllText(Localize_Path + "/Readme/Readme.json")).AsObject;
            cn_dic["/Readme/Readme.json"] = ReadmeObj;

            LoadParatranzWroks(new DirectoryInfo(ParatranzWrok_Path), pt_dic);
            ToGitHubWrok();
#if !DEBUG
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
            }
#endif
        }
        public static Dictionary<string, JSONObject> cn_dic = [];
        public static Dictionary<string, JSONObject> kr_dic = [];
        public static Dictionary<string, JSONArray> pt_dic = [];
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
        public static void LoadParatranzWroks(DirectoryInfo directory, Dictionary<string, JSONArray> dic)
        {
            foreach (FileInfo fileInfo in directory.GetFiles())
            {
                var value = File.ReadAllText(fileInfo.FullName);
                string fileName = fileInfo.DirectoryName.Remove(0, ParatranzWrok_Path_Length) + "/" + fileInfo.Name;
                dic[fileName] = JSON.Parse(value).AsArray;
            }
            foreach (DirectoryInfo directoryInfo in directory.GetDirectories())
                LoadParatranzWroks(directoryInfo, dic);
        }
        public static void ToGitHubWrok()
        {
            if (Directory.Exists(Localize_Path + "/CN"))
                Directory.Delete(Localize_Path + "/CN", true);
            Directory.CreateDirectory(Localize_Path + "/CN");
            kr_dic["/NickName.json"] = cn_dic["/RawNickName.json"];
            foreach (var pt_kvs in pt_dic)
            {
                var pt = pt_kvs.Value.List.ToDictionary(key => key[0].Value, value => value.AsObject);
                if (kr_dic.TryGetValue(pt_kvs.Key, out var kr))
                {
                    var krobjs = kr[0].AsArray;
                    for (int i = 0; i < krobjs.Count; i++)
                    {
                        var krobj = krobjs[i].AsObject;
                        string ObjectId = krobj[0];
                        foreach (var keyValue in krobj.Dict.ToArray())
                        {
                            if (!keyValue.Value.IsNumber && keyValue.Key != "id" && keyValue.Key != "model" && keyValue.Key != "usage")
                            {

                                if (keyValue.Value.IsString)
                                {
                                    if (!pt.TryGetValue(ObjectId + "-" + keyValue.Key, out var ptobj) || !ptobj.Dict.TryGetValue("translation", out var translation) || string.IsNullOrEmpty(translation))
                                        continue;
                                    krobj[keyValue.Key].Value = translation.Value.Replace("\\n", "\n");
                                }
                                else
                                {
                                    JArray token = JArray.Parse(keyValue.Value.ToString());
                                    var jps = GetJsonPaths(token);
                                    foreach (var item in jps)
                                    {
                                        if (!pt.TryGetValue(ObjectId + "-" + keyValue.Key + item.Key, out var ptobj) || !ptobj.Dict.TryGetValue("translation", out var translation) || string.IsNullOrEmpty(translation))
                                            continue;
                                        item.Value.Replace(translation.Value.Replace("\\n", "\n"));
                                    }
                                    krobj.Dict[keyValue.Key] = JSON.Parse(token.ToString());
                                }
                            }
                        }
                    }
                    string krjson = kr.ToString();
                    string filePath = Localize_Path + "/CN" + pt_kvs.Key;
                    string directoryPath = Path.GetDirectoryName(filePath);
                    if (!Directory.Exists(directoryPath))
                        Directory.CreateDirectory(directoryPath);
                    File.WriteAllText(filePath, JObject.Parse(krjson).ToString());
                }
            }
            var Special = pt_dic["/Special.json"].List.ToDictionary(key => key[0].Value, value => value.AsObject);

            string CHANGELOG = Special["更新记录"]["translation"].Value;
            string Parent = new DirectoryInfo(Localize_Path).Parent.FullName;
            File.WriteAllText(Parent + "/CHANGELOG.md", CHANGELOG.Replace("\\n", "\n"));
            var ReadmeObj = cn_dic["/Readme/Readme.json"];
            var noticeList = ReadmeObj[0].AsArray;
            ReadOnlySpan<char> ver = CHANGELOG.AsSpan(3, CHANGELOG.IndexOf("\\n") - 3);
            string LCB_LLCMod = File.ReadAllText(Parent + "/src/LCB_LLCMod.cs");
            int startIndex = LCB_LLCMod.IndexOf("VERSION = \"");
            int endIndex = LCB_LLCMod.IndexOf('"', startIndex + 11);
            LCB_LLCMod = LCB_LLCMod.Remove(startIndex + 11, endIndex - startIndex - 11).Insert(startIndex + 11, ver.ToString());
            File.WriteAllText(Parent + "/src/LCB_LLCMod.cs", LCB_LLCMod);
            string Readme_ver = string.Concat("Mod V", ver);
            var notice = noticeList[^2].AsObject;
            if (!notice[6].Value.Equals(Readme_ver))
            {
                noticeList[^1] = noticeList[^2].Clone();
                notice[0].AsInt += 1;
                notice[3] = DateTime.Now.ToString("yyyy-MM-dd") + "T08:00:00.000Z";
                notice[6] = Readme_ver;
            }
            notice[7] = "{\"list\":[{\"formatKey\":\"Text\",\"formatValue\":\"" + CHANGELOG + "\"}]}";
            File.WriteAllText(Localize_Path + "/Readme/Readme.json", ReadmeObj.ToString(2));
            File.WriteAllText(Localize_Path + "/Readme/LoadingTexts.md", Special["Loading"]["translation"].Value.Replace("\\n", "\n"));
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
    }
}
