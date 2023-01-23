# AssetBundleDumper

## 概要

ビルド済みのAssetBundleをダンプしてAssetBundleに含まれるAssetの情報やAssetBundle間の依存関係を表示するパッケージです。
<img width="800" alt="image" src="https://user-images.githubusercontent.com/29646672/213997131-1fc3d454-6c30-429e-ac8b-8a96669b41fa.png">


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

#### AssetBundle Root

Dumpを行うビルド済みAssetBundleのフォルダーを指定します。Browseボタンを押すことでRootフォルダーを指定する為のFolderPanelが開きます。プラットフォーム毎のRootフォルダーを指定してください。

#### Filters

AssetBundleの[サーチパターン](https://learn.microsoft.com/ja-jp/dotnet/api/system.io.directory.getfiles?view=net-6.0)を指定します。`;`で区切ることで複数のパターンを指定することが可能です。

例) 拡張子を持たない全てのファイル </br>
`.*` </br>
 
例)拡張子が`.a`と`.b`の全てのファイル </br>
`*.a;*.b` </br>

#### Dump

AssetBundleをダンプします。対象となるAssetBundleの数に応じて処理時間が長くなります。


#### Target AssetBundle

Dump結果を確認するAssetBundleを選択します、


### Include Files

AssetBundleに含まれるCABファイルとそのCABファイルから参照しているCABファイルのヒエラルキーを表示します。ダブルクリックするとそのCABファイルを含むAssetBundleに表示を切り替えます。

###　Assets

CABに含まれるAssetsとそのAssetsから参照しているAssetsのヒエラルキーを表示します。
ダブルクリックすると、そのAssetが含まれるAssetBundleへ表示を切り替えます。

### Undo

ダブルクリックして切り替えされた表示を戻します。

## その他

要望・不具合等ありましたらIssueをご利用下さい。可能な限り対応します。




