using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEngine.UI;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using UnityEditorInternal.Profiling.Memory.Experimental;
using System;

namespace UTJ.AssetBundleDumper.Editor
{

    public class AssetTreeViewItem : TreeViewItem
    {
        CABInfo m_cabInfo;
        long m_pathID;

        public CABInfo cabInfo
        {
            get { return m_cabInfo; }
            set { m_cabInfo = value; }
        }

        public long pathID
        {
            get { return m_pathID; }
            set { m_pathID = value; }
        }

    }


    public class AssetTreeView : TreeView
    {
        public Action<int> doubleClickedItemCB;

                
        public AssetBundleDumpData assetBundleDumpData
        {
            set { m_assetBundleDumpData = value; }
        }
        public string cabName
        {
            get { return m_cabName; }
            set { m_cabName = value; }
        }

        AssetBundleDumpData m_assetBundleDumpData;
        string m_cabName;



        public AssetTreeView(TreeViewState state) : base(state)
        {
            showBorder = false;
            showAlternatingRowBackgrounds = true;
        }

        public TreeViewItem FindItem(int id)
        {
            return FindItem(id, rootItem);
        }

        public TreeViewItem FindItem(long pathID)
        {
            var items = GetRows();
            foreach(var item in items)
            {
                var assetTreeViewItem = item as AssetTreeViewItem;
                if(assetTreeViewItem == null)
                {
                    continue;
                }
                if(assetTreeViewItem.depth != 0)
                {
                    continue;
                }
                if(assetTreeViewItem.pathID == pathID)
                {
                    return item;
                }
            }
            return null;
        }

        protected override void DoubleClickedItem(int id)
        {
            base.DoubleClickedItem(id);
            if (doubleClickedItemCB != null)
            {
                doubleClickedItemCB(id);
            }
        }

        protected override TreeViewItem BuildRoot()
        {         
            var cabInfo = m_assetBundleDumpData.GetCABInfo(m_cabName);
            int depth = -1;
            int id = 0;
            TreeViewItem root = new TreeViewItem { id = id++, depth = depth, displayName = "Root" };
            if (cabInfo == null)
            {
                var viewItem = new TreeViewItem { id = id++, depth = 0, displayName = "" };
                root.AddChild(viewItem);
            }
            else if(cabInfo.assetInfos.Length == 0)
            {
                var viewItem = new TreeViewItem { id = id++, depth = 0, displayName = "" };
                root.AddChild(viewItem);
            }
            else
            {                
                foreach(var assetInfo in cabInfo.assetInfos)
                {
                    string displayName = $"ID: {assetInfo.id} (ClassID: {assetInfo.classID}) {assetInfo.objectName} [Name: \"{assetInfo.name}\"]";

                    var viewItem = new AssetTreeViewItem { id = id++, depth = 0, displayName = displayName };
                    viewItem.pathID = assetInfo.id;
                    viewItem.cabInfo = cabInfo;
                    root.AddChild(viewItem);
                    foreach(var pptrInfo in assetInfo.pptrInfos)
                    {
                        CABInfo externalCabInfo;

                        // null‚Ìê‡ApathID‚ª0‚É‚È‚é
                        if (pptrInfo.pathID == 0)
                        {
                            continue;
                        }
                        // “¯ˆêCAB“à‚Å‚ÌŽQÆ
                        else if (pptrInfo.fileID == 0)
                        {
                            var refAssetInfo = cabInfo.GetAssetInfo(pptrInfo.pathID);
                            displayName = $"ID: {refAssetInfo.id} (ClassID: {refAssetInfo.classID}) {refAssetInfo.objectName} [Name: \"{refAssetInfo.name}\"]";
                            var refItem = new AssetTreeViewItem { id = id++, depth = 1, displayName = displayName };
                            refItem.cabInfo = cabInfo;
                            refItem.pathID = pptrInfo.pathID;
                            viewItem.AddChild(refItem);
                        }
                        else
                        {
                            var cabName = cabInfo.externalReferences[pptrInfo.fileID - 1];
                            externalCabInfo = m_assetBundleDumpData.GetCABInfo(cabName);

                            if (externalCabInfo != null)
                            {
                                var externalAssetInfo = externalCabInfo.GetAssetInfo(pptrInfo.pathID);
                                displayName = $"ID: {externalAssetInfo.id} (ClassID: {externalAssetInfo.classID}) {externalAssetInfo.objectName} [Name: \"{externalAssetInfo.name}\"] => {externalCabInfo.assetBundleName}/{cabName}";
                                var externalItem = new AssetTreeViewItem { id = id++, depth = 1, displayName = displayName };
                                externalItem.cabInfo = externalCabInfo;
                                externalItem.pathID = pptrInfo.pathID;
                                viewItem.AddChild(externalItem);
                            }
                            else
                            {
                                displayName = $"{cabName} ID: {pptrInfo.pathID}";
                                var externalItem = new TreeViewItem { id = id++, depth = 1, displayName = displayName };
                                viewItem.AddChild(externalItem);
                            }
                        }
                    }
                }
            }
            return root;
        }
    }
}