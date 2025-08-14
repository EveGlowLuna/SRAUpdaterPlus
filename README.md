# SRAUpdaterPlus
SRAUpdater 的优化版，基于 C# 重构

---

## 实现的功能

- 更新 SRA
- 自动下载 SRA
- 兼容原版参数

## 下一步目标

- 接入 Mirror 酱
- 解决现有 bug
- 添加暂停下载功能

## 如何使用？

SRAUpdaterPlus 的使用方法与 SRAUpdater 相同。

### 安装方法

1. 下载 [.NET 8.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)
2. 前往 [Release](https://github.com/EveGlowLuna/SRAUpdaterPlus/releases/latest) 下载 SRAUpdater
3. 将 SRA 文件夹中的 SRAUpdater 替换成刚才下载的 SRAUpdater

### 如何卸载
使用 `SRAUpdater.exe -ur` 即可。

### 参数说明

- `-url, -u <URL>`
  
  指定自定义下载链接。

  ```bash
  -url https://example.com/file
  ```

- `-use-proxy, -p <代理地址>`
  
  使用指定的镜像代理进行下载。

  ```bash
  -use-proxy http://proxy.example.com
  ```

- `-disable-proxy, -np`
  
  不使用任何镜像代理。

  ```bash
  -disable-proxy
  ```

- `-disable-SSL, -nv`
  
  禁用 SSL 证书验证。

  ```bash
  -disable-SSL
  ```

- `-force-update, -f`
  
  即使已安装最新版本，也强制更新。

  ```bash
  -force-update
  ```

- `-timeout, -t <秒数>`
  
  设置下载操作的超时时间（默认 10 秒）。

  ```bash
  -timeout 30
  ```

- `-file-integrity-check, -i`
  
  下载后启用文件完整性校验。

  ```bash
  -file-integrity-check
  ```

- `-help, -h`
  
  显示帮助信息。

  ```bash
  -help
  ```

- `-use-sraupdater, -ur`
  
  立即进行一次更新并替换 SRAUpdaterPlus，使用官方 SRAUpdater 软件进行更新。

  ```bash
  -use-sraupdater
  ```