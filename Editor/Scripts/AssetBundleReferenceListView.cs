using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System.IO;
using UnityEditor;

namespace UTJ.UnityAssetBundleDumper.Editor
{
    public class AssetBundleReferenceListView : AssetBundleReferenceTreeView
    {
        public AssetBundleReferenceListView(TreeViewState state) : base(state)
        {
        }

        protected override bool Dependency(TreeViewItem parentItem, string hash, ref int _id)
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
                    
                    string[] words = line.Split('"');
                    words = words[1].Split('/');
                    if (words[0] == "Library")
                    {
                        // path(2): "Library/unity default resources" GUID: 0000000000000000e000000000000000 Type: 0
                        var item = new AssetBundleReferenceTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = words[1], displayName = words[1] };
                        

                        if (!m_DependencyFileList.Contains(item.displayName))
                        {
                            parentItem.AddChild(item);
                            m_DependencyFileList.Add(item.displayName);
                        }
                    }
                    else
                    {
                        // path(1): "archive:/BuildPlayer-SampleScene/BuildPlayer-SampleScene.sharedAssets" GUID: 00000000000000000000000000000000 Type: 0
                        // path(2): "archive:/CAB-56bb25c0e5ea7af2a5c41a1994f98568/CAB-56bb25c0e5ea7af2a5c41a1994f98568" GUID: 00000000000000000000000000000000 Type: 0                    

                        var childHash = words[2];
                        var fpath = m_AssetBundleDumpData.m_Hash2AssetBundleFilePaths[childHash];

                        var item = new AssetBundleReferenceTreeViewItem { id = ++_id, depth = parentItem.depth + 1, hash = childHash, displayName = Path.GetFileName(fpath) + "(" + childHash + ")" };
                        
                        // 自分自身は依存リストには含めない
                        if (m_DependencyFileList.Contains(item.displayName) || item.hash == m_AssetBundleHash)
                        {
                            continue;                            
                        }
                        parentItem.AddChild(item);
                        m_DependencyFileList.Add(item.displayName);
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
                            Dependency(parentItem, childHash, ref _id);
                        }
                    }
                }
            }

            EditorUtility.ClearProgressBar();

            return true;
        }
    }

}