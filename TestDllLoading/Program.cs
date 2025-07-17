using System.Runtime.InteropServices;

Console.WriteLine("=== DLL 加载优先级测试 ===");
Console.WriteLine($"当前进程架构: {(Environment.Is64BitProcess ? "x64" : "x86")}");
Console.WriteLine($"应用程序目录: {AppDomain.CurrentDomain.BaseDirectory}");
Console.WriteLine();

TestMethod0_DefaultImport();
Console.WriteLine();
TestMethod1_SetDllDirectory();
Console.WriteLine();
TestMethod2_LoadLibrary();
Console.WriteLine();
TestMethod3_NativeLibrary();
Console.WriteLine();
return;

// 方法0：默认 DllImport 加载
static void TestMethod0_DefaultImport()
{
    Console.WriteLine("方法0: 默认 DllImport 加载");
    TryGetSqliteVersion(NativeMethods.Sqlite3LibVersion, "默认加载");
}

// 方法1：使用 SetDllDirectory 控制 DLL 搜索路径
static void TestMethod1_SetDllDirectory()
{
    Console.WriteLine("方法1: 使用 SetDllDirectory 控制搜索路径");
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    var archDir = Path.Combine(baseDir, Environment.Is64BitProcess ? "x64" : "x86");
    if (Directory.Exists(archDir))
    {
        Console.WriteLine($"设置 DLL 搜索目录: {archDir}");
        if (NativeMethods.SetDllDirectory(archDir))
        {
            TryGetSqliteVersion(NativeMethods.Sqlite3LibVersion, "SetDllDirectory 加载");
            NativeMethods.SetDllDirectory(null); // 清除设置
            Console.WriteLine("已清除 DLL 搜索路径设置");
        }
        else
        {
            Console.WriteLine($"设置 DLL 目录失败: {Marshal.GetLastWin32Error()}");
        }
    }
    else
    {
        Console.WriteLine($"警告: 架构目录不存在: {archDir}");
        Console.WriteLine("将尝试在当前目录加载 DLL...");
        TryGetSqliteVersion(NativeMethods.Sqlite3LibVersion, "当前目录加载");
    }
}

// 方法2：使用 LoadLibrary 手动加载 DLL
static void TestMethod2_LoadLibrary()
{
    Console.WriteLine("方法2: 使用 LoadLibrary 手动加载 DLL");
    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
    string[] dllPaths =
    [
        Path.Combine(baseDir, Environment.Is64BitProcess ? "x64" : "x86", "sqlite3.dll"),
        Path.Combine(baseDir, "sqlite3.dll"),
        "sqlite3.dll"
    ];
    foreach (var dllPath in dllPaths)
    {
        Console.WriteLine($"尝试从路径加载: {dllPath}");
        var hModule = NativeMethods.LoadLibrary(dllPath);
        if (hModule != IntPtr.Zero)
        {
            Console.WriteLine("LoadLibrary 成功");
            try
            {
                var procAddr = NativeMethods.GetProcAddress(hModule, "sqlite3_libversion");
                if (procAddr != IntPtr.Zero)
                {
                    Console.WriteLine("找到 sqlite3_libversion 函数地址");
                    var func = Marshal.GetDelegateForFunctionPointer<Sqlite3LibVersionDelegate>(procAddr);
                    TryGetSqliteVersion(() => func(), "LoadLibrary 调用");
                }
                else
                {
                    Console.WriteLine("未找到 sqlite3_libversion 函数");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调用函数时出错: {ex.Message}");
            }
            finally
            {
                NativeMethods.FreeLibrary(hModule);
                Console.WriteLine("已释放库");
            }

            break;
        }
        else
        {
            Console.WriteLine($"LoadLibrary 失败，错误代码: {Marshal.GetLastWin32Error()}");
        }
    }
}

// 方法3：使用 .NET NativeLibrary 类加载 DLL
static void TestMethod3_NativeLibrary()
{
    Console.WriteLine("方法3: 使用 .NET NativeLibrary 类加载 DLL");
    string[] libNames = ["sqlite3.dll", "sqlite3"];
    foreach (var libName in libNames)
    {
        try
        {
            Console.WriteLine($"尝试加载: {libName}");
            var handle = NativeLibrary.Load(libName);
            Console.WriteLine("NativeLibrary.Load 成功");
            try
            {
                if (NativeLibrary.TryGetExport(handle, "sqlite3_libversion", out var funcPtr))
                {
                    Console.WriteLine("找到 sqlite3_libversion 导出函数");
                    var func = Marshal.GetDelegateForFunctionPointer<Sqlite3LibVersionDelegate>(funcPtr);
                    TryGetSqliteVersion(() => func(), "NativeLibrary 调用");
                }
                else
                {
                    Console.WriteLine("未找到 sqlite3_libversion 导出函数");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"调用函数时出错: {ex.Message}");
            }
            finally
            {
                NativeLibrary.Free(handle);
                Console.WriteLine("已释放 NativeLibrary");
            }

            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NativeLibrary.Load 失败: {ex.Message}");
        }
    }
}

// 统一版本获取和异常处理
static void TryGetSqliteVersion(Func<nint> getVersionPtr, string method)
{
    try
    {
        var ptr = getVersionPtr();
        var result = Marshal.PtrToStringAnsi(ptr) ?? "未知版本";
        Console.WriteLine($"{method}成功，返回值: {result}");
    }
    catch (DllNotFoundException ex)
    {
        Console.WriteLine($"{method}加载失败: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"{method}其他错误: {ex.Message}");
    }
}

// 委托定义
internal delegate nint Sqlite3LibVersionDelegate();

// P/Invoke 声明
internal static class NativeMethods
{
    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool SetDllDirectory(string? lpPathName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr LoadLibrary(string lpLibFileName);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern bool FreeLibrary(IntPtr hLibModule);

    [DllImport("kernel32.dll", SetLastError = true)]
    public static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

    [DllImport("sqlite3.dll", EntryPoint = "sqlite3_libversion", CallingConvention = CallingConvention.Cdecl,
        CharSet = CharSet.Ansi)]
    public static extern nint Sqlite3LibVersion();
}