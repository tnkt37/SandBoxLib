using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;
using System.IO;

using ScriptRunnerLibrary;
using System.Security;
using System.Security.Policy;
using System.Security.Permissions;

namespace SecurityCampApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            string scriptBody =
    @"
using System;
using System.IO;

public class Program
{
    public static int field = 10;
    public static void Main()
    {
        Console.WriteLine(""あ＾～" + 100 + @""");
        System.IO.File.WriteAllText(""Untrusted\\test.txt"", ""こころぴょんぴょん"");
        field *= 10;
        Console.WriteLine(field);
        Console.WriteLine(System.IO.File.ReadAllText(""Untrusted\\test.txt""));
        //System.Diagnostics.Process.Start(""test.txt"");
    }
}

";
            var scripts = new[] { scriptBody };
            var assemplyNames = new[]
                {
                    "System.dll",
                    "System.Core.dll",
                    "mscorlib.dll"
                };
            var param = new CompilerParameters
            {
                IncludeDebugInformation = true,
                GenerateExecutable = false,
                GenerateInMemory = false,
                OutputAssembly = "asm\\test.dll",
            };
            Directory.CreateDirectory("asm");
            var codeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            var results = codeProvider.CompileAssemblyFromSource(param, scripts);
            //providerLits.Add(results);
            //results.CompiledAssembly.GetType("Program").GetMethod("Main").Invoke(new object(), new object[0]);
            var path = Path.GetFullPath(results.PathToAssembly);

            AppDomain appDomain = SandBox("testdomin", path);

            ScriptManager manager;
            manager = appDomain.CreateInstanceAndUnwrap("ScriptRunner", "ScriptRunnerLibrary.ScriptManager") as ScriptRunnerLibrary.ScriptManager;
            //コンストラクタに引数渡したいです

            manager.LoadAssembly(path);
            //TODO Assembly.LoadはGCに回収されないのか調べる(されなさそう);

            manager.InvokeClassFunction("Program", "Main", new object[0]);


            AppDomain.Unload(appDomain);
            Console.WriteLine(results.PathToAssembly);
            Console.ReadLine();
        }


        /// <summary>
        /// サンドボックス化
        /// </summary>
        /// <param name="domainName"></param>
        static AppDomain SandBox(string domainName, string assemblyPath)
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);//ﾐﾄﾒﾗﾚﾅｲﾜｧ
            //PermissionSet permSet = new PermissionSet(PermissionState.Unrestricted);//フルアクセス

            permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, assemblyPath));//やったぜ！！！！！(Readだけじゃダメなのはなぜ？まぁアセンブリの読み込みしかやってないしいいけどさ...)
            permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted")));//指定したディレクトリへのフルアクセス
            //permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.Write, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted")));//指定したディレクトリへの書き込み権限

            //TODO その他のpermission調べる
            //http://msdn.microsoft.com/ja-jp/library/system.security.permissions(v=vs.110).aspx
            //インターネットアクセス制御とか無いの？

            //permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, System.Security.AccessControl.AccessControlActions.None, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted")));
            //System.Security.AccessControl.AccessControlActionsが謎


            AppDomainSetup adSetup = AppDomain.CurrentDomain.SetupInformation;

            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted"));
            //adSetup.ApplicationBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted");
            //adSetup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;//問題あり？こいつのせいで実行ファイルのディレクトリにアクセス可能になってる。無くて良さそう。
            //TODO ApplicationBaseの意味を調べる
            Evidence ev = new Evidence();


            return AppDomain.CreateDomain(domainName, ev, adSetup, permSet);
            //appDomain = AppDomain.CreateDomain(domainName);
        }
    }
}
