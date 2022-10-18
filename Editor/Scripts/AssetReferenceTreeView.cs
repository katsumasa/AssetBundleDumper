using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEditor.Build.Content;
using UnityEngine.Assertions.Must;

namespace UTJ.UnityAssetBundleDumper.Editor
{

    public class AssetReferenceTreeViewItem : TreeViewItem
    {
        string m_Hash;
        public string hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }

        long m_PathID;
        
        public long pathID
        {
            get { return m_PathID; }
            set { m_PathID = value; }
        }

    }


    public class AssetReferenceTreeView : TreeView
    {
        AssetBundleDumpData m_AssetBundleDumpData;
        string m_AssetBundleHash;
        bool m_IsBuild;


        public bool IsBuild
        {
            get { return m_IsBuild; }
        }


        public AssetReferenceTreeView(TreeViewState state) : base(state)
        {
            m_AssetBundleHash = string.Empty;
            m_IsBuild = false;
            showBorder = true;            
        }

    
        protected void BuildSub(TreeViewItem parent,string hash,long pathID,ref int treeID)
        {
            if ((hash == "unity default resources")||(hash == "unity_builtin_extra"))
            {
                string displayName = $"ID: {pathID} Reference Asset: {hash}";
                var assetInfoItem = new TreeViewItem
                {
                    id = treeID++,
                    depth = parent.depth + 1,
                    displayName = displayName,
                };
                parent.AddChild(assetInfoItem);
                return;
            }


            AssetBundleDumpInfo assetBundleDumpInfo;

            var result = m_AssetBundleDumpData.m_Hash2AssetBundleBundeInfo.TryGetValue(hash, out assetBundleDumpInfo);
            if (result == false)
            {
                var assetInfoItem = new AssetReferenceTreeViewItem
                {
                    id = treeID++,
                    depth = parent.depth + 1,
                    displayName = $"{hash} is not exist.",
                };
                assetInfoItem.hash = hash;
                assetInfoItem.pathID = pathID;
                parent.AddChild(assetInfoItem);

                Debug.LogError($"{hash} is not exist.");
                return;
            }

            var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[hash];

            foreach(var assetDumpInfo in assetBundleDumpInfo.assetDumpInfos)            
            {
                if (assetDumpInfo.id == pathID)
                {
                    string name = "Empty";
                    if (!string.IsNullOrEmpty(assetDumpInfo.name))
                    {
                        name = assetDumpInfo.name;
                    }
                    string displayName = $"ID: {assetDumpInfo.id} Name: {name} ClassID: {assetDumpInfo.classID} ({assetDumpInfo.objectName})";

                    if (hash != m_AssetBundleHash)
                    { 
                        displayName += $" => Reference AssetBundle: {Path.GetFileName(fpath)}({hash})";
                    }

                    var assetInfoItem = new AssetReferenceTreeViewItem
                    {
                        id = treeID++,
                        depth = parent.depth + 1,
                        displayName = displayName,
                        hash = hash,
                        pathID = pathID,
                    };
                    
                    parent.AddChild(assetInfoItem);




                    bool IsCirculation = false;
                    var checkItem = (AssetReferenceTreeViewItem)parent;
                    while(checkItem != null)
                    {
                        if((checkItem.pathID == pathID) && (checkItem.hash == hash))
                        {
                            IsCirculation = true;

                            assetInfoItem.displayName += " [Circular Reference]";
                            break;
                        }
                        checkItem = checkItem.parent as AssetReferenceTreeViewItem;
                    }
                    if (!IsCirculation)
                    {
                        var i = 0;
                        foreach (var pptrInfo in assetDumpInfo.PPtrInfos)
                        {
                            var progress = (float)i / (float)assetDumpInfo.PPtrInfos.Length;
                            EditorUtility.DisplayProgressBar("AssetBundleDumper", $"Build TreeView depth({assetInfoItem.depth}) TreeID:{treeID}", progress);
                            i++;
                            if (pptrInfo.fileID == 0 && pptrInfo.pathID == 0)
                            {
                                // null
                                continue;
                            }
                            var path = assetBundleDumpInfo.paths[pptrInfo.fileID];
                            var pptrHash = Path.GetFileNameWithoutExtension(path);
                            BuildSub(assetInfoItem, pptrHash, pptrInfo.pathID, ref treeID);
                        }
                        EditorUtility.ClearProgressBar();
                    }
                    break;
                }
            }
            
        }


        protected override TreeViewItem BuildRoot()
        {                        
            int id = 0;
            var root = new AssetReferenceTreeViewItem { id = id++, depth = -1, displayName = "Root" };
            if (m_AssetBundleDumpData != null && m_AssetBundleDumpData.m_Hash2AssetBundleBundeInfo != null)
            {
                AssetBundleDumpInfo assetBundleDumpInfo;
                var result = m_AssetBundleDumpData.m_Hash2AssetBundleBundeInfo.TryGetValue(m_AssetBundleHash, out assetBundleDumpInfo);
                if (result)
                {
                    var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[m_AssetBundleHash];
                    int i = 0;                    
                    foreach (var assetDumpInfo in assetBundleDumpInfo.assetDumpInfos)
                    {
                        var progress = (float)i / (float)assetBundleDumpInfo.assetDumpInfos.Length;
                        EditorUtility.DisplayProgressBar("AssetBundleDumper", "Build TreeView", progress);
                        BuildSub(root, m_AssetBundleHash, assetDumpInfo.id, ref id);
                        i++;
                    }
                    EditorUtility.ClearProgressBar();
                }
                else
                {
                    var dummy = new AssetReferenceTreeViewItem { id = id++, depth = 0, displayName = "" };
                    root.AddChild(dummy);
                }
            }
            else
            {
                var dummy = new AssetReferenceTreeViewItem { id = id++, depth = 0, displayName = "" };
                root.AddChild(dummy);
            }
            m_IsBuild = true;
            return root;
        }


        public void Rebuild(AssetBundleDumpData assetBundleDumpData, string hash)
        {
            m_AssetBundleDumpData = assetBundleDumpData;
            m_AssetBundleHash = hash;
            Reload();         
        }
    }

}