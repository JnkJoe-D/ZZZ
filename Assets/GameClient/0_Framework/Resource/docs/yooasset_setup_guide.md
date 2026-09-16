# YooAsset 热更新系统配置与踩坑指南

本指南记录了基于 YooAsset 2.3.17 实现的资源管理系统的配置流程、服务端部署及常见问题的解决方法。

---

## 1. 架构总览

系统采用 **“固定链接 -> 动态版本”** 的解耦设计。
- **底包 (App)**：版本号固定，指向固定的远程服务器目录。
- **资源包 (Bundle)**：通过服务器根目录下的 `.version` 文件动态指定当前最新的资源文件夹。

---

## 2. 客户端配置 (Unity)

### ResourceConfig (ScriptableObject)
- **Play Mode**: 
    - `EditorSimulateMode`: 开发期使用，无需打包。
    - `HostPlayMode`: 正式环境使用，从 CDN 下载更新。
- **Default Package Name**: 必须与 YooAsset 打包窗口填写的 `Package Name` 一致（例如 `Test1`）。
- **Append Version To URL**: 
    - **建议关闭**。关闭后，URL 指向 `/CDN/PC/`。
    - 这样即使 App 版本不变，也能通过修改服务器文件来更新资源。

### Project Settings
- **Allow downloads over HTTP**: 必须设为 `Always allowed`，否则无法连接非 HTTPS 的阿里云服务器。
- **StreamingAssets**: 首次打包后，必须将 `BuildinFiles` 拷贝至 `StreamingAssets/yoo/[PackageName]/` 目录下，作为引擎启动的初始“户口本”。

---

## 3. 服务端部署 (宝塔 Linux 面板)

### 目录结构 (以 PC 平台为例)
路径映射关系：`http://[IP]/CDN/PC/` -> `/www/wwwroot/[SiteName]/CDN/PC/`

```text
CDN/
└── PC/
    ├── Test1.version       <-- [核心控制文件] 内容为当前最新目录名 (如: 1393)
    ├── 2026-02-22-1392/    <-- 旧版本目录
    └── 2026-02-22-1393/    <-- 最新资源目录
```

### 宝塔设置要点
1. **添加站点**：必须为站点配置正确的公网 IP 或域名，否则 404。
2. **权限**：确保目录权限为 `755`，所有者为 `www`。
3. **MIME 类型**：如果 `.bundle` 或 `.hash` 文件下载失败，需在 Nginx 添加 `application/octet-stream bundle;`。

---

## 4. 常见报错与排查 (FAQ)

### Q1: `404 Not Found` (URL 指向 StreamingAssets)
- **原因**：YooAsset 启动时找不到内置清单。
- **解决**：拷贝打包产物的 `BuildinFiles` 到 `Assets/StreamingAssets/yoo/[PackageName]/`。

### Q2: `Insecure connection not allowed`
- **原因**：Unity 默认禁止 HTTP，只允许 HTTPS。
- **解决**：在 `Player Settings` 中将 `Allow downloads over HTTP` 设为 `Always allowed`。

### Q3: `404 Not Found` (URL 指向服务器 .version)
- **原因**：
    1. 宝塔没有添加站点导致路径不通。
    2. URL 中的版本号（Application.version）与服务器文件夹名不匹配。
    3. `Test1.version` 文件放在了子文件夹里，而不是根目录。
- **解决**：先在浏览器直接输入该 URL 访问测试，确保能看到版本号文字。

---

## 5. 增量更新操作流

1. **Unity**：修改资源 -> `AssetBundle Builder` -> `Build` (版本号 +1)。
2. **Server**：将生成的新日期文件夹上传。
3. **Server**：修改 `CDN/PC/[PackageName].version` 的内容为最新的日期字符串。
4. **Result**：客户端启动，自动感知版本变化并进入增量下载流程。
