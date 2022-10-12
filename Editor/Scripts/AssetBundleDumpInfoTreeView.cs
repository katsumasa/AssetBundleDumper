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

    public class AssetBundleDumpInfoTreeView : TreeView
    {

        AssetBundleDumpData m_AssetBundleDumpData;
        string m_AssetBundleHash;
        bool m_IsBuild;

        public bool IsBuild
        {
            get { return m_IsBuild; }
        }


        public AssetBundleDumpInfoTreeView(TreeViewState state) : base(state)
        {
            m_AssetBundleHash = string.Empty;
            m_IsBuild = false;
        }

    

        protected override TreeViewItem BuildRoot()
        {
            AssetBundleDumpInfo assetBundleDumpInfo;
            
            var result = m_AssetBundleDumpData.m_Hash2AssetBundleBundeInfo.TryGetValue(m_AssetBundleHash, out assetBundleDumpInfo);
            if(result == false)
            {
                return null;
            }

            int id = 0;
            var root = new TreeViewItem { id = id++, depth = -1, displayName = "Root" };
            var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[m_AssetBundleHash];
            var assetBundleInfoItem = new TreeViewItem { id = id++, depth = root.depth + 1, displayName = Path.GetFileName(fpath) + "(" + m_AssetBundleHash + ")" };
            root.AddChild(assetBundleInfoItem);

            
            var assetInfoRootItem = new TreeViewItem
            {
                id = id++,
                depth = assetBundleInfoItem.depth + 1,
                displayName = "AssetInfo"
            };
            assetBundleInfoItem.AddChild(assetInfoRootItem);

            for(var i = 0; i < assetBundleDumpInfo.assetDumpInfos.Length; i++)
            {
                var assetInfo = assetBundleDumpInfo.assetDumpInfos[i];
                string name = "Empty";
                if(!string.IsNullOrEmpty(assetInfo.name))
                {
                    name = assetInfo.name;
                }
                
                var assetInfoItem = new TreeViewItem {
                    id = id++, 
                    depth = assetInfoRootItem.depth + 1, 
                    displayName = $"ID: {assetInfo.id} Name: {name} (ClassID: {assetInfo.classID}) {assetInfo.objectName}"
                };
                assetBundleInfoItem.AddChild(assetInfoItem);
                for(var j = 0; j < assetInfo.PPtrInfos.Length; j++)
                {
                    var pptrInfo = assetInfo.PPtrInfos[j];
                    
                    if (pptrInfo.m_FileID == 0 && pptrInfo.m_PathID == 0)
                    {
                        // null
                        continue;
                    }           
                    var path = assetBundleDumpInfo.paths[pptrInfo.m_FileID];
                    var hash = Path.GetFileNameWithoutExtension(path);
                    if(hash == "unity default resources")
                    {
                        continue;
                    }


                    AssetBundleDumpInfo reference;
                    if (m_AssetBundleDumpData.m_Hash2AssetBundleBundeInfo.TryGetValue(hash, out reference))
                    {

                        for (var k = 0; k < reference.assetDumpInfos.Length; k++)
                        {
                            var assetDumpInfo = reference.assetDumpInfos[k];
                            if (assetDumpInfo.id == pptrInfo.m_PathID)
                            {
                                name = "Empty";
                                if (!string.IsNullOrEmpty(assetDumpInfo.name))
                                {
                                    name = assetDumpInfo.name;
                                }
                                string displyName;
                                if (hash == m_AssetBundleHash)
                                {
                                    displyName = $"ID: {assetDumpInfo.id}  Name: {name} (ClassID: {assetDumpInfo.classID}) {assetDumpInfo.objectName}";
                                }
                                else
                                {
                                    path = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[hash];
                                    
                                    displyName = $"ID: {assetDumpInfo.id}  Name: {name} (ClassID: {assetDumpInfo.classID}) {assetDumpInfo.objectName} ref {Path.GetFileName(path)}({hash})";
                                }

                                var pptrItem = new TreeViewItem
                                {
                                    id = id++,
                                    depth = assetInfoItem.depth + 1,
                                    displayName = displyName,

                                };
                                assetInfoItem.AddChild(pptrItem);
                                break;
                            }
                        }
                    }
                    else
                    {
                        var pptrItem = new TreeViewItem
                        {
                            id = id++,
                            depth = assetInfoItem.depth + 1,
                            displayName = $"ID: {pptrInfo.m_FileID} ref {Path.GetFileName(path)}({hash})",
                        };
                        assetInfoItem.AddChild(pptrItem);                        
                    }
                }
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