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

    public class DependencyTreeViewItem : TreeViewItem
    {
        public string m_Hash;
        public string hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }

        public DependencyTreeViewItem() : base() { }
    }


    public class DependencyTreeView : TreeView
    {
        AssetBundleDumpData m_AssetBundleDumpData;
        string m_AssetBundleHash;
        bool m_IsBuild;

        public bool IsBuild
        {
            get { return m_IsBuild; }
        }


        public DependencyTreeView(TreeViewState state) : base(state)
        {
            m_IsBuild = false;
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new DependencyTreeViewItem { id = 0, depth = -1,hash = string.Empty,displayName = "Root" };
            var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[m_AssetBundleHash];
            var item = new DependencyTreeViewItem { id = root.id+1, depth = root.depth + 1, hash = m_AssetBundleHash, displayName = Path.GetFileName(fpath) + "(" + m_AssetBundleHash  + ")"};
            root.AddChild(item);            
            int id = item.id + 1;
            Dependency(item, m_AssetBundleHash, ref id);
            return root;
        }

        public void Rebuild(AssetBundleDumpData assetBundleDumpData,string hash)
        {
            m_AssetBundleDumpData = assetBundleDumpData;
            m_AssetBundleHash = hash;
            Reload();
            m_IsBuild = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="parentItem">親のTreeViewItem</param>
        /// <param name="hash"></param>
        /// <param name="_id">TreeViewItemのID</param>
        /// <returns></returns>
        bool Dependency(TreeViewItem parentItem,string hash,ref int _id)
        {
            string assetBundleFilePath;
            var result = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths.TryGetValue(hash, out assetBundleFilePath);
            if (!result)
            {
                return false;
            }
            string dumpFilePath;
            result = m_AssetBundleDumpData.m_Hash2DumpFilePaths.TryGetValue(hash, out dumpFilePath);
            if (!result)
            {
                return false;
            }
            using (StreamReader sr = new StreamReader(new FileStream(dumpFilePath, FileMode.Open)))
            {
                string line = sr.ReadLine();
                if (line != "External References")
                {
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
                        var item = new DependencyTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = string.Empty, displayName = words[1] };
                        parentItem.AddChild(item);                     
                    }
                    else
                    {
                        var childHash = words[2];
                        var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[childHash];

                        var item = new DependencyTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = childHash, displayName = Path.GetFileName(fpath) + "(" + childHash + ")" };
                        parentItem.AddChild(item);

                        // 循環参照している場合は、処理を打ち切る
                        bool IsCirculation = false;
                        var checkItem = (DependencyTreeViewItem)parentItem.parent;
                        while (checkItem != null)
                        {
                            if (checkItem.hash == childHash)
                            {
                                IsCirculation = true;
                                Debug.LogWarning($"{item.displayName} is circular reference.");
                                break;
                            }
                            checkItem = (DependencyTreeViewItem)checkItem.parent;
                        }
                        if (!IsCirculation)
                        {
                            Dependency(item, childHash, ref _id);
                        }
                    }
                }
            }
            return true;
        }
    }
}