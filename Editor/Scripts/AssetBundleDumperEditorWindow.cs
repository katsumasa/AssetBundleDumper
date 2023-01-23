
using UnityEngine;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UTJ.UnityCommandLineTools;
using System;
using UTJ.UnityAssetBundleDumper.Editor;
using Unity.VisualScripting.YamlDotNet.Core;
using System.Data.SqlTypes;
using static UnityEditor.Progress;

namespace UTJ.AssetBundleDumper.Editor
{
    public class History
    {
        string m_assetBundleName;
        int m_fileID;
        int m_pathID;

        public string assetBundleName
        {
            get { return m_assetBundleName; }
            set { m_assetBundleName = value; }
        }

        public int fileID
        {
            get { return m_fileID; }
            set { m_fileID = value; } 
        }

        public int pathID
        {
            get { return m_pathID; }
            set { m_pathID = value; }
        }
    }

    public interface ISerializer
    {
        public void Serialize(BinaryWriter binaryWriter);
        public void Deserialize(BinaryReader binaryReader);
    }

    public class PPtrInfo : ISerializer
    {
        int m_fileID;
        long m_pathID;

        public int fileID
        {
            get { return m_fileID; }
            set { m_fileID = value; }
        }

        public long pathID
        {
            get { return m_pathID; }
            set { m_pathID = value; }
        }

        public PPtrInfo() { }

        public PPtrInfo(int fileID, long pathID)
        {
            m_fileID = fileID;
            m_pathID = pathID;
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_fileID);
            binaryWriter.Write(m_pathID);
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            m_fileID = binaryReader.ReadInt32();
            m_pathID = binaryReader.ReadInt64();
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                PPtrInfo pPtrInfo = (PPtrInfo)obj;
                return ((m_fileID == pPtrInfo.m_fileID) && (m_pathID == pPtrInfo.m_pathID));
            }
        }

        public override int GetHashCode()
        {
            var hash = new { m_fileID, m_pathID }.GetHashCode();
            var hash128 = new Hash128();
            hash128.Append(base.GetHashCode());
            hash128.Append(hash);
            return hash128.GetHashCode();
        }
    }

    // AssetDump情報
    public class AssetInfo : ISerializer
    {
        long m_id;
        int m_classID;
        string m_objectName;
        string m_name;
        PPtrInfo[] m_pptrInfos;

        public long id
        {
            get { return m_id; }
        }

        public int classID
        {
            get { return m_classID; }
        }
        public string objectName
        {
            get { return m_objectName; }
        }
        public string name
        {
            get { return m_name; }
            set { m_name = value; }
        }
        public PPtrInfo[] pptrInfos
        {
            get { return m_pptrInfos; }
            set { m_pptrInfos = value; }
        }

        public AssetInfo() { }

        public AssetInfo(long ID, int classID, string objectName)
        {
            m_id = ID;
            m_classID = classID;
            m_objectName = objectName;
            m_name = string.Empty;
            m_pptrInfos = new PPtrInfo[0];
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_id);
            binaryWriter.Write(m_classID);
            binaryWriter.Write(m_objectName);
            binaryWriter.Write(m_name);
            binaryWriter.Write(m_pptrInfos.Length);
            for (var i = 0; i < m_pptrInfos.Length; i++)
            {
                m_pptrInfos[i].Serialize(binaryWriter);
            }
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            m_id = binaryReader.ReadInt64();
            m_classID = binaryReader.ReadInt32();
            m_objectName = binaryReader.ReadString();
            m_name = binaryReader.ReadString();
            var len = binaryReader.ReadInt32();
            m_pptrInfos = new PPtrInfo[len];
            for (var i = 0; i < len; i++)
            {
                m_pptrInfos[i] = new PPtrInfo();
                m_pptrInfos[i].Deserialize(binaryReader);
            }
        }
    }

    // CABファイルの情報
    public class CABInfo : ISerializer
    {
        // Dumpファイルのパス
        string m_path;
        // CABファイル名
        string m_name;
        // CABファイルを内包しているAssetBundle名
        string m_assetBundleName;
        // 外部参照のテーブル
        string[] m_externalReferences;
        // 間接的に参照しているリファレンス全て
        string[] m_externalReferenceRecursives;
        // 外部から参照されている数
        int m_referenceCounter;
        // プリロードテーブル
        PPtrInfo[] m_preloads;
        // CABに含まれるAssetのダンプ情報
        AssetInfo[] m_assetInfos;

        public string path { get { return m_path; } set { m_path = value; } }
        public string name { get { return m_name; } set { m_name = value; } }
        public string assetBundleName { get { return m_assetBundleName; } set { m_assetBundleName = value; } }
        public string[] externalReferences { get { return m_externalReferences; } set { m_externalReferences = value; } }
        public string[] externalReferenceRecursives { get { return m_externalReferenceRecursives; } set { m_externalReferenceRecursives = value; } }
        public int referenceCounter
        {
            get { return m_referenceCounter; }
            set { m_referenceCounter = value; }
        }
        public PPtrInfo[] preloads { get { return m_preloads; } set { m_preloads = value; } }
        public AssetInfo[] assetInfos { get { return m_assetInfos; } set { m_assetInfos = value; } }

        public CABInfo()
        {
            m_path = string.Empty;
            m_assetBundleName = string.Empty;
            m_name = string.Empty;
            m_externalReferences = new string[0];
            m_externalReferenceRecursives = new string[0];
            m_preloads = new PPtrInfo[0];
            m_assetInfos = new AssetInfo[0];
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_path);
            binaryWriter.Write(m_assetBundleName);
            binaryWriter.Write(m_name);            
            binaryWriter.Write(m_externalReferences.Length);
            for (var i = 0; i < m_externalReferences.Length; i++)
            {
                binaryWriter.Write(m_externalReferences[i]);
            }
            binaryWriter.Write(m_externalReferenceRecursives.Length);
            for(var i = 0; i < m_externalReferenceRecursives.Length; i++)
            {
                binaryWriter.Write(m_externalReferenceRecursives[i]);
            }
            binaryWriter.Write(m_referenceCounter);
            binaryWriter.Write(m_preloads.Length);
            for (var i = 0; i < m_preloads.Length; i++)
            {
                m_preloads[i].Serialize(binaryWriter);
            }

            binaryWriter.Write(m_assetInfos.Length);
            for (var i = 0; i < m_assetInfos.Length; i++)
            {
                m_assetInfos[i].Serialize(binaryWriter);
            }
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            m_path = binaryReader.ReadString();
            m_assetBundleName = binaryReader.ReadString();
            m_name = binaryReader.ReadString();            
            int len = binaryReader.ReadInt32();
            m_externalReferences = new string[len];
            for (var i = 0; i < len; i++)
            {
                m_externalReferences[i] = binaryReader.ReadString();
            }
            len = binaryReader.ReadInt32();
            m_externalReferenceRecursives = new string[len];
            for(var i = 0; i < len; i++)
            {
                m_externalReferenceRecursives[i] = binaryReader.ReadString();
            }
            m_referenceCounter = binaryReader.ReadInt32();

            len = binaryReader.ReadInt32();
            m_preloads = new PPtrInfo[len];
            for (var i = 0; i < len; i++)
            {
                m_preloads[i] = new PPtrInfo();
                m_preloads[i].Deserialize(binaryReader);
            }

            len = binaryReader.ReadInt32();
            m_assetInfos = new AssetInfo[len];
            for (var i = 0; i < len; i++)
            {
                m_assetInfos[i] = new AssetInfo();
                m_assetInfos[i].Deserialize(binaryReader);
            }
        }

        public AssetInfo GetAssetInfo(long id)
        {
            foreach(var assetInfo in assetInfos)
            {
                if(assetInfo.id == id)
                {
                    return assetInfo;
                }
            }
            return null;
        }
    }

    public class AssetBundleInfo : ISerializer
    {
        // AssetBundle名
        string m_name;
        // AssetBundleに含まれるCABのテーブル
        CABInfo[] m_cabInfos;
        // 間接的に参照している全外部参照
        string[] m_externalReferenceRecursives;
        int m_referenceCounter;

        public string name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public CABInfo[] cabInfos
        {
            get { return m_cabInfos; }
            set { m_cabInfos = value; }
        }

        public string[] externalReferenceRecursives
        {
            get { return m_externalReferenceRecursives; }
            set { m_externalReferenceRecursives = value; }
        }

        public int referenceCounter
        {
            get { return m_referenceCounter; }
            set { m_referenceCounter = value; }
        }

        public AssetBundleInfo()
        {
            m_name = string.Empty;
            m_cabInfos = new CABInfo[0];
            m_externalReferenceRecursives = new string[0];
            m_referenceCounter = 0;
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_name);
            binaryWriter.Write(m_cabInfos.Length);
            for (var i = 0; i < m_cabInfos.Length; i++)
            {
                m_cabInfos[i].Serialize(binaryWriter);
            }
            binaryWriter.Write(m_externalReferenceRecursives.Length);
            for(var i = 0; i < externalReferenceRecursives.Length; i++)
            {
                binaryWriter.Write(externalReferenceRecursives[i]);
            }
            binaryWriter.Write(m_referenceCounter);
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            m_name = binaryReader.ReadString();
            var len = binaryReader.ReadInt32();
            m_cabInfos = new CABInfo[(int)len];
            for (var i = 0; i < len; i++)
            {
                m_cabInfos[i] = new CABInfo();
                m_cabInfos[i].Deserialize(binaryReader);
            }
            len = binaryReader.ReadInt32();
            m_externalReferenceRecursives = new string[len];
            for(var i = 0; i < len; i++)
            {
                m_externalReferenceRecursives[i] = binaryReader.ReadString();
            }
            m_referenceCounter = binaryReader.ReadInt32();
        }

        public CABInfo GetCABInfo(string name)
        {
            foreach(var cabInfo in cabInfos)
            {
                if(cabInfo.name == name)
                {
                    return cabInfo;
                }
            }
            return null;
        }
    }

    public class AssetBundleDumpData : ISerializer
    {
        readonly string m_versions = "v.0.0.2";
        int m_externalReferenceDepth;
        string m_assetBundleRootFolder;
        string m_assetBundleExtentions;
        AssetBundleInfo[] m_assetBunleInfos;
        
        public int externalReferenceDepth
        {
            get { return m_externalReferenceDepth; }
            set { m_externalReferenceDepth = value;}
        }

        public string assetBundleRootFolder
        {
            get { return m_assetBundleRootFolder; }
            set { m_assetBundleRootFolder = value; }
        }
        public string assetBundleExtentions
        {
            get { return m_assetBundleExtentions; }
            set { m_assetBundleExtentions = value; }
        }

        public AssetBundleInfo[] assetBundleInfos
        {
            get { return m_assetBunleInfos; }
            set
            {
                m_assetBunleInfos = value;
            }
        }


        public AssetBundleDumpData()
        {
            m_externalReferenceDepth = 1;
            m_assetBundleRootFolder = Application.dataPath;
            m_assetBundleExtentions = "*.";
            m_assetBunleInfos = new AssetBundleInfo[0];
        }
        
        public AssetBundleInfo GetAssetBundleInfo(string assetBundleName)
        {
            foreach (var assetBundleInfo in m_assetBunleInfos)
            {
                if(assetBundleInfo.name == assetBundleName)
                {
                    return assetBundleInfo;
                }
            }
            return null;
        }

        public CABInfo GetCABInfo(string cabName)
        {
            foreach(var assetBundleInfo in m_assetBunleInfos)
            {
                var cabInfo = assetBundleInfo.GetCABInfo(cabName);
                if(cabInfo != null)
                {
                    return cabInfo;
                }
            }
            return null;
        }

        public void Serialize(BinaryWriter binaryWriter)
        {
            binaryWriter.Write(m_versions);
            binaryWriter.Write(m_externalReferenceDepth);
            binaryWriter.Write(m_assetBundleRootFolder);
            binaryWriter.Write(m_assetBundleExtentions);
            binaryWriter.Write(m_assetBunleInfos.Length);
            foreach(var assetBundleInfo in m_assetBunleInfos)
            {
                assetBundleInfo.Serialize(binaryWriter);
            }
        }

        public void Deserialize(BinaryReader binaryReader)
        {
            var v = binaryReader.ReadString();
            if(v != m_versions)
            {
                // バージョンが異なる場合の処理
                return;
            }
            m_externalReferenceDepth = binaryReader.ReadInt32();
            m_assetBundleRootFolder = binaryReader.ReadString();
            m_assetBundleExtentions = binaryReader.ReadString();
            var len = binaryReader.ReadInt32();
            m_assetBunleInfos = new AssetBundleInfo[len];
            for(var i = 0; i < len; i++)
            {
                m_assetBunleInfos[i] = new AssetBundleInfo();
                m_assetBunleInfos[i].Deserialize(binaryReader);
            }
        }        


        public void Analyze()
        {
            try
            {
                var no = 0;
                foreach (var assetBundleInfo in m_assetBunleInfos)
                {
                    if (EditorUtility.DisplayCancelableProgressBar("AssetBundleDumper", "Deep Analyze", (float)no / (float)m_assetBunleInfos.Length))
                    {
                        break; ;
                    }

                    // 依存先が依存しているCABを再帰的に調べていく
                    var list = new List<string>();
                    foreach (var cabInfo in assetBundleInfo.cabInfos)
                    {
                        // 再帰的に内部
                        var externalReferences = new List<string>();
                        ExternalReferenceRecursive(cabInfo, ref externalReferences);
                        // 自分自身が含まれている場合は削除
                        if (externalReferences.Contains(cabInfo.name))
                        {
                            externalReferences.Remove(cabInfo.name);
                        }
                        cabInfo.externalReferenceRecursives = externalReferences.ToArray();
                        foreach (var reference in externalReferences)
                        {
                            if (!list.Contains(reference))
                            {
                                list.Add(reference);
                            }
                        }

                        foreach (var renference in cabInfo.externalReferences)
                        {
                            var external = GetCABInfo(renference);
                            if (external != null)
                            {
                                // 自身が参照されている数を調べる
                                external.referenceCounter++;
                            }
                        }
                    }
                    assetBundleInfo.externalReferenceRecursives = list.ToArray();
                }

                var path = EditorUtility.SaveFilePanel("Save File as CSV", "", "", "csv");
                if (!string.IsNullOrEmpty(path))
                {
                    using (var sw = new StreamWriter(path, false, System.Text.Encoding.UTF8))
                    {

                        foreach (var assetBundleInfo in m_assetBunleInfos)
                        {
                            foreach (var cabInfo in assetBundleInfo.cabInfos)
                            {
                                sw.WriteLine($"{assetBundleInfo.name},{cabInfo.name},{cabInfo.referenceCounter}");
                            }
                        }
                    }
                }


            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        void ExternalReferenceRecursive(CABInfo cabInfo,ref List<string> externalReferences)
        {
            if(cabInfo == null)
            {
                return;
            }
            if (externalReferences.Contains(cabInfo.name))
            {
                return;
            }
            foreach(var cabName in cabInfo.externalReferences)
            {
                externalReferences.Add(cabName);
                ExternalReferenceRecursive(GetCABInfo(cabName),ref externalReferences);                
            }

        }

    }


    public class AssetBundleDumperEditorWindow : EditorWindow
    {
        static class Styles
        {
            public static readonly GUIContent Undo = new GUIContent("Undo","Undo History");
            public static readonly GUIContent AssetBundleRootFolder = new GUIContent("AssetBundle Root", "AssetBundle RootFolder Location");
            public static readonly GUIContent BrowseAssetBundleRootFolderLocation = new GUIContent("Browse","Browse AssetBundle RootFolder Loacation");
            public static readonly GUIContent AssetBundleExtentions = new GUIContent("Filters", "Specifies the filename pattern for the AssetBundle. The default value is \"*.\"and this is the pattern when there is no extension. To specify multiple patterns, separate them with ';'. (example: \"*.;*.txt\")");
            public static readonly GUIContent Dump = new GUIContent("Dump","Dump AssetBundles");
            public static readonly GUIContent Clear = new GUIContent("Clear","Clear DumpFiles");
            public static readonly GUIContent TargetAssetBundle = new GUIContent("Target AssetBundle");
            public static readonly GUIContent SelectTargetAssetBundle = new GUIContent("Select");
            public static readonly GUIContent IncludeFiles = new GUIContent("Include Files");
            public static readonly GUIContent Assets = new GUIContent("Assets");
        }

        string m_workFolder;
        string m_dataBaseFilePath;
        string m_casheFolder;
        string m_targetAssetBundlePath;
        AssetBundleDumpData m_assetBundleDumpData;                        
        TreeViewState m_externalReferenceTreeViewState;
        ExternalReferenceTreeView m_externalReferenceTreeView;
        TreeViewState m_assetTreeViewState;
        AssetTreeView m_assetTreeView;
        Stack<History> m_historys;


        [MenuItem("Window/UTJ/AssetBundleDumper2")]
        public static void Open()
        {
            var instance = EditorWindow.GetWindow<AssetBundleDumperEditorWindow>();
            instance.titleContent.text = "AssetBundleDumper";            
        }

        public static AssetBundleDumpData GetAssetBundleDumpData()
        {
            var instance = EditorWindow.GetWindow<AssetBundleDumperEditorWindow>();
            return instance.m_assetBundleDumpData;
        }                       

        private void OnEnable()
        {
            if(m_assetBundleDumpData == null)
            {
                m_assetBundleDumpData = new AssetBundleDumpData();                                 
            }
            if(m_historys == null)
            {
                m_historys = new Stack<History>();
            }


            m_workFolder = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Library", "AssetBundleDumper");
            m_dataBaseFilePath = Path.Combine(m_workFolder, "db");
            m_casheFolder = Path.Combine(m_workFolder, "caches");
            if (File.Exists(m_dataBaseFilePath))
            {
                using (BinaryReader binaryReader = new BinaryReader(new FileStream(m_dataBaseFilePath,FileMode.Open)))
                {
                    m_assetBundleDumpData.Deserialize(binaryReader);
                }
            }            

            if (m_externalReferenceTreeViewState == null)
            {
                m_externalReferenceTreeViewState = new TreeViewState();
            }
            m_externalReferenceTreeView = new ExternalReferenceTreeView(m_externalReferenceTreeViewState);
            m_externalReferenceTreeView.selectionChangeCB = ExternalReferenceChangeItemCB;
            m_externalReferenceTreeView.doubleClickedItemCB = ExternalReferenceTreeViewDoubleClickedItem;
            m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
            m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
            m_externalReferenceTreeView.Reload();

            if (m_assetTreeViewState == null)
            {
                m_assetTreeViewState = new TreeViewState();
            }
            m_assetTreeView = new AssetTreeView(m_assetTreeViewState);            
            m_assetTreeView.doubleClickedItemCB = AssetReferenceTreeViewDoubleClickedItem;
            m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
            m_assetTreeView.Reload();
            ExternalReferenceTreeViewDoubleClickedItem(1);
        }

        private void OnDestroy()
        {
            
            using ( var bw = new BinaryWriter(new FileStream(m_dataBaseFilePath, FileMode.Create)))
            {
                m_assetBundleDumpData.Serialize(bw);
            }
            
        }

        private void OnProjectChange()
        {
            Debug.Log("OnProjectChange"); 
        }

        private void OnGUI()
        {
            EditorGUI.BeginDisabledGroup(m_historys.Count <= 0);
            {
                var size = EditorStyles.miniButton.CalcSize(Styles.Undo);
                if (GUILayout.Button(Styles.Undo, UnityEngine.GUILayout.MaxWidth(size.x)))
                {
                    if (m_historys != null && m_historys.Count > 0)
                    {
                        var history = m_historys.Pop();
                        if (history != null)
                        {
                            m_targetAssetBundlePath = history.assetBundleName;
                            m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
                            m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
                            m_externalReferenceTreeView.Reload();
                            m_externalReferenceTreeView.SetSelection(new List<int> { history.fileID }, TreeViewSelectionOptions.RevealAndFrame);                            
                            var item = m_externalReferenceTreeView.FindItem(history.fileID) as ExternalReferenceTreeViewItem;
                            if (item != null && item.cabInfo != null)
                            {
                                
                                m_assetTreeView.cabName = item.cabInfo.name;
                            }
                            m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
                            m_assetTreeView.Reload();
                            m_assetTreeView.SetSelection(new List<int> { history.pathID }, TreeViewSelectionOptions.RevealAndFrame);

                        }
                    }
                }
            }
            EditorGUI.EndDisabledGroup();

            using (new EditorGUI.IndentLevelScope())
            {
                // AssetBundleのRootフォルダーを設定する
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel(Styles.AssetBundleRootFolder);
                    EditorGUILayout.TextField(m_assetBundleDumpData.assetBundleRootFolder);
                    if (GUILayout.Button(Styles.BrowseAssetBundleRootFolderLocation))
                    {
                        var path = EditorUtility.OpenFolderPanel("Set AssetBundle Root Folder Location", m_assetBundleDumpData.assetBundleRootFolder, "");
                        if (!string.IsNullOrEmpty(path))
                        {
                            m_assetBundleDumpData.assetBundleRootFolder = path;
                            m_assetBundleDumpData.assetBundleInfos = new AssetBundleInfo[0];
                            m_targetAssetBundlePath = String.Empty;

                            m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
                            m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
                            m_externalReferenceTreeView.Reload();

                            m_assetTreeView.cabName = String.Empty;
                            m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
                            m_assetTreeView.Reload();
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                // AssetBundleの拡張子を指定する
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.PrefixLabel(Styles.AssetBundleExtentions);
                    m_assetBundleDumpData.assetBundleExtentions = EditorGUILayout.TextField(m_assetBundleDumpData.assetBundleExtentions);
                    // AssetBundleのDumpを実行
                    if (GUILayout.Button(Styles.Dump))
                    {
                        Dump();
                        if (m_targetAssetBundlePath != String.Empty)
                        {
                            m_externalReferenceTreeView.Reload();
                            m_externalReferenceTreeView.SetSelection(new List<int> { 1 });
                            ExternalReferenceChangeItemCB();
                        }
                    }
                    if(GUILayout.Button("Deep"))
                    {
                        m_assetBundleDumpData.Analyze();
                    }
                    if (GUILayout.Button(Styles.Clear))
                    {
                        if (Directory.Exists(m_workFolder))
                        {
                            DeleteDirectoryRecursive(m_workFolder);
                            m_assetBundleDumpData.assetBundleInfos = new AssetBundleInfo[0];
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();

                EditorGUI.BeginDisabledGroup(m_assetBundleDumpData.assetBundleInfos.Length == 0);
                {
                    EditorGUILayout.BeginHorizontal();
                    {
                        EditorGUILayout.PrefixLabel(Styles.TargetAssetBundle);
                        EditorGUILayout.TextField(Path.GetFileName(m_targetAssetBundlePath));
                        if (GUILayout.Button(Styles.SelectTargetAssetBundle))
                        {
                            var path = EditorUtility.OpenFilePanel("Select AssetBundle", m_targetAssetBundlePath, "");
                            if (!string.IsNullOrEmpty(path))
                            {
                                m_historys.Clear();

                                m_targetAssetBundlePath = path;
                                m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
                                m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
                                m_externalReferenceTreeView.Reload();
                                m_externalReferenceTreeView.SetSelection(new List<int> { 1 });
                                ExternalReferenceChangeItemCB();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.EndDisabledGroup();

                //
                EditorGUILayout.PrefixLabel(Styles.IncludeFiles);
                var r = EditorGUILayout.GetControlRect(false, 0);
                var h = (position.height - (r.y + r.height + 200)) / 2;
                r = EditorGUILayout.GetControlRect(false, h);
                m_externalReferenceTreeView.OnGUI(r);

                //
                EditorGUILayout.PrefixLabel(Styles.Assets);
                r = EditorGUILayout.GetControlRect(false, 0);
                h = (position.height - (r.y + r.height + 200)) / 2;
                r = EditorGUILayout.GetControlRect(false, h);
                m_assetTreeView.OnGUI(r);
                
            }
        }

        void ExternalReferenceTreeViewDoubleClickedItem(int id)
        {
            var item = m_externalReferenceTreeView.FindItem(m_externalReferenceTreeViewState.lastClickedID) as ExternalReferenceTreeViewItem;
            if((item == null) || (item.cabInfo == null))
            {
                return;
            }
            
            
            // 変更の必要がない場合は処理を抜ける
            if(m_targetAssetBundlePath == item.cabInfo.assetBundleName)
            {
                return;
            }

            var history = new History();
            history.assetBundleName = m_targetAssetBundlePath;
            history.fileID = m_externalReferenceTreeViewState.lastClickedID;
            history.pathID = m_assetTreeViewState.lastClickedID;

            var cabInfo = item.cabInfo;                        
            m_targetAssetBundlePath = cabInfo.assetBundleName;
            m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
            m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
            m_externalReferenceTreeView.Reload();

            item = m_externalReferenceTreeView.FindItem(cabInfo) as ExternalReferenceTreeViewItem;
            m_externalReferenceTreeView.SetSelection(new List<int> { item.id }, TreeViewSelectionOptions.RevealAndFrame);
            m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
            m_assetTreeView.cabName = cabInfo.name;
            m_assetTreeView.Reload();
            m_assetTreeView.SetSelection(new List<int> { 1 }, TreeViewSelectionOptions.RevealAndFrame);

            m_historys.Push(history);
        }


        void ExternalReferenceChangeItemCB()
        {            
            var item = m_externalReferenceTreeView.FindItem(m_externalReferenceTreeViewState.lastClickedID) as ExternalReferenceTreeViewItem;
            m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
            if (item == null)
            {
                m_assetTreeView.cabName = String.Empty;
            }
            else
            {
                if (item.cabInfo == null)
                {
                    m_assetTreeView.cabName = String.Empty;
                }
                else
                {
                    m_assetTreeView.cabName = item.cabInfo.name;
                }
            }
            m_assetTreeView.Reload();
        }
        

        void AssetReferenceTreeViewDoubleClickedItem(int id)
        {
            var history = new History();
            history.assetBundleName = m_targetAssetBundlePath;
            history.fileID = m_externalReferenceTreeViewState.lastClickedID;
            history.pathID = m_assetTreeViewState.lastClickedID;            

            var item = m_assetTreeView.FindItem(id) as AssetTreeViewItem;
            if((item == null) || (item.cabInfo == null))
            {
                return;
            }
            var cabInfo = item.cabInfo;
            var pathID = item.pathID;
            {
                m_targetAssetBundlePath = cabInfo.assetBundleName;
                m_externalReferenceTreeView.assetBundleDumpData = m_assetBundleDumpData;
                m_externalReferenceTreeView.targetAssetBundlePath = m_targetAssetBundlePath;
                m_externalReferenceTreeView.Reload();
                var externalReferenceItem = m_externalReferenceTreeView.FindItem(cabInfo) as ExternalReferenceTreeViewItem;
                if (externalReferenceItem != null)
                {
                    m_externalReferenceTreeView.SetSelection(new List<int> { externalReferenceItem.id }, TreeViewSelectionOptions.RevealAndFrame);
                    m_assetTreeView.assetBundleDumpData = m_assetBundleDumpData;
                    m_assetTreeView.cabName = cabInfo.name;
                    m_assetTreeView.Reload();
                    item = m_assetTreeView.FindItem(pathID) as AssetTreeViewItem;
                    if (item != null)
                    {
                        m_assetTreeView.SetSelection(new List<int> { item.id }, TreeViewSelectionOptions.RevealAndFrame);
                    }
                    m_historys.Push(history);
                }                
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
            Directory.Delete(path, true);
        }


        private void Dump()
        {
            // AssetBundle(のファイルパス)の一覧を取得する
            var fpaths = new List<string>();
            var extentions = m_assetBundleDumpData.assetBundleExtentions.Split(new char[] { ';' });
            foreach (var ext in extentions)
            {
                var assetbundles = Directory.GetFiles(m_assetBundleDumpData.assetBundleRootFolder, ext, SearchOption.AllDirectories);
                fpaths.AddRange(assetbundles);
            }

            // 既存のキャッシュを削除してから、
            if (Directory.Exists(m_casheFolder))
            {
                DeleteDirectoryRecursive(m_casheFolder);
            }
            Directory.CreateDirectory(m_casheFolder);


            var webExtractExec = new WebExtractExec();
            var b2t = new Binary2TextExec();
            var no = 0;
            var hashs = new List<string>();
            var assetBundleInfos = new List<AssetBundleInfo>();            

            try
            {
                foreach (var fpath in fpaths)
                {
                    var assetBundleInfo = new AssetBundleInfo();
                    assetBundleInfos.Add(assetBundleInfo);
                    assetBundleInfo.name = Path.GetFileName(fpath);
                    var progress = (float)no / (float)fpaths.Count;
                    if (EditorUtility.DisplayCancelableProgressBar("AssetBundleDumper", $"Extract AssetBundles... {no}/{fpaths.Count}", progress))
                    {                        
                        break;
                    }
                    var cloneAB = Path.Combine(m_casheFolder, assetBundleInfo.name);
                    File.Copy(fpath, cloneAB, true);
                    var result = webExtractExec.Exec(cloneAB);
                    File.Delete(cloneAB);
                    if (result != 0)
                    {
                        EditorUtility.DisplayDialog("WebExtract Fail", fpath, "OK");
                        throw new System.InvalidProgramException();
                    }
                    // シリアライズファイルの一覧取得
                    // WebExtractはAssetBundle名の後ろに_dataを付けたフォルダを作成する
                    var unpackFolder = cloneAB + "_data";
                    // AssetBundleに含まれるファイルはCAB-から始まるものとBuildPlayer-から始まる２種類が存在する。(ScenesInBuildに含まれるSceneをAssetBundle化した場合、BuildPlayer-から始まるファイルが解凍される)
                    // それ以外には無い筈・・・、あるのであればここで処理を追加する必要あり
                    // AssetBundleに含まれるファイルには拡張子無し・sharedAssets・sharedAssets.Res・resourceの４種類が存在する。
                    // resource/ResファイルにはTextureのバイナリデータ等が含まれテキストファイルには変換出来ない
                    var cabFiles = Directory.GetFiles(unpackFolder, "CAB-*", SearchOption.TopDirectoryOnly);
                    var buildPlayerFiles = Directory.GetFiles(unpackFolder, "BuildPlayer-*", SearchOption.TopDirectoryOnly);
                    var serializeFiles = new string[cabFiles.Length + buildPlayerFiles.Length];
                    for (var i = 0; i < cabFiles.Length; i++)
                    {
                        serializeFiles[i] = cabFiles[i];
                    }
                    for (var i = 0; i < buildPlayerFiles.Length; i++)
                    {
                        serializeFiles[i + cabFiles.Length] = buildPlayerFiles[i];
                    }

                    var cabInfos = new List<CABInfo>();
                    foreach(var serializeFile in serializeFiles)
                    {
                        var serializeFileName = Path.GetFileName(serializeFile);
                        var cabInfo = new CABInfo();
                        cabInfos.Add(cabInfo);
                        cabInfo.assetBundleName = assetBundleInfo.name;
                        cabInfo.name = serializeFileName;
                        if ((Path.GetExtension(serializeFileName) == ".resS") || (Path.GetExtension(serializeFileName) == ".resource"))
                        {
                            // Resourcesファイルはbin2txtでダンプ出来ない為、パスする
                            continue;
                        }
                        // Dumpファイルのパス
                        var dumpFilePath = Path.Combine(unpackFolder, serializeFileName) + ".txt";
                        // from シリアライズファイル to テキストファイル
                        result = b2t.Exec(serializeFile, dumpFilePath, "");
                        if(result == 0)
                        {
                            cabInfo.path = dumpFilePath;
                            AnalyzeCAB(cabInfo, dumpFilePath);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Bin2Text Fail", b2t.output, "OK");
                            Debug.Log(b2t.output);
                            throw new System.InvalidProgramException();
                        }                        
                    }
                    assetBundleInfo.cabInfos = cabInfos.ToArray();
                    no++;
                }

                m_assetBundleDumpData.assetBundleInfos = assetBundleInfos.ToArray();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
            
        }

        private void OnDisable()
        {
            using (BinaryWriter binaryWriter = new BinaryWriter(new FileStream(m_dataBaseFilePath, FileMode.OpenOrCreate)))
            {
                m_assetBundleDumpData.Serialize(binaryWriter);
            }
        }

        void AnalyzeCAB(CABInfo cabInfo,string fpath)
        {
            var externalReferences = new List<string>();
            var assetInfos = new List<AssetInfo>();

            using (StreamReader streamReader = new StreamReader(new FileStream(fpath,FileMode.Open)))
            {
                string line = null;
                while (true)
                {
                    if (line == null)
                    {
                        line = streamReader.ReadLine();
                    }
                    if (line == null)
                    {
                        break;
                    }
                    if(line.StartsWith("path"))
                    {
                        // "path"で始まる行はこんな感じ
                        // path(1): "archive:/CAB-f3c312a9e65553d7dbd7177e0ab68398/CAB-f3c312a9e65553d7dbd7177e0ab68398" GUID: 00000000000000000000000000000000 Type: 0
                        // path(6): "Library/unity default resources" GUID: 0000000000000000e000000000000000 Type: 0
                        // '"'で分割してやるとword[1]は
                        // archive:/CAB-f3c312a9e65553d7dbd7177e0ab68398/CAB-f3c312a9e65553d7dbd7177e0ab68398
                        // Library/unity default resources
                        // となる。
                        var words = line.Split('"');
                        words = words[1].Split('/');
                        if (words[0] == "archive:")
                        {
                            externalReferences.Add(words[2]);
                        }
                        else
                        {
                            externalReferences.Add(words[1]);
                        }
                        line = null;                        
                    }
                    else if (line.StartsWith("ID:"))
                    {


                        // ID: -9222277132543864576 (ClassID: 114) MonoBehaviour
                        var words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                        // IDとClassIDを取得
                        var id = long.Parse(words[1]);
                        var classID = int.Parse(words[3].TrimEnd(')'));

                        var assetInfo = new AssetInfo(id, classID, words[4]);
                        assetInfos.Add(assetInfo);

                        if (classID == 142)
                        {
                            // ClassID 142はAssetBundleで
                            // 最初の行には
                            // m_Name ""(string)
                            // AssetBundle名が来る
                            // m_AssetBundleNameにも入っているが・・・
                            line = streamReader.ReadLine();
                            GetLineWithoutTab(ref line);
                            assetInfo.name = line.Split("\"")[1];

                            // 次の行からプリロードテーブルが始まる
                            // m_PreloadTable  (vector)
                            streamReader.ReadLine();
                            // テーブルのサイズ                            
                            line = streamReader.ReadLine();
                            // size 1 (int)
                            words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                            cabInfo.preloads = new PPtrInfo[int.Parse(words[1])];
                            for(int i = 0; i < cabInfo.preloads.Length; i++)
                            {
                                // この様なブロックで来る
                                // data(PPtr<Object>)
                                // m_FileID 0(int)
                                // m_PathID - 6529559389951329797(SInt64)
                                
                                cabInfo.preloads[i]= new PPtrInfo();

                                streamReader.ReadLine();
                                line = streamReader.ReadLine();
                                words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                cabInfo.preloads[i].fileID = int.Parse(words[1]);

                                line = streamReader.ReadLine();
                                words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                cabInfo.preloads[i].pathID = long.Parse(words[1]);                                                                
                            }
                            line = null;
                        }
                        else
                        {
                            
                            var pptrInfos = new List<PPtrInfo>();
                            while(true)
                            {
                                line = streamReader.ReadLine();
                                var backup = line;
                                if(line == null)
                                {
                                    assetInfo.pptrInfos = pptrInfos.ToArray();
                                    break;
                                }
                                else if(line == string.Empty)
                                {
                                    continue;
                                }
                                var indent = GetLineWithoutTab(ref line);
                                words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                // "ID:"から始まる場合、次のAssetの情報になる為、ライン読み込みを指し戻して終了
                                if (words[0] == "ID:")
                                {
                                    assetInfo.pptrInfos = pptrInfos.ToArray();
                                    line = backup;
                                    break;
                                }
                                else if ((words[0] == "m_Name") && (indent == 1))
                                {
                                    var name = line.Split("\"")[1];
                                    assetInfo.name = name;
                                }
                                else if (words.Length > 1 && words[1].StartsWith("(PPtr"))
                                {
                                    var pptrInfo = new PPtrInfo();                                    
                                    // m_FileID 0(int)
                                    // m_PathID - 6529559389951329797(SInt64)                                    
                                    line = streamReader.ReadLine();
                                    words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                    pptrInfo.fileID = int.Parse(words[1]);

                                    line = streamReader.ReadLine();
                                    words = line.Split(new string[] { " ", "　" }, StringSplitOptions.RemoveEmptyEntries);
                                    pptrInfo.pathID = long.Parse(words[1]);

                                    pptrInfos.Add(pptrInfo);
                                }
                            }
                            assetInfo.pptrInfos = pptrInfos.ToArray();
                            
                        }
                    }
                    else
                    {
                        line = null;
                    }
                }
            }
            cabInfo.externalReferences = externalReferences.ToArray();            
            cabInfo.assetInfos = assetInfos.ToArray();
        }

        int GetLineWithoutTab(ref string line)
        {
            int indent = 0;
            while (line.StartsWith("\t"))
            {
                line = line.Substring(1);
                indent++;
            }
            return indent;
        }
    }
}
