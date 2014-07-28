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
        System.IO.File.WriteAllText(""test.txt"", ""こころぴょんぴょん"");
        field *= 10;
        Console.WriteLine(field);
        Console.WriteLine(System.IO.File.ReadAllText(""test.txt""));
        //System.Diagnostics.Process.Start(""test.txt"");
    }
}

";
            using (var runner = new ScriptRunner(false, scriptBody))
            {
                runner.ReloadAssembly(new[]
                    {
                        new FileIOPermission(FileIOPermissionAccess.AllAccess, Path.GetDirectoryName(Path.GetFullPath(runner.PathToAssembly))), //指定したディレクトリへのフルアクセス
                        //new FileIOPermission(FileIOPermissionAccess.Write, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted")), //指定したディレクトリへの書き込み権限
                    });
                runner.InvokeClassFunction("Program", "Main", new object[0]);
            }
            Console.ReadLine();
        }
    }
}
