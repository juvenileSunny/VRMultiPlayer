/*
 

                      _                      _  _           _                                       
                     (_ )                   ( )(_ )        ( )                                      
   __   _   _    _    | |  _   _    __     _| | | |    _ _ | |_     ___       ___    _     ___ ___  
 /'__`\( ) ( ) /'_`\  | | ( ) ( ) /'__`\ /'_` | | |  /'_` )| '_`\ /',__)    /'___) /'_`\ /' _ ` _ `\
(  ___/| \_/ |( (_) ) | | | \_/ |(  ___/( (_| | | | ( (_| || |_) )\__, \ _ ( (___ ( (_) )| ( ) ( ) |
`\____)`\___/'`\___/'(___)`\___/'`\____)`\__,_)(___)`\__,_)(_,__/'(____/(_)`\____)`\___/'(_) (_) (_)
                                                                                                    
                                                                                                    
    TTS-o-matic (c) 2025 by EvolvedLabs SAS - www.evolvedlabs.com
  
 */

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Linq;

namespace TTS_o_matic
{
    public class AssetDownloaderWindow : EditorWindow
    {
        private const string JsonUrl = "https://www.noiseomatic.com/ttsmodels/nomttsmodels.json";
        private const string StreamingAssetsPath = "Assets/StreamingAssets";

        private Vector2 scrollPos;
        private string searchTerm = "";

        private List<ModelAsset> assets = new List<ModelAsset>();
        private Dictionary<string, float> downloadProgress = new Dictionary<string, float>();
        private Dictionary<string, bool> isDownloading = new Dictionary<string, bool>();
        private Dictionary<string, bool> languageFoldouts = new Dictionary<string, bool>();

        [MenuItem("Tools/TTS-o-matic/Model Asset Downloader")]
        public static void ShowWindow()
        {
            GetWindow<AssetDownloaderWindow>("TTS Model Downloader");
        }

        private async void OnEnable()
        {
            await FetchAssetList();
        }

        private async Task FetchAssetList()
        {
            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(JsonUrl))
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                        await Task.Yield();

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError("Failed to download JSON: " + request.error);
                        return;
                    }

                    string json = request.downloadHandler.text;
                    assets = JsonUtilityWrapper.FromJsonArray<ModelAsset>(json);
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Error fetching asset list: " + ex.Message);
            }
        }

        private void OnGUI()
        {
            // Search bar
            EditorGUILayout.BeginHorizontal("box");
            GUILayout.Label("Search:", GUILayout.Width(50));
            searchTerm = EditorGUILayout.TextField(searchTerm);
            EditorGUILayout.EndHorizontal();

            if (assets == null || assets.Count == 0)
            {
                EditorGUILayout.LabelField("Loading asset list or no assets available...");
                return;
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            var filteredAssets = assets
                .Where(a => IsMatch(a, searchTerm))
                .GroupBy(a => a.language)
                .OrderBy(g => g.Key);

            foreach (var languageGroup in filteredAssets)
            {
                if (!languageFoldouts.ContainsKey(languageGroup.Key))
                    languageFoldouts[languageGroup.Key] = true;

                languageFoldouts[languageGroup.Key] = EditorGUILayout.Foldout(languageFoldouts[languageGroup.Key], "Language: "+languageGroup.Key, true);
                if (!languageFoldouts[languageGroup.Key]) continue;

                var sortedAssets = languageGroup.OrderBy(a => a.sex).ToList();

                foreach (var asset in sortedAssets)
                {
                    string extractPath = Path.Combine(StreamingAssetsPath, "ttsmodels");
                    extractPath = Path.Combine(extractPath, Path.GetFileNameWithoutExtension(asset.filename));
                    bool isDownloadingNow = isDownloading.TryGetValue(asset.filename, out bool downloading) && downloading;
                    bool isDownloaded = Directory.Exists(extractPath);

                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();

                    GUILayout.Label(asset.name, EditorStyles.boldLabel, GUILayout.Width(150));

                    string sex = "Male";
                    if (asset.sex == "F")
                        sex = "Female";

                    GUILayout.Label($"({sex})", EditorStyles.miniLabel, GUILayout.Width(50));

                    if (isDownloadingNow)
                    {
                        float progress = downloadProgress.ContainsKey(asset.filename) ? downloadProgress[asset.filename] : 0f;
                        Rect progressRect = GUILayoutUtility.GetRect(150, 16);
                        EditorGUI.ProgressBar(progressRect, progress, $"{Mathf.RoundToInt(progress * 100)}%");
                    }
                    else if (isDownloaded)
                    {
                        GUILayout.Label("✅ Downloaded", EditorStyles.miniLabel, GUILayout.Width(100));
                    }
                    else
                    {
                        if (GUILayout.Button("⬇ Download", GUILayout.Width(100)))
                        {
                            DownloadAndExtract(asset);
                        }
                    }

                    EditorGUILayout.EndHorizontal();

                    // Secondary info (always visible but compact)
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(20);
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Label($"Quality: {asset.quality}", EditorStyles.miniLabel, GUILayout.Width(120));
                    GUILayout.Label($"Size: {(int.Parse(asset.filesize) / 1024 / 1024)} MB", EditorStyles.miniLabel, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.EndVertical();
                    GUILayout.Space(4);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool IsMatch(ModelAsset asset, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;

            search = search.ToLower();
            return asset.name.ToLower().Contains(search) ||
                   asset.language.ToLower().Contains(search) ||
                   asset.quality.ToLower().Contains(search);
        }

        private async void DownloadAndExtract(ModelAsset asset)
        {
            if (!Directory.Exists(StreamingAssetsPath))
                Directory.CreateDirectory(StreamingAssetsPath);

            string tempZipPath = Path.Combine(Path.GetTempPath(), asset.filename);
            string extractPath = Path.Combine(StreamingAssetsPath, "ttsmodels" );

            if (!Directory.Exists(extractPath))
                Directory.CreateDirectory(extractPath);


            extractPath = Path.Combine(extractPath, Path.GetFileNameWithoutExtension(asset.filename));
            string url = $"https://noiseomatic.com/ttsmodels/{asset.filename}";

            isDownloading[asset.filename] = true;
            downloadProgress[asset.filename] = 0f;

            try
            {
                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.downloadHandler = new DownloadHandlerFile(tempZipPath);
                    var operation = request.SendWebRequest();

                    while (!operation.isDone)
                    {
                        downloadProgress[asset.filename] = request.downloadProgress;
                        Repaint();
                        await Task.Yield();
                    }

                    if (request.result != UnityWebRequest.Result.Success)
                    {
                        Debug.LogError($"Download failed: {request.error}");
                        EditorUtility.DisplayDialog("Error", $"Download failed: {request.error}", "OK");
                        isDownloading[asset.filename] = false;
                        return;
                    }
                }

                if (!Directory.Exists(extractPath))
                    Directory.CreateDirectory(extractPath);

                ZipFile.ExtractToDirectory(tempZipPath, extractPath, true);
                File.Delete(tempZipPath);
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Download Complete", $"{asset.name} has been extracted to StreamingAssets.", "OK");
            }
            catch (System.Exception ex)
            {
                Debug.LogError("Download or extraction failed: " + ex.Message);
                EditorUtility.DisplayDialog("Error", $"Failed to download or extract asset: {ex.Message}", "OK");
            }
            finally
            {
                isDownloading[asset.filename] = false;
                downloadProgress[asset.filename] = 0f;
                Repaint();
            }
        }

        [System.Serializable]
        public class ModelAsset
        {
            public string sex;
            public string language;
            public string name;
            public string quality;
            public string filename;
            public string filesize;
        }

        public static class JsonUtilityWrapper
        {
            [System.Serializable]
            private class Wrapper<T>
            {
                public List<T> items;
            }

            public static List<T> FromJsonArray<T>(string json)
            {
                string wrapped = "{\"items\":" + json + "}";
                return JsonUtility.FromJson<Wrapper<T>>(wrapped).items;
            }
        }
    }
}
