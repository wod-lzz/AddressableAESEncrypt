# AddressableAESEncrypt

* 保留原有 AES Build Script / AES AssetBundle Provider 接入方式
* 原则：不入侵Addressable源码
* Base：Unity 2022.2.9 Addressable 1.21.9
* 打包：Build -- New Build -- AES Build Script
* 所有Group的AssetBundleProvider设置为AES AssetBundle Provider
* 当前实现已调整为轻量级头部混淆，不再对整个 AssetBundle 做完整 AES 加密


## 当前方案
* 仅处理 AssetBundle 前 256 字节，用可随机访问的流在读取时实时还原
* 构建阶段只原地改写头部，不再整包 ReadAllBytes 后重写
* 运行时继续通过自定义 AssetBundleProvider + SeekableAesStream 加载，不需要修改 Addressables 源码
* 相比整包加密，这种方式显著降低了打包耗时、加载耗时和内存分配


## 适用场景
* 适合需要基础资源保护，同时更关注构建速度和运行时加载速度的项目
* 不适合把它当作强安全方案；它本质上是快速混淆，不是完整内容加密
* 如果需要稍微提升保护强度，可以把 SeekableAesStream 里的 EscapeLength 从 256 提高到 1024 或 4096


## 源码说明
* Assets/AddressableAssetsData/Extends/SeekableAesStream.cs
	负责可随机访问的头部混淆/还原逻辑
* Assets/AddressableAssetsData/Extends/Editor/BuildScriptAESPackedMode.cs
	负责打包完成后原地处理 bundle 头部
* Assets/AddressableAssetsData/Extends/AesAssetBundleProvider.cs
	负责运行时通过流方式加载并解密头部


# en
* Keeps the existing AES Build Script / AES AssetBundle Provider integration flow
* Principle: Do not modify the Addressable source code.
* Base: Unity 2022.2.9 Addressables 1.21.9
* Build Process: Build → New Build → AES Build Script
* Configuration: Set the AssetBundleProvider of all Groups to AES AssetBundle Provider.
* The current implementation now uses lightweight header obfuscation instead of full AssetBundle AES encryption.

## Current Approach
* Only the first 256 bytes of each AssetBundle are transformed and restored through a seekable stream.
* The build step updates the bundle header in place instead of reading and rewriting the whole file.
* Runtime loading still works through the custom AssetBundleProvider plus SeekableAesStream, without modifying Addressables source code.
* Compared with full-bundle encryption, this greatly reduces build time, load time, and memory usage.

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
