# AssetBundleDumper

## 概要

ビルド済みのAssetBundleをダンプしてAssetBundleに含まれるAssetの情報やAssetBundle間の依存関係を表示するパッケージです。
<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/196111070-0036b978-9ade-4ef7-8810-268cf56f64ad.png">


## 動作環境

Unity2021.3.7f1 + Windows11にて開発を行っています。動作確認は行っていませんが、Unity2017以降のUnityであれば恐らく動作すると思われます。
※AssetBundleをビルドしたUnityEditorと同じバージョンのUnityEditorを使用して下さい。

## インストール方法

本パッケージは[UnityComandLineTools](https://github.com/katsumasa/UnityCommandLineTools.git)パッケージを使用している為、合わせてインストールを行う必要があります。

1. Window > Package ManagerでPackage Managerを開く
2. Package Manager左上の+のプルダウンメニューからAdd package form git URL...を選択する
3. ダイアログへhttps://github.com/katsumasa/UnityCommandLineTools.git を設定し、Addボタンを押す
4. Package Manager左上の+のプルダウンメニューからAdd package form git URL...を選択する
5. ダイアログへhttps://github.com/katsumasa/AssetBundleDumper.git を設定し、Addボタンを押す


## 基本的な使い方

1. Window > UTJ > AssetBundleDumperでAssetBundleDumperWindowを起動する
2. Browseボタンを押して、ビルド済みAssetBundleが置かれたフォルダを選択する
3. Dumpボタンを押してビルド済みAssetBundleをダンプする(AssetBundleの数に応じて処理時間が増加します。)
4. Selectボタンを押して調査するAssetBundleを選択する。(Hash値のプルダウンメニューからもAssetを選択することが可能）

## AssetBundleDumperWindowの説明

Window > UTJ > AssetBundleDumperで起動します。

### AssetBundle DataBase

#### AssetBundle Folder

ビルド済みAssetBundleのフォルダー。Browseボタンを押すことでRootフォルダーを指定する為のFolderPanelが開きます。プラットフォーム毎のRootフォルダーを指定してください。

#### Search Pattern

AssetBundleのサーチパターンを指定します。`;`で区切ることで複数のパターンを指定することが可能です。

例) 拡張子を持たない全てのファイル </br>
`.*` </br>
 
例)拡張子が`.a`と`.b`の全てのファイル </br>
`*.a;*.b` </br>

#### Dump

AssetBundleをダンプします。対象となるAssetBundleの数に応じて処理時間が長くなります。

#### Delete Dump Cache

Dumpによって生成されたCacheファイルを削除します。(DumpファイルはProjectのLibraryフォルダ以下に作成されます)

### Asset Reference TreeView

対象となるAssetBundleに含まれる各Assetから参照されているAssetをTree表示します。

<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/195803425-d3652d4b-eaf6-4184-b65d-29e746546985.png">

#### ID

AssetBundle内の各Assetに割り当てられたユニークなIDですが、全てのAssetBundleを通してユニークという訳ではありません。
又、ID自体には特に重要な意味はありません。

#### Name

Asset名。名前が付けられていない場合、`Empty`と表示されます。

#### ClassID

Assetの[ClassID](https://docs.unity3d.com/ja/current/Manual/ClassIDReference.html)とClass名を表示します。

#### Reference AssetBundle

そのAssetが別のAssetBundleから参照されている場合、そのAssetBundle名とHash値を表示します。

#### Reference Asset

そのAssetの参照先がAssetBundleではなく`unity default resources`や`unity_builtin_extra`の場合、その名称を表示します。

#### Circular Reference

Assetの参照が循環している場合、末尾に`[Circular Reference]`と表示してそれ以降の表示を中断します。


### AssetBundle Reference TreeView

<img width="400" alt="image" src="https://user-images.githubusercontent.com/29646672/196111350-6054a32e-0f48-4bc1-9988-92d372fd2b55.png">

AssetBundleから参照されているAssetBundleの名前とHash値をTree表示で再帰的に表示します。
循環参照が発生している場合、末尾に`[Circular Reference]`と表示してそれ以降の表示を中断します。

### AssetBundle Reference ListView

AssetBundleから参照されているAssetBundleを直接参照されているだけではなく再帰的に調べて重複しない形でリスト表示します。

<img width="400" alt="image" src="https://user-images.githubusercontent.com/29646672/196111452-26c83468-52f9-4e60-9b04-b8ac4ce95a02.png">


## その他

要望・不具合等ありましたらIssueをご利用下さい。可能な限り対応します。





