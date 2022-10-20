# AssetBundleDumper

## 概要

ビルド済みのAssetBundleをダンプしてAssetBundleに含まれるAssetの情報やAssetBundle間の依存関係を表示するパッケージです。
<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/196863022-81036341-1817-48aa-a215-1a91c0cd7072.png">


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
4. Browseボタンを押して調査するAssetBundleを選択する。(Hash値のプルダウンメニューからもAssetを選択することが可能）

## AssetBundleDumperWindow

Window > UTJ > AssetBundleDumperで起動します。

### AssetBundle Settings

ここではDumpを行うAssetBundleが置かれたフォルダーやAssetBundleの検索パターンを設定します。

#### AssetBundle Folder

Dumpを行うビルド済みAssetBundleのフォルダーを指定します。Browseボタンを押すことでRootフォルダーを指定する為のFolderPanelが開きます。プラットフォーム毎のRootフォルダーを指定してください。

#### Search Pattern

AssetBundleの[サーチパターン](https://learn.microsoft.com/ja-jp/dotnet/api/system.io.directory.getfiles?view=net-6.0)を指定します。`;`で区切ることで複数のパターンを指定することが可能です。

例) 拡張子を持たない全てのファイル </br>
`.*` </br>
 
例)拡張子が`.a`と`.b`の全てのファイル </br>
`*.a;*.b` </br>

#### Dump

AssetBundleをダンプします。対象となるAssetBundleの数に応じて処理時間が長くなります。

#### Delete Dump Cache

Dumpによって生成されたCacheファイルを削除します。(CacheファイルはProjectのLibraryフォルダ以下に作成されます)

#### AssetBundle Path

Browseボタンを押すことでDump結果を確認したいAssetBundleを指定することが可能です。

#### AssetBundle Name

選択されたAssetBundle名が表示されます。

#### Hash

選択されたAssetBundleのHash値が表示されます。プルダウンメニューからHash値を切り替えることが可能です。（Hash値を切り替えることで、AssetBundleの切り替えが行われます。）


### Asset Reference TreeView

![1ba94a649b92132a3b2e7180ccf5da1f](https://user-images.githubusercontent.com/29646672/196867615-964fb254-1de5-4a61-8619-9f41ca00de63.gif)

対象となるAssetBundleに含まれるAssetと、そのAssetから参照されているAssetをTree表示します。
参照されているAssetをダブルクリックすることで、参照されているAssetが定義されている箇所へジャンプすることが可能です。

#### Undo/Redo

TreeViewItemをダブルクリックして発生したJumpのUndo/Redoを行います。

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


### AssetBundle Reference TreeView

<img width="400" alt="image" src="https://user-images.githubusercontent.com/29646672/196111350-6054a32e-0f48-4bc1-9988-92d372fd2b55.png">

AssetBundleから参照されているAssetBundleの名前とHash値をTree表示で再帰的に表示します。
※直接参照されていないAssetBundleが含まれている事に注意して下さい。
TreeViewItemをダブルクリックするとAsset Reference TreeViewdでそのAssetBundleから参照されているAssetTreeViewItemへ表示位置が切り替わります。
![97f93cd8d441259710051734cfcd619d](https://user-images.githubusercontent.com/29646672/196866988-15a9bf51-ecf7-4680-8410-171b14d44bb4.gif)

### AssetBundle Reference ListView

AssetBundle Reference TreeViewと似ていますが、重複しない形でリスト表示します。
<img width="400" alt="image" src="https://user-images.githubusercontent.com/29646672/196111452-26c83468-52f9-4e60-9b04-b8ac4ce95a02.png">


## その他

要望・不具合等ありましたらIssueをご利用下さい。可能な限り対応します。




