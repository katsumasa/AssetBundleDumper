using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UTJ.UnityCommandLineTools;
using System;


namespace UTJ.UnityAssetBundleDumper.Editor
{
    public class PPtrInfo
    {
        public int m_FileID;
        public long m_PathID;

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_FileID);
            binaryWriter.Write(m_PathID);
        }
        
        public void Deserialize(BinaryReader binaryReader)
        {
            m_FileID = binaryReader.ReadInt32();
            m_PathID = binaryReader.ReadInt64();
        }
    }


    public class AssetBundeInfo
    {
        public string m_Name;
        public PPtrInfo[] m_Preloads;
        public AssetInfo[] m_AssetInfos;

        public AssetBundeInfo()
        {
            m_Preloads = new PPtrInfo[0];
            m_AssetInfos = new AssetInfo[0];
        }


        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_Name);
            binaryWriter.Write(m_Preloads.Length);
            for(var i = 0; i < m_Preloads.Length; i++)
            {
                m_Preloads[i].Serialize(binaryWriter);
            }
            binaryWriter.Write(m_AssetInfos.Length);
            for(var i = 0; i < m_AssetInfos.Length; i++)
            {
                m_AssetInfos[i].Serialize(binaryWriter);
            }
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            var m_Name = binaryReader.ReadString();
            var len = binaryReader.ReadInt32();
            m_Preloads = new PPtrInfo[len];
            for(var i = 0; i < len; i++)
            {
                m_Preloads[i] = new PPtrInfo();
                m_Preloads[i].Deserialize(binaryReader);
            }
            len = binaryReader.ReadInt32();
            m_AssetInfos = new AssetInfo[len];
            for(var i = 0; i < len; i++) 
            {
                m_AssetInfos[i] = new AssetInfo();
                m_AssetInfos[i].Deserialize(binaryReader);
            }
        }
    }



    public class AssetInfo
    {
        long m_ID;
        int m_classID;
        string m_objectName;
        public string m_Name;
        public PPtrInfo[] m_PPtrInfos;

        public AssetInfo() { }

        public AssetInfo(long ID, int classID, string objectName)
        {
            m_ID = ID;
            m_classID = classID;
            m_objectName = objectName;
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_ID);
            binaryWriter.Write(m_classID);
            binaryWriter.Write(m_objectName);
            binaryWriter.Write(m_Name);
            binaryWriter.Write(m_PPtrInfos.Length);
            for(var i = 0; i < m_PPtrInfos.Length; i++)
            {
                m_PPtrInfos[i].Serialize(binaryWriter);
            }
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            m_ID = binaryReader.ReadInt64();
            m_classID = binaryReader.ReadInt32();
            m_objectName = binaryReader.ReadString();
            m_Name = binaryReader.ReadString();
            var len = binaryReader.ReadInt32();
            m_PPtrInfos = new PPtrInfo[len];
            for(var i = 0; i < len; i++)
            {
                m_PPtrInfos[i] = new PPtrInfo();
                m_PPtrInfos[i].Deserialize(binaryReader);
            }
        }

    }

    // AssetBundleの依存関係を表す為のClass
    public class HashTree
    {
        public int depth { get { return m_Depth; } }
        public string hash { get { return m_Hash; } }
        public string[] children { get { return m_Children; } }

        int m_Depth;
        string m_Hash;
        string[] m_Children;
        public HashTree(int depth,string hash,string[] children)
        {
            m_Depth = depth;
            m_Hash = hash;
            m_Children = children;
        }        
    }


    [System.Serializable]
    public class AssetBundleDumpData
    {
        readonly string m_Versions = "v.0.0.2";

        [SerializeField] public string m_AssetBundleRootFolder;
        [SerializeField] public string m_AssetBundleExtentions = "*.";
        [SerializeField] public string[] m_AssetBundleHashes;
        [SerializeField] public Dictionary<string, string> m_Hash2AssetBundleFilePaths;
        [SerializeField] public Dictionary<string, string> m_AssetBundleFilePath2Hashes;
        [SerializeField] public Dictionary<string, string> m_Hash2DumpFilePaths;
        [SerializeField] public Dictionary<string, AssetBundeInfo> m_Hash2AssetBundleBundeInfo;


        public AssetBundleDumpData()
        {
            m_AssetBundleRootFolder = string.Empty;
            m_AssetBundleExtentions = "*.";
            if (m_AssetBundleHashes == null)
            {
                m_AssetBundleHashes = new string[] { string.Empty };
            }
            if (m_Hash2AssetBundleFilePaths == null)
            {
                m_Hash2AssetBundleFilePaths = new Dictionary<string, string>();
            }
            if (m_AssetBundleFilePath2Hashes == null)
            {
                m_AssetBundleFilePath2Hashes = new Dictionary<string, string>();
            }
            if (m_Hash2DumpFilePaths == null)
            {
                m_Hash2DumpFilePaths = new Dictionary<string, string>();
            }
            if(m_Hash2AssetBundleBundeInfo == null)
            {
                m_Hash2AssetBundleBundeInfo = new Dictionary<string, AssetBundeInfo>();
            }
        }

        public void Serialize(string fpath)
        {
            using (var fs = new FileStream(fpath, FileMode.Create))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    bw.Write(m_Versions);


                    bw.Write(m_AssetBundleRootFolder);
                    bw.Write(m_AssetBundleExtentions);

                    bw.Write(m_AssetBundleHashes.Length);
                    for (var i = 0; i < m_AssetBundleHashes.Length; i++)
                    {
                        bw.Write(m_AssetBundleHashes[i]);
                    }

                    bw.Write(m_Hash2AssetBundleFilePaths.Count);
                    foreach (var kv in m_Hash2AssetBundleFilePaths)
                    {
                        bw.Write(kv.Key);
                        bw.Write(kv.Value);
                    }

                    bw.Write(m_AssetBundleFilePath2Hashes.Count);
                    foreach (var kv in m_AssetBundleFilePath2Hashes)
                    {
                        bw.Write(kv.Key);
                        bw.Write(kv.Value);
                    }

                    bw.Write(m_Hash2DumpFilePaths.Count);
                    foreach (var kv in m_Hash2DumpFilePaths)
                    {
                        bw.Write(kv.Key);
                        bw.Write(kv.Value);
                    }
                    bw.Write(m_Hash2AssetBundleBundeInfo.Count);
                    foreach(var kv in m_Hash2AssetBundleBundeInfo)
                    {
                        bw.Write(kv.Key);
                        kv.Value.Serialize(bw);
                    }
                }
            }                        
        }

        public bool Deserialize(string fpath)
        {
            using (var fs = new FileStream(fpath, FileMode.Open))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    var versions = br.ReadString();
                    if(versions != m_Versions)
                    {
                        Debug.LogError("");                        
                        return false;
                    }

                    m_AssetBundleRootFolder = br.ReadString();
                    m_AssetBundleExtentions = br.ReadString();
                    
                    var len = br.ReadInt32();
                    m_AssetBundleHashes = new string[len];
                    for(var i = 0; i < len; i++)
                    {
                        m_AssetBundleHashes[i] = br.ReadString();
                    }

                    len = br.ReadInt32();
                    m_Hash2AssetBundleFilePaths = new Dictionary<string, string>();
                    for(var i = 0; i < len; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadString();
                        m_Hash2AssetBundleFilePaths.Add(key, value);
                    }

                    len = br.ReadInt32();
                    m_AssetBundleFilePath2Hashes = new Dictionary<string, string>();
                    for (var i = 0; i < len; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadString();
                        m_AssetBundleFilePath2Hashes.Add(key, value);
                    }

                    len = br.ReadInt32();
                    m_Hash2DumpFilePaths = new Dictionary<string, string>();
                    for (var i = 0; i < len; i++)
                    {
                        var key = br.ReadString();
                        var value = br.ReadString();
                        m_Hash2DumpFilePaths.Add(key, value);
                    }
                    len = br.ReadInt32();
                    m_Hash2AssetBundleBundeInfo = new Dictionary<string, AssetBundeInfo>();
                    for(var i = 0; i < len; i++)
                    {
                        var key = br.ReadString();
                        var value = new AssetBundeInfo();
                        value.Deserialize(br);
                        m_Hash2AssetBundleBundeInfo.Add(key, value);
                    }
                }
            }
            return true;
        }
    }

    public class UnityAssetBundleDumperEditorWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent AssetBundleRootFolder = new GUIContent("AssetBundle Root Folder", "AssetBundle Root Folder Location");
            public static readonly GUIContent Browse = new GUIContent("Browse");
            public static readonly GUIContent AssetBundleExtentions = new GUIContent("Search Pattern", "Specifies the filename pattern for the AssetBundle. The default value is \"*.\"and this is the pattern when there is no extension. To specify multiple patterns, separate them with ';'. (example: \"*.;*.txt\")");
            public static readonly GUIContent CreateDB = new GUIContent("Dump", "Dump all AssetBundles.");
            public static readonly GUIContent DeleteDB = new GUIContent("Delete Dump Cache","Delete AssetBundle Dump Data");
            public static readonly GUIContent SelectAssetBundle = new GUIContent("Target AssetBundle");            
            public static readonly GUIContent Hash = new GUIContent("Hash", "This is the AssetBundle Hash, but you can also select an AssetBundle from the Hash.");
            public static readonly GUIContent AssetBundleDependency = new GUIContent("AssetBundle Dependency");
            public static readonly GUIContent CheckDependency = new GUIContent("Check Dependency");
            public static readonly GUIContent DependencyTreeView = new GUIContent("Dependency Tree", "Tree display of AssetBundle dependencies");
            public static readonly GUIContent DependencyListView = new GUIContent("Dependency List", "List of dependent AssetBundles");
            public static readonly GUIContent Select = new GUIContent("Select","Select AssetBundle");
        }

        static string m_WorkFolder;
        static string m_DataBaseFilePath;
        static string m_CasheFolder;

        static UnityAssetBundleDumperEditorWindow m_Instance;

        AssetBundleDumpData m_AssetBundleDumpData;
        AssetBundleDumpData assetBundleDumpData
        {
            get {
                if (m_AssetBundleDumpData == null)
                {
                    m_AssetBundleDumpData = new AssetBundleDumpData();
                }
                return m_AssetBundleDumpData;
            }
        }

        string m_AssetBundleRootFolder 
        {
            get { return assetBundleDumpData.m_AssetBundleRootFolder; }
            set { assetBundleDumpData.m_AssetBundleRootFolder = value; }
        }

        string m_AssetBundleExtentions
        {
            get { return assetBundleDumpData.m_AssetBundleExtentions; }
            set { assetBundleDumpData.m_AssetBundleExtentions = value; }
        }

        string[] m_AssetBundleHashes
        {
            get { return assetBundleDumpData.m_AssetBundleHashes; }
            set
            {
                assetBundleDumpData.m_AssetBundleHashes = value;
            }
        }
        Dictionary<string, string> m_Hash2AssetBundleFilePaths
        {
            get { return assetBundleDumpData.m_Hash2AssetBundleFilePaths; }
            set
            {
                assetBundleDumpData.m_Hash2AssetBundleFilePaths = value;
            }
        }
        Dictionary<string, string> m_AssetBundleFilePath2Hashes
        {
            get { return assetBundleDumpData.m_AssetBundleFilePath2Hashes; }
            set
            {
                assetBundleDumpData.m_AssetBundleFilePath2Hashes = value;
            }
        }
        Dictionary<string, string> m_Hash2DumpFilePaths
        {
            get { return assetBundleDumpData.m_Hash2DumpFilePaths; }
            set { assetBundleDumpData.m_Hash2DumpFilePaths = value; }
        }

        int m_HashIndex;       
        string m_AssetBundleFilePath = string.Empty;
        string m_DependencyListText;                
        Vector2 m_DependencyTreeScroll;
        Vector2 m_DependencyListScroll;

        [SerializeField] TreeViewState m_DependencyTreeViewState;
        DependencyTreeView m_DependencyTreeView;


        [MenuItem("Window/UTJ/UnityAssetBundleDumper")]
        public static void Open()
        {
            m_Instance = EditorWindow.GetWindow(typeof(UnityAssetBundleDumperEditorWindow)) as UnityAssetBundleDumperEditorWindow;
            m_Instance.titleContent.text = "UnityAssetBundleDumper";
        }

        private void OnEnable()
        {
            m_Instance = EditorWindow.GetWindow(typeof(UnityAssetBundleDumperEditorWindow)) as UnityAssetBundleDumperEditorWindow;
            m_Instance.titleContent.text = "UnityAssetBundleDumper";
            if (m_Instance.m_AssetBundleRootFolder == null)
            {
                m_Instance.m_AssetBundleRootFolder = Application.dataPath;
            }
            m_WorkFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "UnityAssetBundleDumper");
            m_DataBaseFilePath = Path.Combine(m_WorkFolder, "db");
            m_CasheFolder = Path.Combine(m_WorkFolder, "caches");

            if (File.Exists(m_DataBaseFilePath))
            {
                if(assetBundleDumpData.Deserialize(m_DataBaseFilePath) == false)
                {
                    DeleteDirectoryRecursive(m_WorkFolder);
                }
            }

            if(m_DependencyTreeViewState == null)
            {
                m_DependencyTreeViewState = new TreeViewState();
            }
            m_DependencyTreeView = new DependencyTreeView(m_DependencyTreeViewState);
        }

        private void OnDisable()
        {            
        }


        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("AssetBunde DataBase"));            
            EditorGUILayout.EndHorizontal();

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Styles.AssetBundleRootFolder);
                EditorGUILayout.TextField(m_AssetBundleRootFolder);
                if (GUILayout.Button(Styles.Browse))
                {
                    m_AssetBundleRootFolder = EditorUtility.OpenFolderPanel("Set AssetBundle Root Folder Location", m_AssetBundleRootFolder, "");
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Styles.AssetBundleExtentions);
                m_AssetBundleExtentions = EditorGUILayout.TextField(m_AssetBundleExtentions);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(Styles.CreateDB))
                {
                    CreateDB();
                    AnalyzeDumpFiles();
                }
                if (GUILayout.Button(Styles.DeleteDB))
                {
                    if (Directory.Exists(m_WorkFolder))
                    {
                        DeleteDirectoryRecursive(m_WorkFolder);
                    }
                    m_AssetBundleDumpData = new AssetBundleDumpData();
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Separator();

            EditorGUILayout.LabelField(Styles.AssetBundleDependency);

            using (new EditorGUI.IndentLevelScope())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PrefixLabel(Styles.SelectAssetBundle);
                EditorGUILayout.TextField(m_AssetBundleFilePath);

                var oldHashIndex = m_HashIndex;
                if (GUILayout.Button(Styles.Select))
                {
                    BrowseAssetBundleRootFolder();
                }
                EditorGUILayout.PrefixLabel(Styles.Hash);
                if (m_AssetBundleHashes == null || m_AssetBundleHashes.Length == 0)
                {
                    m_AssetBundleHashes = new string[] { "" };
                }
                
                m_HashIndex = System.Math.Min(m_HashIndex, m_AssetBundleHashes.Length - 1);
                m_HashIndex = System.Math.Max(m_HashIndex, 0);

                var hash = m_AssetBundleHashes[m_HashIndex];
                string filePath;
                if (m_Hash2AssetBundleFilePaths.TryGetValue(hash, out filePath))
                {
                    m_AssetBundleFilePath = filePath;
                }
                if (m_HashIndex != oldHashIndex)
                {
                    m_DependencyTreeView.Rebuild(assetBundleDumpData, m_AssetBundleHashes[m_HashIndex]);
                    using (StringWriter sw = new StringWriter())
                    {
                        foreach (var dependency in m_DependencyTreeView.DependencyFileList)
                        {                            
                            sw.WriteLine(dependency);
                        }
                        m_DependencyListText = sw.ToString();
                    }

                }

                EditorGUI.BeginChangeCheck();
                m_HashIndex = EditorGUILayout.Popup(m_HashIndex, m_AssetBundleHashes);
                if (EditorGUI.EndChangeCheck())
                {
                    // 依存関係のTree表示をビルド
                    m_DependencyTreeView.Rebuild(assetBundleDumpData, m_AssetBundleHashes[m_HashIndex]);
                    using (StringWriter sw = new StringWriter())
                    {
                        foreach (var dependency in m_DependencyTreeView.DependencyFileList)
                        {
                            sw.WriteLine(dependency);
                        }
                        m_DependencyListText = sw.ToString();
                    }
                }
                EditorGUILayout.EndHorizontal();
                
                EditorGUILayout.BeginHorizontal();
                {
                    // 依存関係のTree表示
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField(Styles.DependencyTreeView);
                        m_DependencyTreeScroll = EditorGUILayout.BeginScrollView(m_DependencyTreeScroll);
                        if (m_DependencyTreeView.IsBuild)
                        {
                            m_DependencyTreeView.OnGUI(new Rect(0, 0, position.width, position.height));
                        }
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();

                    // 依存ファイルのリスト表示
                    EditorGUILayout.BeginVertical();
                    {
                        EditorGUILayout.LabelField(Styles.DependencyListView);
                        m_DependencyListScroll = EditorGUILayout.BeginScrollView(m_DependencyListScroll);

                        if ((m_DependencyTreeView != null) && (m_DependencyTreeView.DependencyFileList != null))
                        {
                            foreach (var dependency in m_DependencyTreeView.DependencyFileList)
                            {
                                EditorGUILayout.LabelField(dependency);
                            }
                        }
                        
                        

                        //EditorGUILayout.TextArea(m_DependencyListText);
                        EditorGUILayout.EndScrollView();
                    }
                    EditorGUILayout.EndVertical();
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void CreateDB()
        {            
            m_Hash2AssetBundleFilePaths.Clear();
            m_AssetBundleFilePath2Hashes.Clear();
            m_Hash2DumpFilePaths.Clear();            
            m_AssetBundleHashes = new string[] { string.Empty };            

            var fpaths = new List<string>();
            var extentions = m_AssetBundleExtentions.Split(new char[] {';'});
            foreach (var ext in extentions)
            {
                var assetbundles = Directory.GetFiles(m_AssetBundleRootFolder, ext, SearchOption.AllDirectories);                
                fpaths.AddRange(assetbundles);
            }            
            if (Directory.Exists(m_CasheFolder))
            {
                DeleteDirectoryRecursive(m_CasheFolder);
            }
            Directory.CreateDirectory(m_CasheFolder);

            var webExtractExec = new WebExtractExec();
            var b2t = new Binary2TextExec();
            var no = 0;            
            var hashs = new List<string>();
            try
            {
                foreach (var fpath in fpaths)
                {
                    var fileName = Path.GetFileName(fpath);
                    var progress = (float)no / (float)fpaths.Count;
                    EditorUtility.DisplayProgressBar("UnityAssetBundleDumper", $"Create DB... {no}/{fpaths.Count}",progress);
                    
                    // 同じ名前を持つAssetBundleが存在するケースがあるので、ランダムな名前のディレクトリを追加する
                    var folderPath = Path.Combine(m_CasheFolder, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
                    
                    
                    Directory.CreateDirectory(folderPath);
                    var dstFilePath = Path.Combine(folderPath, fileName);
                    var unpackFolder = dstFilePath + "_data";                    

                    File.Copy(fpath, Path.Combine(m_CasheFolder, dstFilePath), true);
                    var result = webExtractExec.Exec(dstFilePath);
                    File.Delete(dstFilePath);
                    if(result != 0)
                    {
                        EditorUtility.DisplayDialog("WebExtract Fail", fpath, "OK");                        
                        throw new System.InvalidProgramException();                        
                    }
                    
                    var serializeFiles = Directory.GetFiles(unpackFolder, "CAB-*.", SearchOption.TopDirectoryOnly);
                    if(serializeFiles.Length == 0)
                    {
                        // SceneをAssetBundle化した場合、BuildPlayer-から始まるファイルが解凍される
                        serializeFiles = Directory.GetFiles(unpackFolder, "BuildPlayer-*", SearchOption.TopDirectoryOnly);
                    }

                    
                    for (var i = 0; i < serializeFiles.Length; i++)
                    {
                        var serializeFileName = Path.GetFileName(serializeFiles[i]);
                        if(Path.GetExtension(serializeFileName) == ".resS")
                        {
                            // Resourcesファイル？
                            // bin2txtでダンプ出来ない為、パスする
                            continue;
                        }
                        var dumpFilePath = Path.Combine(folderPath, serializeFileName) + ".txt";
                        result = b2t.Exec(serializeFiles[i], dumpFilePath, "");
                        if (result != 0)
                        {
                            EditorUtility.DisplayDialog("Bin2Text Fail", b2t.output, "OK");
                            Debug.Log(b2t.output);
                            throw new System.InvalidProgramException();
                        }

                        var filePath = fpath.Replace('\\', '/');
                        dumpFilePath = dumpFilePath.Replace('\\', '/');
                        
                        hashs.Add(serializeFileName);
                        m_Hash2AssetBundleFilePaths.Add(serializeFileName, filePath);
                        if (i == 0)
                        {
                            m_AssetBundleFilePath2Hashes.Add(filePath, serializeFileName);
                        }
                        m_Hash2DumpFilePaths.Add(serializeFileName, dumpFilePath);
                        

                    }
                    no++;
                }
            }
            catch(System.ArgumentException e)
            {
                Debug.LogException(e);                
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);                
            }
            finally
            {             
                hashs.Sort();
                m_AssetBundleHashes = hashs.ToArray();                
                assetBundleDumpData.Serialize(m_DataBaseFilePath);
                EditorUtility.ClearProgressBar();
            }
        }


        


        void DeleteDirectoryRecursive(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);
            foreach (FileInfo file in di.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo subDirectory in di.EnumerateDirectories())
            {
                subDirectory.Delete(true);
            }
            foreach (DirectoryInfo subDirectory in di.EnumerateDirectories())
            {
                subDirectory.Delete(true);
            }
            Directory.Delete(path, true);
        }

        void BrowseAssetBundleRootFolder()
        {
            m_AssetBundleFilePath = EditorUtility.OpenFilePanel("Select AssetBundle", m_AssetBundleRootFolder, "");
            string hash;
            if (m_AssetBundleFilePath2Hashes.TryGetValue(m_AssetBundleFilePath, out hash))
            {
                for (var i = 0; i < m_AssetBundleHashes.Length; i++)
                {
                    if (m_AssetBundleHashes[i] == hash)
                    {
                        m_HashIndex = i;
                        break;
                    }
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Select AssetBundle", $"{m_AssetBundleFilePath} is not registed DB.", "OK");
                m_AssetBundleFilePath = string.Empty;
            }
        }

        


        void AnalyzeDumpFiles()
        {
            assetBundleDumpData.m_Hash2AssetBundleBundeInfo = new Dictionary<string, AssetBundeInfo>();
            foreach (var hash in m_AssetBundleHashes)
            {
                var fpath = m_Hash2AssetBundleFilePaths[hash];
                var dump = m_Hash2DumpFilePaths[hash];
                var assetBundleInfo = AnalyzeDumpFile(dump);
                assetBundleDumpData.m_Hash2AssetBundleBundeInfo.Add(hash, assetBundleInfo);
            }
        }


        AssetBundeInfo AnalyzeDumpFile(string fpath)
        {
            var assetBundeInfo = new AssetBundeInfo();
            var assetInfos = new List<AssetInfo>();

            using (StreamReader sr = new StreamReader(new FileStream(fpath, FileMode.Open)))
            {
                string line = null;
                while (true)
                {
                    if(line == null)
                    {
                        line = sr.ReadLine();
                    }                    
                    if (line == null)
                    {
                        break;
                    }
                    if (line.StartsWith("ID:"))
                    {
                        var words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                        var id = long.Parse(words[1]);
                        var classID = int.Parse(words[3].TrimEnd(')'));
                        if (id == 1)
                        {
                            line = sr.ReadLine();
                            GetLine(ref line);
                            assetBundeInfo.m_Name = line.Split("\"")[1];
                            sr.ReadLine();
                            line = sr.ReadLine();
                            words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            assetBundeInfo.m_Preloads = new PPtrInfo[int.Parse(words[1])];
                            for (var i = 0; i < assetBundeInfo.m_Preloads.Length; i++)
                            {
                                assetBundeInfo.m_Preloads[i] = new PPtrInfo();
                                // data  (PPtr<Object>)
                                sr.ReadLine();
                                for (var j = 0; j < 2; j++)
                                {
                                    line = sr.ReadLine();
                                    GetLine(ref line);
                                    words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                    try
                                    {
                                        if (j == 0)
                                        {
                                            assetBundeInfo.m_Preloads[i].m_FileID = int.Parse(words[1]);
                                        }
                                        else
                                        {
                                            assetBundeInfo.m_Preloads[i].m_PathID = long.Parse(words[1]);
                                        }
                                    }
                                    catch (System.Exception e)
                                    {
                                        Debug.LogException(e);
                                    }
                                }
                            }
                            line = null;
                        }
                        else
                        {
                            var assetInfo = new AssetInfo(id, classID, words[4]);
                            line = CheckProperty(sr, assetInfo);
                            assetInfos.Add(assetInfo);
                        }
                    }
                    else
                    {
                        line = null;
                    }
                }
            }
            assetBundeInfo.m_AssetInfos = assetInfos.ToArray();
            return assetBundeInfo;
        }

        int GetLine(ref string line)
        {
            int indent = 0;
            while (line.StartsWith("\t"))
            {
                line = line.Substring(1);                                
                indent++;
            }
            return indent;
        }


        // プロパティチェック
        string CheckProperty(StreamReader sr,AssetInfo assetInfo)
        {
            var pptrInfoList = new List<PPtrInfo>();
            while (true)
            {                
                var line = sr.ReadLine();
                var backup = line;
                // ファイルの終端の場合は終了
                if (line == null)
                {
                    assetInfo.m_PPtrInfos = pptrInfoList.ToArray();
                    return null;
                } 
                else if(line == String.Empty)
                {
                    continue;
                }
                var indent = GetLine(ref line);                
                var words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);                
                // "ID:"から始まる場合、次のAssetの情報になる為、ライン読み込みを指し戻して終了
                if (words[0] == "ID:")
                {
                    assetInfo.m_PPtrInfos = pptrInfoList.ToArray();
                    return backup;
                } 
                else if ((words[0] == "m_Name") && (indent == 1))
                {
                    var name = line.Split("\"")[1];
                    assetInfo.m_Name = name;
                } 
                else if (words[1].StartsWith("(PPtr"))
                {
                    // PPtrの次の行はm_FileID、その次はm_PathIDで固定されている                    
                    var pptrInfo = new PPtrInfo();
                    for(var i = 0; i < 2; i++)
                    {                        
                        line = sr.ReadLine();                        
                        indent = GetLine(ref line);
                        words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                        if((i == 0) && (words[0] == "m_FileID"))
                        {
                            pptrInfo.m_FileID = int.Parse(words[1]);
                        }
                        else if((i == 1) && (words[0] == "m_PathID"))
                        {
                            pptrInfo.m_PathID = long.Parse(words[1]);
                        }
                        else
                        {
                            Debug.LogError($"{line}");
                        }
                    }                    
                    pptrInfoList.Add(pptrInfo);
                }
            }
            
        }


    }
}
