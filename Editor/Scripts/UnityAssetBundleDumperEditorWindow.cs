using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UTJ.UnityCommandLineTools;


namespace UTJ.UnityAssetBundleDumper.Editor
{
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



    public class UnityAssetBundleDumperEditorWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent AssetBundleRootFolder = new GUIContent("Root", "AssetBundle Root Folder Location");
            public static readonly GUIContent Browse = new GUIContent("Browse");
            public static readonly GUIContent AssetBundleExtentions = new GUIContent("Search Pattern", "Specifies the filename pattern for the AssetBundle. The default value is \"*.\"and this is the pattern when there is no extension. To specify multiple patterns, separate them with ';'. (example: \"*.;*.txt\")");
            public static readonly GUIContent CreateDB = new GUIContent("Create DB","Create AssetBundle DataBase");
            public static readonly GUIContent SelectAssetBundle = new GUIContent("AssetBundle");            
            public static readonly GUIContent Hash = new GUIContent("Hash", "This is the AssetBundle Hash, but you can also select an AssetBundle from the Hash.");
        }

        static UnityAssetBundleDumperEditorWindow m_Instance;

        string m_AssetBundleRootFolder;
        string m_AssetBundleExtentions = "*.";
        string[] m_AssetBundleHashes;
        Dictionary<string, string> m_Hash2AssetBundleFilePaths;
        Dictionary<string, string> m_AssetBundleFilePath2Hashes;
        Dictionary<string, string> m_AssetBundleFilePath2DumpFilePaths;
        int m_HashIndex;       
        string m_AssetBundleFilePath = string.Empty;
        string m_DependencyListText;        
        string m_DependencyTreeText;
        Vector2 m_DependencyTreeScroll;
        Vector2 m_DependencyListScroll;


        [MenuItem("Window/UTJ/UnityAssetBundleDumper")]
        public static void Open()
        {
            m_Instance = EditorWindow.GetWindow(typeof(UnityAssetBundleDumperEditorWindow)) as UnityAssetBundleDumperEditorWindow;
            m_Instance.titleContent.text = "UnityAssetBundleDumper";
            if (m_Instance.m_AssetBundleRootFolder == null)
            {
                m_Instance.m_AssetBundleRootFolder = Application.dataPath;
            }            
        }

        private void OnEnable()
        {
            if(m_AssetBundleHashes == null)
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
            if (m_AssetBundleFilePath2DumpFilePaths == null)
            {
                m_AssetBundleFilePath2DumpFilePaths = new Dictionary<string, string>();
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("AssetBunde DataBase"));            
            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel++;
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

            if (GUILayout.Button(Styles.CreateDB))
            {
                CreateDB();
            }

            EditorGUI.indentLevel--;

            EditorGUILayout.Separator();
                                
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(Styles.SelectAssetBundle);
            EditorGUILayout.TextField(m_AssetBundleFilePath);
            if (GUILayout.Button(Styles.Browse))
            {                
                m_AssetBundleFilePath = EditorUtility.OpenFilePanel("Select AssetBundle", m_AssetBundleRootFolder,"");
                string hash;
                if(m_AssetBundleFilePath2Hashes.TryGetValue(m_AssetBundleFilePath,out hash))
                {
                    for(var i = 0; i < m_AssetBundleHashes.Length; i++)
                    {
                        if(m_AssetBundleHashes[i] == hash)
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
            EditorGUILayout.PrefixLabel(Styles.Hash);
            if(m_AssetBundleHashes == null || m_AssetBundleHashes.Length == 0)
            {
                m_AssetBundleHashes = new string[] {""};
            }
            m_HashIndex = System.Math.Min(m_HashIndex, m_AssetBundleHashes.Length - 1);
            m_HashIndex = System.Math.Max(m_HashIndex, 0);
            var oldHashIndex = m_HashIndex;
            m_HashIndex = EditorGUILayout.Popup(m_HashIndex, m_AssetBundleHashes);
            if(m_HashIndex != oldHashIndex)
            {
                var hash = m_AssetBundleHashes[m_HashIndex];
                string filePath;
                if(m_Hash2AssetBundleFilePaths.TryGetValue(hash, out filePath))
                {
                    m_AssetBundleFilePath = filePath;
                }
            }            
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("Dependency"))
            {
                var hashTrees = new List<HashTree>();
                var hash = m_AssetBundleHashes[m_HashIndex];
                Dependency(0, hash, ref hashTrees);
                var dependencyList = new List<string>();
                foreach(var hashTree in hashTrees)
                {
                    if(hashTree.depth == 0)
                    {
                        continue;
                    }
                    if(dependencyList.Contains(hashTree.hash) == false)
                    {
                        dependencyList.Add(hashTree.hash);
                    }
                }
                using (StringWriter sw = new StringWriter())
                {
                    foreach(var dependency in dependencyList)
                    {
                        var line = m_Hash2AssetBundleFilePaths[dependency];
                        line = line.Remove(0,m_AssetBundleRootFolder.Length+1);
                        sw.WriteLine(line);
                    }
                    m_DependencyListText = sw.ToString();
                }

                using (StringWriter sw = new StringWriter())
                {
                    foreach (var hashTree in hashTrees)
                    {
                        string line = string.Empty;
                        for (var i = 0; i < hashTree.depth; i++)
                        {
                            line = line + " ";
                        }
                        var fname = Path.GetFileName(m_Hash2AssetBundleFilePaths[hashTree.hash]);
                        line = line + $"{fname}:({hashTree.hash})";
                        sw.WriteLine(line);
                    }
                    m_DependencyTreeText = sw.ToString();
                }                
            }
            EditorGUILayout.BeginHorizontal();
            m_DependencyTreeScroll = EditorGUILayout.BeginScrollView(m_DependencyTreeScroll);
            EditorGUILayout.TextArea(m_DependencyTreeText);
            EditorGUILayout.EndScrollView();

            m_DependencyListScroll = EditorGUILayout.BeginScrollView(m_DependencyListScroll) ;
            EditorGUILayout.TextArea(m_DependencyListText);
            EditorGUILayout.EndScrollView();

            EditorGUILayout.EndHorizontal();
        }

        private void CreateDB()
        {            
            m_Hash2AssetBundleFilePaths.Clear();
            m_AssetBundleFilePath2Hashes.Clear();
            m_AssetBundleFilePath2DumpFilePaths.Clear();            
            m_AssetBundleHashes = new string[] { string.Empty };            

            var fpaths = new List<string>();
            var extentions = m_AssetBundleExtentions.Split(new char[] {';'});
            foreach (var ext in extentions)
            {
                var assetbundles = Directory.GetFiles(m_AssetBundleRootFolder, ext, SearchOption.AllDirectories);                
                fpaths.AddRange(assetbundles);
            }

            var cachePath = Path.GetDirectoryName(Application.dataPath);
            cachePath = Path.Combine(cachePath, "Library");
            cachePath = Path.Combine(cachePath, "UnityAssetBundleDumper");
            cachePath = Path.Combine(cachePath, "Cache");
            if (Directory.Exists(cachePath))
            {
                DeleteDirectoryRecursive(cachePath);                
            }
            Directory.CreateDirectory(cachePath);

            var webExtractExec = new WebExtractExec();
            var b2t = new Binary2TextExec();
            var i = 0;
            var filePaths = new List<string>();
            var hashs = new List<string>();
            try
            {
                foreach (var fpath in fpaths)
                {
                    var fileName = Path.GetFileName(fpath);
                    var progress = (float)i / (float)fpaths.Count;
                    EditorUtility.DisplayProgressBar("UnityAssetBundleDumper", $"Create DB... {i}/{fpaths.Count}",progress);
                    
                    // 同じ名前を持つAssetBundleが存在するケースがあるので、ランダムな名前のディレクトリを追加する
                    var dstFilePath = Path.Combine(cachePath, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));                                        
                    Directory.CreateDirectory(dstFilePath);
                    dstFilePath = Path.Combine(dstFilePath,fileName);
                    var unpackFolder = dstFilePath + "_data";                    

                    File.Copy(fpath, Path.Combine(cachePath, dstFilePath), true);
                    var result = webExtractExec.Exec(dstFilePath);
                    File.Delete(dstFilePath);
                    if(result != 0)
                    {
                        EditorUtility.DisplayDialog("WebExtract Fail", fpath, "OK");                        
                        throw new System.InvalidProgramException();                        
                    }
                    
                    var serializeFiles = Directory.GetFiles(unpackFolder, "CAB-*.", SearchOption.TopDirectoryOnly);
                    if(serializeFiles.Length > 1)
                    {
                        Debug.LogWarning($"Serialize File number is bigger than 1. ({fpath})");
                    }
                    var serializeFileName = Path.GetFileName(serializeFiles[0]);                    
                    var dumpFilePath = dstFilePath + ".txt";
                    result = b2t.Exec(serializeFiles[0], dumpFilePath,"");
                    if(result != 0)
                    {
                        EditorUtility.DisplayDialog("Bin2Text Fail", b2t.output, "OK");
                        Debug.Log(b2t.output);
                        throw new System.InvalidProgramException();
                    }

                    var filePath = fpath.Replace('\\','/');
                    dumpFilePath = dumpFilePath.Replace('\\', '/');

                    filePaths.Add(filePath);
                    hashs.Add(serializeFileName);
                    m_Hash2AssetBundleFilePaths.Add(serializeFileName, filePath);
                    m_AssetBundleFilePath2Hashes.Add(filePath, serializeFileName);
                    m_AssetBundleFilePath2DumpFilePaths.Add(filePath, dumpFilePath);
                    i++;
                }                                                
            }
            catch(System.ArgumentException e)
            {
                Debug.Log(filePaths[i]);
            }
            catch(System.Exception e)
            {
                Debug.LogException(e);                
            }
            finally
            {             
                hashs.Sort();
                m_AssetBundleHashes = hashs.ToArray();
                EditorUtility.ClearProgressBar();
            }
        }


        bool Dependency(int depth,string hash,ref List<HashTree> hashTrees)
        {
            string fpath;

            var result = m_Hash2AssetBundleFilePaths.TryGetValue(hash, out fpath);
            if (result == false)
            {
                return false;
            }
            string dump;
            result = m_AssetBundleFilePath2DumpFilePaths.TryGetValue(fpath, out dump);
            if(result == false)
            {
                return false;
            }

            using (StreamReader sr = new StreamReader(new FileStream(dump, FileMode.Open)))
            {
                string line = sr.ReadLine();
                if (line != "External References")
                {
                    return false;
                }
                var children = new List<string>();

                while ((line = sr.ReadLine()) != null)
                {
                    if(line.StartsWith("path")  == false)
                    {
                        break;
                    }
                    // path(1): "Resources/unity_builtin_extra" GUID: 0000000000000000f000000000000000 Type: 0
                    if (line.Contains("Resources"))
                    {
                        continue;
                    }
                    //
                    // path(2): "archive:/CAB-56bb25c0e5ea7af2a5c41a1994f98568/CAB-56bb25c0e5ea7af2a5c41a1994f98568" GUID: 00000000000000000000000000000000 Type: 0                    
                    string[] words = line.Split('/');
                    // word[0]:path(2): "archive:/
                    // word[1]:CAB-56bb25c0e5ea7af2a5c41a1994f98568
                    //
                    children.Add(words[1]);

                }
                var hashTree = new HashTree(depth,hash, children.ToArray());
                hashTrees.Add(hashTree);
                for(var i = 0; i < children.Count; i++)
                {
                    Dependency(++depth,children[i], ref hashTrees);
                }
            }


            return true;
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
    }
}
