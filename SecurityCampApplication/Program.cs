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
using System.Collections.Generic;

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
        new Program().Calc();
    }
    void Calc()
    {
        var fibs = new List<decimal>();
        fibs.Add(0); fibs.Add(1);
        for(int i = 0; i < 100; i++)
        {
            int last = fibs.Count - 1;
            fibs.Add(fibs[last] + fibs[last - 1]);
            Console.WriteLine(fibs[fibs.Count - 2]);
        }
    }
}

";
            using (var runner = new ScriptRunner(false, scriptBody))
            {
                runner.ReloadAssembly(new[]
                    {
                        new FileIOPermission(FileIOPermissionAccess.AllAccess, Path.GetDirectoryName(Path.GetFullPath(runner.PathToAssembly))), //指定したディレクトリへのフルアクセス
                        //これってそのディレクトリ以下もアクセス出来るの？
                        //new FileIOPermission(FileIOPermissionAccess.Write, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Untrusted")), //指定したディレクトリへの書き込み権限
                    });
                runner.InvokeEntoryPoint("Program");

                //プラグインとして読み込むときに別のファイルの読み込みが必要な時は「xxディレクトリ以下のファイルを読み込みます」的な警告出して、そのディレクトリへの絶対パスを取得し、スクリプト内にデータを埋め込んで文字列を置換する
                //例 プラグインを読み込むときにそのディレクトリの情報を取得 string pluguinPath
                //      new Bitmap("<$path$>\\image.png");
                //   script.Replace(<$path$>, pluguinPath);
                //   new FileIOPermission(FileIOPermissionAccess.Read, pluguinPath);
            }
            Console.ReadLine();
        }
    }
}
