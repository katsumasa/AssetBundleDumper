using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEngine.UI;
using System.Security.Cryptography;

namespace UTJ.UnityAssetBundleDumper.Editor
{

    /// <summary>
    /// TreeViewItem
    /// </summary>
    public class AssetBundleReferenceTreeViewItem : TreeViewItem
    {
        public string m_Hash;
        public string hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }

        public AssetBundleReferenceTreeViewItem() : base() { }
    }


    public class AssetBundleReferenceTreeView : TreeView
    {
        public delegate void DoubleClickedAction(string hash);
        public delegate void ChangeAssetBundleAction(string hash1, string hash2, long pathID, int id);

        public DoubleClickedAction doubleClickedAction { get; set; }
        public ChangeAssetBundleAction changeAssetBundleAction { get; set; }

        protected AssetBundleDumpData m_AssetBundleDumpData;
        protected string m_AssetBundleHash;
        protected bool m_IsBuild;
        protected List<string> m_DependencyFileList;
        protected AssetReferenceTreeView m_assetReferenceTreeView;

                
        public bool IsBuild
        {
            get { return m_IsBuild; }
        }

        public List<string> DependencyFileList
        {
            get { return m_DependencyFileList; }
        }


        public AssetBundleReferenceTreeView(TreeViewState state) : base(state)
        {
            m_IsBuild = false;
            showBorder = false;
            showAlternatingRowBackgrounds = true;
        }

        private void ChangeAssetBundleCB(object obj)
        {
            var id = (int)obj;
            var item = this.FindItem(id, rootItem) as AssetBundleReferenceTreeViewItem;
            if (item != null)
            {
                if (changeAssetBundleAction != null) { changeAssetBundleAction(item.hash, item.hash ,- 1,-1); }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Jump"),false, ChangeAssetBundleCB,id);
            menu.ShowAsContext();
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = this.FindItem(id, rootItem) as AssetBundleReferenceTreeViewItem;
            if(item == null) { return; }
            var hash = item.hash;

            if(doubleClickedAction != null)
            {
                doubleClickedAction(hash);
            }            
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        protected override TreeViewItem BuildRoot()
        {
            m_DependencyFileList = new List<string>();

            TreeViewItem root;
            
            if (m_AssetBundleDumpData != null && m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths != null)
            {                
                var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[m_AssetBundleHash];
                root = new AssetBundleReferenceTreeViewItem { id = 0, depth = -1, hash = m_AssetBundleHash, displayName = Path.GetFileName(fpath) + "(" + m_AssetBundleHash + ")" };                
                int id = root.id + 1;
                Dependency(root, m_AssetBundleHash, ref id);
            }
            else
            {
                root = new AssetBundleReferenceTreeViewItem { id = 0, depth = -1, hash = string.Empty, displayName = "Root" };                
            }
            
            if(root.children == null)
            {
                var dummy = new AssetReferenceTreeViewItem { id = root.id + 1, depth = 0, displayName = "" };
                root.AddChild(dummy);
            }

            m_IsBuild = true;
            return root;
        }

        public void Rebuild(AssetBundleDumpData assetBundleDumpData,string hash)
        {
            m_AssetBundleDumpData = assetBundleDumpData;
            m_AssetBundleHash = hash;
            Reload();            
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentItem">親のTreeViewItem</param>
        /// <param name="hash"></param>
        /// <param name="_id">TreeViewItemのID</param>
        /// <returns></returns>
        protected virtual bool Dependency(TreeViewItem parentItem,string hash,ref int _id)
        {
            string assetBundleFilePath = string.Empty;
            var result = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths.TryGetValue(hash, out assetBundleFilePath);
            if (!result)
            {
                return false;
            }
            
            string dumpFilePath = string.Empty;
            result = m_AssetBundleDumpData.m_Hash2DumpFilePaths.TryGetValue(hash, out dumpFilePath);
            if (!result)
            {
                return false;
            }
            
            var info = Path.GetFileName(dumpFilePath);
            EditorUtility.DisplayProgressBar("AssetBundleDumper", info, 0f);
            
            using (StreamReader sr = new StreamReader(new FileStream(dumpFilePath, FileMode.Open)))
            {                
                string line = sr.ReadLine();
                if (line != "External References")
                {
                    EditorUtility.ClearProgressBar();

                    return false;
                }
                var children = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("path") == false)
                    {
                        break;
                    }
                    if (line.Contains("Resources"))
                    {
                        continue;
                    }
                    // path(1): "archive:/BuildPlayer-SampleScene/BuildPlayer-SampleScene.sharedAssets" GUID: 00000000000000000000000000000000 Type: 0
                    // path(2): "archive:/CAB-56bb25c0e5ea7af2a5c41a1994f98568/CAB-56bb25c0e5ea7af2a5c41a1994f98568" GUID: 00000000000000000000000000000000 Type: 0                    
                    string[] words = line.Split('"');
                    words = words[1].Split('/');
                    if (words[0] == "Library")
                    {
                        // path(2): "Library/unity default resources" GUID: 0000000000000000e000000000000000 Type: 0
                        var item = new AssetBundleReferenceTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = words[1], displayName = words[1] };
                        parentItem.AddChild(item);

                        if (!m_DependencyFileList.Contains(item.displayName) && item.hash != m_AssetBundleHash)
                        {
                            m_DependencyFileList.Add(item.displayName);
                        }
                    }
                    else
                    {
                        var childHash = words[2];
                        if(m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths.ContainsKey(childHash) == false)
                        {
                            Debug.Log(childHash);
                            continue;
                        }

                        var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[childHash];

                        var item = new AssetBundleReferenceTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = childHash, displayName = Path.GetFileName(fpath) + "(" + childHash + ")" };
                        parentItem.AddChild(item);
                        // 自分自身は依存リストには含めない
                        if (!m_DependencyFileList.Contains(item.displayName) && item.hash != m_AssetBundleHash)
                        {
                            m_DependencyFileList.Add(item.displayName);
                        }
                        // 循環参照している場合は、処理を打ち切る
                        bool IsCirculation = false;
                        var checkItem = (AssetBundleReferenceTreeViewItem)parentItem.parent;
                        while (checkItem != null)
                        {
                            if (checkItem.hash == childHash)
                            {
                                IsCirculation = true;
                                item.displayName += " [Circular Reference]";                                
                                break;
                            }
                            checkItem = (AssetBundleReferenceTreeViewItem)checkItem.parent;
                        }
                        if (!IsCirculation)
                        {                            
                            Dependency(item, childHash, ref _id);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
            return true;
        }
    }
}