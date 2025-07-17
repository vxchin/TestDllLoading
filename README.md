# DLL 加载优先级测试项目

## 项目简介
本项目用于测试和演示在 .NET 8 环境下多种 DLL 加载方式的优先级和行为，示例以 `sqlite3.dll` 为例，涵盖：
- 默认 DllImport 加载
- 使用 SetDllDirectory 控制 DLL 搜索路径
- 使用 LoadLibrary 手动加载 DLL
- 使用 .NET NativeLibrary 类加载 DLL

项目支持 x64 和 x86 架构，主程序会自动检测当前进程架构，并优先选择对应目录下的 DLL 进行加载。

## 主要功能
- 测试系统 API（如 kernel32.dll）和第三方 DLL（如 sqlite3.dll）的加载与调用
- 展示不同加载方式的优缺点和适用场景
- 提供详细的错误处理、加载路径和错误码信息
- 自动输出当前进程架构、应用程序目录等环境信息

## DLL 文件与项目配置
- 根目录下的 `sqlite3.dll` 与 `x86\sqlite3.dll` 文件内容完全相同，均为 32 位 DLL。
- 在 x64 平台上，默认加载方式（DllImport）会优先尝试加载根目录的 DLL，由于架构不匹配，加载会失败。
- 若需在 x64 平台下正常加载，请确保 `x64\sqlite3.dll` 为 64 位 DLL。
- 项目已在 TestDllLoading.csproj 中配置自动复制所有 DLL（根目录及 x64/x86 子目录）到输出目录，无需手动操作。

## 运行方法
1. 编译项目，目标框架为 .NET 8。
2. 运行程序后，控制台将自动输出当前架构、应用目录，并依次测试各加载方式，显示 DLL 加载结果、版本信息及详细错误说明。
3. 所有依赖 DLL 已自动复制到输出目录，无需手动干预。

## 测试方法说明
- **方法0: 默认加载方式**  
  直接通过 DllImport 声明调用 `sqlite3_libversion`，依赖系统默认搜索路径。
  - 在 x64 平台下，由于根目录 DLL 为 32 位，加载会失败。
- **方法1: 使用 SetDllDirectory**  
  通过设置 DLL 搜索目录，优先在架构目录查找 DLL。
- **方法2: 使用 LoadLibrary 手动加载**  
  手动加载 DLL 并通过 GetProcAddress 获取函数指针，动态调用。
- **方法3: 使用 .NET NativeLibrary 类**  
  使用 .NET 提供的 NativeLibrary API 加载 DLL 并获取导出函数。

每种方法均有详细异常处理，加载失败时会输出错误码或异常信息，便于排查问题。

## 扩展性说明
- 项目已包含 sqlite3.dll 作为测试示例，可自行替换为其他 DLL 进行测试。
- 若需扩展测试其他 DLL，请在 NativeMethods 类中添加对应 DllImport 声明，并复用现有测试流程。

## 文件结构
- Program.cs：主程序入口及测试逻辑
- x64/、x86/：架构专用 DLL 存放目录
- TestDllLoading.csproj：项目配置文件（自动复制 DLL）
- README.md：项目说明文档

## 适用场景
- .NET 桌面应用开发中 DLL 加载问题排查
- 多架构兼容性测试
- 动态/手动加载第三方或自定义 DLL

---
如有问题或建议，欢迎反馈！
