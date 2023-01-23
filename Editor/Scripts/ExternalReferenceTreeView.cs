using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEngine.UI;
using System.Security.Cryptography;
using static Codice.Client.Common.Connection.AskCredentialsToUser;
using System;

namespace UTJ.AssetBundleDumper.Editor
{
    public class ExternalReferenceTreeViewItem : TreeViewItem
    {
        CABInfo m_cabInfo;

        public CABInfo cabInfo
        {
            get { return m_cabInfo; }
            set { m_cabInfo = value; }
        }

        public ExternalReferenceTreeViewItem() : base() { }
    }



    public class ExternalReferenceTreeView : TreeView
    {
#if UNITY_EDITOR_WIN
        private const string k_RevealInFinderLabel = "Show in Explorer";
#elif UNITY_EDITOR_OSX
        private const string k_RevealInFinderLabel = "Reveal in Finder";
#else
        private const string k_RevealInFinderLabel = "Open Containing Folder";
#endif

        public Action selectionChangeCB;
        public Action<int> doubleClickedItemCB;

        public AssetBundleDumpData assetBundleDumpData
        {
            set { m_assetBundleDumpData = value; }
        }

        public string targetAssetBundlePath
        {
            set { m_targetAssetBundlePath = value; }
        }
        

        AssetBundleDumpData m_assetBundleDumpData;
        string m_targetAssetBundlePath;

        ExternalReferenceTreeViewItem m_root;


        public ExternalReferenceTreeView(TreeViewState state) : base(state)
        {
            showBorder = false;
            showAlternatingRowBackgrounds = true;
            
        }

        public TreeViewItem FindItem(int id)
        {
            return FindItem(id, rootItem);
        }

        public TreeViewItem FindItem(CABInfo cabInfo)
        {
            var items = GetRows();
            foreach(var item in items)
            {
                var extItem = (ExternalReferenceTreeViewItem)item;
                if(extItem.cabInfo == cabInfo)
                {
                    return item;
                }
            }
            return null;
        }

        void RevealInFinder(object obj)
        {
            EditorUtility.RevealInFinder((string)obj);
        }

        protected override void ContextClickedItem(int id)
        {
            base.ContextClickedItem(id);
            var menu = new GenericMenu();

            var item = FindItem(id) as ExternalReferenceTreeViewItem;
            if (item == null || item.cabInfo == null || item.cabInfo.path == string.Empty)
            {
                menu.AddDisabledItem(new GUIContent(k_RevealInFinderLabel));
            }
            else
            {
                menu.AddItem(new GUIContent(k_RevealInFinderLabel), false, RevealInFinder, item.cabInfo.path);
            }

            menu.ShowAsContext();
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            if(selectionChangeCB != null)
            {
                selectionChangeCB();
            }
        }


        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            if(doubleClickedItemCB != null)
            {
                doubleClickedItemCB(id);
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            int depth = -1;
            var root = new ExternalReferenceTreeViewItem { id = 0, depth = depth, displayName = "Root" };
            var assetBundleName = Path.GetFileName(m_targetAssetBundlePath);
            var assetBundleInfo =  m_assetBundleDumpData.GetAssetBundleInfo(assetBundleName);

            m_root = root;

            if (assetBundleInfo == null) 
            {
                var viewItem = new ExternalReferenceTreeViewItem { id = 1, depth = 1, displayName = "" };
                root.AddChild(viewItem);
                return root;
            }            

            var id = 1;
            depth = 0;
            // AssetBundle直下のCAB
            foreach(var cabInfo in assetBundleInfo.cabInfos)
            {
                var treeViewItem = CreateTreeViewItem(cabInfo, id++,depth);
                root.AddChild(treeViewItem);
            }
            // 各CAB内のExternal Referecnecsを再帰的に表示していく
            for(depth = 0; depth < m_assetBundleDumpData.externalReferenceDepth; depth++)
            {
                if(CreateRows(ref id, depth) <= 0)
                {
                    break;
                }
            }

            return root;
        }

        ExternalReferenceTreeViewItem CreateTreeViewItem(CABInfo cabInfo,int id,int depth)
        {
            var treeViewItem = new ExternalReferenceTreeViewItem { id = id,depth = depth};
            treeViewItem.cabInfo = cabInfo;
            if (cabInfo.assetBundleName == null || cabInfo.assetBundleName == string.Empty)
            {
                treeViewItem.displayName = $"{cabInfo.name}";
            }
            else
            {
                treeViewItem.displayName = $"{cabInfo.assetBundleName}/{cabInfo.name}";
            }
            return treeViewItem;
        }

        ExternalReferenceTreeViewItem CreateTreeViewItem(string dispName, int id, int depth)
        {
            var treeViewItem = new ExternalReferenceTreeViewItem { id = id, depth = depth };            
            return treeViewItem;
        }

        // 指定された深さのItemに対して子のItemを作成する
        int CreateRows(ref int id,int depth)
        {
            int count = 0;

            // 指定されたdepthのItemのリストを作成
            var externalReferenceTreeViewItems = new List<TreeViewItem>();
            FindDepthItem(depth,ref externalReferenceTreeViewItems);
           
            foreach(var treeViewItem  in externalReferenceTreeViewItems)
            {
                ExternalReferenceTreeViewItem externalReferenceTreeViewItem = (ExternalReferenceTreeViewItem)treeViewItem;

                foreach (var cabName in externalReferenceTreeViewItem.cabInfo.externalReferences)
                {
                    var cabInfo = m_assetBundleDumpData.GetCABInfo(cabName);
                    if (cabInfo == null) 
                    {
                        var childItem = new ExternalReferenceTreeViewItem { id = ++id, depth = depth + 1,displayName = cabName };
                        treeViewItem.AddChild(childItem);
                    }
                    else
                    {
                        var childItem = CreateTreeViewItem(cabInfo, ++id, depth + 1);
                        treeViewItem.AddChild(childItem);
                    }
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 指定されたdepthのTreeViewItemの一覧を取得
        /// </summary>
        /// <param name="depth"></param>
        /// <param name="treeViewItems"></param>
        void FindDepthItem(int depth,ref List<TreeViewItem> treeViewItems)
        {
            FindDepthItemsRecursive(m_root, depth, ref treeViewItems);
        }

        /// <summary>
        /// 指定されてdepthのTreeViewItemを再帰的に取得していく
        /// </summary>
        /// <param name="treeViewItem"></param>
        /// <param name="depth"></param>
        /// <param name="treeViewItems"></param>
        void FindDepthItemsRecursive(TreeViewItem treeViewItem,int depth,ref List<TreeViewItem> treeViewItems)
        {
            if(treeViewItem.depth == depth)
            {
                treeViewItems.Add(treeViewItem);
                return;
            }
            if(treeViewItem.depth > depth)
            {
                // ここは来ない筈
                return;
            }
            if (treeViewItem.hasChildren)
            {
                foreach (var childItem in treeViewItem.children)
                {
                    FindDepthItemsRecursive(childItem, depth, ref treeViewItems);
                }
            }
        }
    }
}