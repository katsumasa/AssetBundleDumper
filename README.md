# AssetBundleDumper

## 概要

ビルド済みのAssetBundleをダンプしてAssetBundleに含まれるAssetの情報やAssetBundle間の依存関係を表示することが出来るパッケージです。

<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/195793413-f27afd9c-3bb9-45ad-b289-0739e5a55b50.png">

## 使い方

1. Window > UTJ > AssetBundleDumperでAssetBundleDumperを起動する
2. Browseボタンを押して、ビルド済みAssetBundleが置かれたフォルダを選択する
3. Dumpボタンを押してビルド済みAssetBundleをダンプする(AssetBundleの数に応じて処理時間が増加します。)
4. Selectボタンを押して調査するAssetBundleを選択します。


### Asset Reference TreeView

対象となるAssetBundleに含まれる各Assetから参照されているAssetをTree表示します。

<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/195803425-d3652d4b-eaf6-4184-b65d-29e746546985.png">

#### ID

AssetBundle内の各Assetに割り当てられたユニークなIDですが、全てのAssetBundleを通してユニークという訳ではありません。

#### Name

Asset名。名前が付けられていない場合、`Empty`と表示されます。

#### ClassID

AssetのClassIDとClass名を表示します。

#### Reference AssetBundle

そのAssetが別のAssetBundleから参照されている場合、AssetBundle名とHash値を表示します。

#### Reference Asset

そのAssetの参照先がAssetBundleではなく`unity default resources`や`unity_builtin_extra`の場合、その名称を表示します。

#### Circular Reference

Assetの循環参照が発生している場合、`Circular Reference`と表示してそれ意向の表示を終了します。
