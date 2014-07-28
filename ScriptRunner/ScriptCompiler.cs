using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Linq.Expressions;

namespace ScriptRunnerLibrary
{
    /// <summary>
    /// コンパイル済みスクリプトを表すクラス<br/>
    /// 別 AppDomain で動作させることを前提に MarshalByRefObject を継承している。
    /// http://dora.bk.tsukuba.ac.jp/~takeuchi/index.php?%A5%D7%A5%ED%A5%B0%A5%E9%A5%DF%A5%F3%A5%B0%2F%A3%C3%A1%F4%2F%A3%C3%A1%F4%A4%C7%BD%F1%A4%AB%A4%EC%A4%BF%A5%B9%A5%AF%A5%EA%A5%D7%A5%C8%A4%F2%BC%C2%B9%D4%A4%B9%A4%EB
    /// </summary>
    public class ScriptCompiler : MarshalByRefObject
    {
        public CompilerResults CompilerResults { get; private set; }

        public Assembly CompiledAsesmbly
        {
            get
            {
                return CompilerResults != null
                    ? CompilerResults.CompiledAssembly
                    : null;
            }
        }

        public string PathToAssembly
        {
            get
            {
                return CompilerResults != null
                    ? CompilerResults.PathToAssembly
                    : null;
            }
        }

        /// <summary>
        /// デフォルトのアセンブリ参照を使ってスクリプトをコンパイルする
        /// <code>
        /// Compile(script,
        /// new string[]{
        ///     "System.dll",
        ///     "System.Data.dll",
        ///     "System.Deployment.dll",
        ///     "System.Drawing.dll",
        ///     "System.Windows.Forms.dll",
        ///     "System.Xml.dll",
        ///     "mscorlib.dll"
        ///     });
        /// </code>
        /// と同等の動作。
        /// </summary>
        /// <param name="script">スクリプトソース</param>
        /// <returns>成功したら true</returns>
        public bool Compile(string script, string outputAssemblyName = null)
        {
            return Compile(new[] { script },
                new string[]{
                    "System.dll",
                    "System.Core.dll",
                    "System.Data.dll",
                    "System.Deployment.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Xml.dll",
                    "mscorlib.dll"
                }, outputAssemblyName);
        }

        public bool Compile(string[] scripts, string outputAssemblyName = null)
        {
            return Compile(scripts,
                new string[]{
                    "System.dll",
                    "System.Core.dll",
                    "System.Data.dll",
                    "System.Deployment.dll",
                    "System.Drawing.dll",
                    "System.Windows.Forms.dll",
                    "System.Xml.dll",
                    "mscorlib.dll"
                }, outputAssemblyName);
        }

        /// <summary>
        /// アセンブリ参照名を指定してスクリプトをコンパイルする
        /// </summary>
        /// <param name="script">スクリプトソース</param>
        /// <param name="assemblyNames">
        ///   アセンブリ参照名
        ///   <code>
        ///     new string[]{"System.dll", "System.Windows.Forms.dll"}
        ///   </code>のようにして与える。
        /// </param>
        /// <returns>成功したら true</returns>
        public bool Compile(string script, string[] assemblyNames)
        {
            return Compile(new[] { script }, assemblyNames);
        }

        public bool Compile(string[] scripts, string[] assemblyNames, string outPutAssemblyName = null)
        {
            var param = new CompilerParameters
            {
                IncludeDebugInformation = true,
                GenerateExecutable = false,
                GenerateInMemory = false,
                OutputAssembly = outPutAssemblyName,
            };

            //コンパイル
            var codeProvider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });
            CompilerResults = codeProvider.CompileAssemblyFromSource(param, scripts);

            // エラーメッセージが無ければ成功
            return CompilerResults.Errors.Count == 0;
        }

        /// <summary>
        /// 直前のコンパイルで生じたエラーメッセージを返す。
        /// </summary>
        /// <returns>エラーメッセージ</returns>
        public String GetErrorMessages()
        {
            if (CompilerResults == null)
                return "";
            string result = "";
            for (int i = 0; i < CompilerResults.Errors.Count; i++)
                result +=
                    CompilerResults.Errors[i].Line.ToString() + ":" +
                    CompilerResults.Errors[i].ErrorText + "\r\n";
            return result;
        }
    }
}