using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEditor.Build.Content;
using UnityEngine.Assertions.Must;
using Unity.VisualScripting.YamlDotNet.Core.Tokens;
using UnityEditor.Search;
using System.Security.Policy;
using static UnityEditor.Progress;
using static GluonGui.WorkspaceWindow.Views.Checkin.Operations.CheckinViewDeleteOperation;

namespace UTJ.UnityAssetBundleDumper.Editor
{
    public class ReferenceInfo
    {
        string m_Hash;
        long m_PathID;

        public string hash
        {
            get { return m_Hash; }
            set { m_Hash = value; }
        }
        public long pathID
        {
            get { return m_PathID; }
            set { m_PathID = value; }
        }

        public override bool Equals(object obj)
        {
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                ReferenceInfo pPtrInfo = (ReferenceInfo)obj;
                return ((m_Hash == pPtrInfo.m_Hash) && (m_PathID == pPtrInfo.m_PathID));
            }
        }

        public override int GetHashCode()
        {
            var hashCode = m_Hash.GetHashCode();
            hashCode = (hashCode * 397) ^ m_PathID.GetHashCode();
            return hashCode;
        }
    }


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

        bool m_IsReference;
        public bool IsReference
        {
            get { return m_IsReference; }
            set { m_IsReference = value; }
        }
    }


    public class AssetReferenceTreeView : TreeView
    {
        AssetBundleDumpData m_AssetBundleDumpData;
        string m_AssetBundleHash;
        bool m_IsBuild;
        Dictionary<ReferenceInfo, AssetReferenceTreeViewItem> m_ReferenceInfo2AssetReferenceTreeViewItems;
        Stack<int> m_Undo;
        Stack<int> m_Redo;
        string m_SearchHash;
        int m_SearchIndex;
        List<AssetReferenceTreeViewItem> m_SearchItems;
        public delegate void ChangeAssetBundleAction(string hash1,string hash2,long pathID,int id);
        public ChangeAssetBundleAction changeAssetBundleAction;


        public bool IsBuild
        {
            get { return m_IsBuild; }
        }


        public AssetReferenceTreeView(TreeViewState state) : base(state)
        {
            m_AssetBundleHash = string.Empty;
            m_IsBuild = false;
            showBorder = true;
            m_Undo  = new Stack<int>();
            m_Redo = new Stack<int>();
            m_SearchItems = new List<AssetReferenceTreeViewItem>();
            showAlternatingRowBackgrounds = true;
        }


        public void SearchHashTreeViewItem(string hash)
        {
            AssetReferenceTreeViewItem item;
            if (m_SearchHash == hash)
            {
                m_SearchIndex++;
                if(m_SearchItems.Count <= m_SearchIndex)
                {
                    m_SearchIndex = 0;
                }                
            }
            else
            {                
                m_SearchItems.Clear();
                m_Undo.Clear();
                m_Redo.Clear();
                CollectHashItemRecurive(hash,rootItem as AssetReferenceTreeViewItem, ref m_SearchItems);
                if(m_SearchItems.Count == 0)
                {
                    return;
                }
                m_SearchIndex = 0;
                m_SearchHash = hash;
            }
            if (m_SearchItems.Count > m_SearchIndex)
            {
                var selections = GetSelection();
                if (selections != null && selections.Count > 0)
                {
                    m_Undo.Push(selections[0]);
                }
                item = m_SearchItems[m_SearchIndex];
                FrameItem(item.id);
                SetSelection(new List<int> { item.id });
            }
        }

        protected void CollectHashItemRecurive(string hash, AssetReferenceTreeViewItem item,ref List<AssetReferenceTreeViewItem> list)
        {
            if(item == null)
            {
                return;
            }
            if(item.hash == hash)
            {
                list.Add(item);
            }
            if (item.hasChildren)
            {
                foreach (TreeViewItem child in item.children)
                {
                    CollectHashItemRecurive(hash, child as AssetReferenceTreeViewItem, ref list);
                }
            }
        }

    
        protected void BuildSub(TreeViewItem parent,string hash,long pathID,ref int treeID)
        {
            if ((hash == "unity default resources")||(hash == "unity_builtin_extra"))
            {
                string displayName = $"ID: {pathID} Reference Asset: {hash}";
                var assetInfoItem = new AssetReferenceTreeViewItem
                {
                    id = treeID++,
                    depth = parent.depth + 1,
                    displayName = displayName,
                    hash = hash,
                    pathID = pathID,                    
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
                        displayName += $" => Reference AssetBundle: {Path.GetFileName(fpath)} ({hash})";
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

                    // àŸÇ»ÇÈAssetBundleÇ…ä‹Ç‹ÇÍÇƒÇ¢ÇÈAssetÇÃèÍçáÇÕèàóùÇë≈ÇøêÿÇÈ
                    if (hash != m_AssetBundleHash)
                    {
                        continue;
                    }

                    if (hash == m_AssetBundleHash && assetInfoItem.depth != 0)
                    {
                        assetInfoItem.displayName += " [...]";
                        assetInfoItem.IsReference = true;
                        continue;
                    }

                    var referenceInfo = new ReferenceInfo { hash = hash, pathID = pathID };
                    result = m_ReferenceInfo2AssetReferenceTreeViewItems.ContainsKey(referenceInfo);
                    if (result)
                    {
                        assetInfoItem.displayName += " [...]";
                        assetInfoItem.IsReference = true;
                        continue;
                    }
                    else
                    {
                        m_ReferenceInfo2AssetReferenceTreeViewItems.Add(referenceInfo, assetInfoItem);
                    }
                  
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
                        foreach (var pptrInfo in assetDumpInfo.PPtrInfos)
                        {                     
                            if (pptrInfo.fileID == 0 && pptrInfo.pathID == 0)
                            {
                                // null
                                continue;
                            }
                            var path = assetBundleDumpInfo.paths[pptrInfo.fileID];
                            var pptrHash = Path.GetFileNameWithoutExtension(path);
                            //var pptrHash = path;
                            BuildSub(assetInfoItem, pptrHash, pptrInfo.pathID, ref treeID);
                        }                        
                    }
                    break;
                }
            }            
        }


        protected override TreeViewItem BuildRoot()
        {
            m_Undo.Clear();
            m_Redo.Clear();
            m_SearchItems.Clear();
            m_SearchHash = string.Empty;

            m_ReferenceInfo2AssetReferenceTreeViewItems = new Dictionary<ReferenceInfo, AssetReferenceTreeViewItem>();
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
                        var cancel = EditorUtility.DisplayCancelableProgressBar("AssetBundleDumper", "Build TreeView", progress);
                        if (cancel)
                        {
                            break;
                        }
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

        protected override void DoubleClickedItem(int id)
        {            
            var item = this.FindItem(id, rootItem) as AssetReferenceTreeViewItem;
            // àŸÇ»ÇÈAssetBundleÇÃèÍçá
            if (item.hash != m_AssetBundleHash)
            {
                changeAssetBundleAction(item.hash,item.hash,item.pathID,id);                
            }
            else if (item.IsReference)
            {
                SelectItem(item.hash, item.pathID);
            }
        }

        public int GetSelectItem()
        {
            var selections = GetSelection();
            if(selections == null || selections.Count <= 0)
            {
                return 0;
            }
            return selections[0];
        }


        public void SelectItem(string hash,long pathID)
        {
            var referenceInfo = new ReferenceInfo { hash = hash, pathID = pathID };
            var result = m_ReferenceInfo2AssetReferenceTreeViewItems.ContainsKey(referenceInfo);
            if (result)
            {                
                var referenceItem = m_ReferenceInfo2AssetReferenceTreeViewItems[referenceInfo];
                SelectItem(referenceItem.id);                
            }
        }

        public void SelectItem(int id)
        {
            FrameItem(id);
            SetSelection(new List<int> { id });
        }


        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }        
    }

}