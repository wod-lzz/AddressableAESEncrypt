# AddressableAESEncrypt

* 保留原有 AES Build Script / AES AssetBundle Provider 接入方式
* 原则：不入侵Addressable源码
* Base：Unity 2022.2.9 Addressable 1.21.9
* 打包：Build -- New Build -- AES Build Script
* 需要加载混合加密/未加密包时，相关 Group 的 AssetBundleProvider 仍统一设置为 AES AssetBundle Provider
* 当前实现已调整为轻量级头部混淆，不再对整个 AssetBundle 做完整 AES 加密


## 当前方案
* 仅处理 AssetBundle 前 256 字节，用可随机访问的流在读取时实时还原
* 构建阶段只原地改写头部，不再整包 ReadAllBytes 后重写
* Build Script 现支持通过自定义 Group Schema 勾选是否加密，只对核心资源所在 Group 做头部混淆
* 运行时继续通过自定义 AssetBundleProvider + SeekableAesStream 加载，不需要修改 Addressables 源码，并且可自动兼容已混淆和未混淆的 bundle
* 相比整包加密，这种方式显著降低了打包耗时、加载耗时和内存分配


## 选择性加密配置
* AES Build Script 默认保持历史行为：`encryptAllBundles = true` 时对所有 bundle 处理
* 如需仅加密核心资源：把核心资源拆到独立 Group，然后把 `encryptAllBundles` 关闭
* 在 Addressables Groups 窗口中选中目标 Group，点击 `Add Schema`，添加 `AES Encryption`
* 在 `AES Encryption` Schema 里勾选 `Encrypt Bundle`
* 运行时可继续统一使用 AES AssetBundle Provider，provider 会自动识别 bundle 是否已做头部混淆


## 适用场景
* 适合需要基础资源保护，同时更关注构建速度和运行时加载速度的项目
* 不适合把它当作强安全方案；它本质上是快速混淆，不是完整内容加密
* 如果需要稍微提升保护强度，可以把 SeekableAesStream 里的 EscapeLength 从 256 提高到 1024 或 4096


## 源码说明
* Assets/AddressableAssetsData/Extends/SeekableAesStream.cs
	负责可随机访问的头部混淆/还原逻辑
* Assets/AddressableAssetsData/Extends/Editor/BuildScriptAESPackedMode.cs
	负责打包完成后原地处理 bundle 头部
* Assets/AddressableAssetsData/Extends/Editor/AesEncryptionGroupSchema.cs
	负责在 Group Inspector 中声明“该 Group 是否需要加密”
* Assets/AddressableAssetsData/Extends/AesAssetBundleProvider.cs
	负责运行时通过流方式加载并解密头部


# en
* Keeps the existing AES Build Script / AES AssetBundle Provider integration flow
* Principle: Do not modify the Addressable source code.
* Base: Unity 2022.2.9 Addressables 1.21.9
* Build Process: Build → New Build → AES Build Script
* Configuration: For mixed encrypted/plain bundles, keep the relevant Groups on AES AssetBundle Provider.
* The current implementation now uses lightweight header obfuscation instead of full AssetBundle AES encryption.

## Current Approach
* Only the first 256 bytes of each AssetBundle are transformed and restored through a seekable stream.
* The build step updates the bundle header in place instead of reading and rewriting the whole file.
* The build script now supports selective processing through a custom Group Schema, so only core resource Groups need to be transformed.
* Runtime loading still works through the custom AssetBundleProvider plus SeekableAesStream, without modifying Addressables source code, and it auto-detects transformed vs plain bundles.
* Compared with full-bundle encryption, this greatly reduces build time, load time, and memory usage.

## Selective Encryption Setup
* The AES Build Script keeps the old behavior by default: when `encryptAllBundles = true`, every bundle is transformed.
* To encrypt only core resources, move them into dedicated Groups, disable `encryptAllBundles`, add the `AES Encryption` schema to those Groups, and enable `Encrypt Bundle`.
* The AES AssetBundle Provider can stay on those Groups even in mixed mode because it now detects whether the bundle header was transformed.

## Use Cases
* Good for projects that need basic asset protection but care more about build and loading performance.
* This should not be treated as strong security; it is fast obfuscation rather than full content encryption.
* If you want slightly stronger protection, increase EscapeLength in SeekableAesStream from 256 to 1024 or 4096.

## Source Code Description:
* Assets/AddressableAssetsData/Extends/SeekableAesStream.cs
	Handles seekable header obfuscation and restoration.
* Assets/AddressableAssetsData/Extends/Editor/BuildScriptAESPackedMode.cs
	Updates bundle headers in place after the build finishes.
* Assets/AddressableAssetsData/Extends/AesAssetBundleProvider.cs
	Loads encrypted bundles at runtime through the stream-based path.
