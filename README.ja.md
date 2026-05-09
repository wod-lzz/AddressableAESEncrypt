# AddressableAESEncrypt

* Languages: [中文](README.md) | 日本語 | [English](README.md#en)

* 既存の AES Build Script / AES AssetBundle Provider の統合フローを維持
* 方針: Addressables のソースコードには手を入れない
* ベース環境: Unity 2022.2.9 / Addressables 1.21.9
* ビルド手順: Build → New Build → AES Build Script
* 暗号化済み/未暗号化 bundle が混在する場合でも、対象 Group の AssetBundleProvider は引き続き AES AssetBundle Provider に統一可能
* 現在の実装は AssetBundle 全体を AES 暗号化する方式ではなく、軽量なヘッダー難読化方式に変更済み

## 現在の方式
* 各 AssetBundle の先頭 256 バイトのみを処理し、シーク可能なストリーム経由で読み込み時にリアルタイム復元する
* ビルド時は bundle 全体を読み直して再書き込みするのではなく、ヘッダーだけをその場で更新する
* Build Script はカスタム Group Schema による選択的処理をサポートしており、コアリソースを含む Group のみを難読化できる
* 実行時は引き続きカスタム AssetBundleProvider と SeekableAesStream で読み込み、Addressables 本体の改修なしで、難読化済み bundle と通常 bundle の両方を自動判別して扱える
* bundle 全体を暗号化する方式と比べて、ビルド時間・読み込み時間・メモリ確保量を大きく削減できる

## 選択的暗号化の設定
* AES Build Script は既定で従来挙動を維持し、`encryptAllBundles = true` の場合はすべての bundle を処理する
* コアリソースだけを暗号化したい場合は、それらを専用 Group に分けて `encryptAllBundles` を無効化する
* Addressables Groups ウィンドウで対象 Group を選択し、`Add Schema` から `AES Encryption` を追加する
* `AES Encryption` Schema 内で `Encrypt Bundle` を有効にする
* 実行時は引き続き AES AssetBundle Provider を統一利用でき、provider 側で bundle ヘッダーが難読化済みかどうかを自動判別する

## 想定ユースケース
* 基本的なアセット保護は必要だが、ビルド速度と実行時ロード性能をより重視するプロジェクトに向いている
* 強固なセキュリティ対策として使う用途には向かず、本質的には完全暗号化ではなく高速な難読化である
* 保護強度を少し上げたい場合は、SeekableAesStream 内の EscapeLength を 256 から 1024 または 4096 に引き上げられる

## ソースコード概要
* Assets/AddressableAssetsData/Extends/SeekableAesStream.cs
	シーク可能なヘッダー難読化/復元ロジックを担当
* Assets/AddressableAssetsData/Extends/Editor/BuildScriptAESPackedMode.cs
	ビルド完了後に bundle ヘッダーをその場で処理する
* Assets/AddressableAssetsData/Extends/Editor/AesEncryptionGroupSchema.cs
	Group Inspector 上で「この Group を暗号化するか」を宣言する
* Assets/AddressableAssetsData/Extends/AesAssetBundleProvider.cs
	実行時にストリーム経由で bundle を読み込み、ヘッダーを復元する
