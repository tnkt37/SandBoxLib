using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Reflection;

using System.Security.Policy;
using System.Security;
using System.Security.Permissions;
using System.IO;
using System.Diagnostics;

namespace ScriptRunnerLibrary
{
    /// <summary>
    /// ＤＬＬに格納された ScriptRunnerLibrary.Script クラスを
    /// 使って文字列として渡されたＣ＃コードをコンパイルし、
    /// 実行するためのクラス
    /// http://dora.bk.tsukuba.ac.jp/~takeuchi/index.php?%A5%D7%A5%ED%A5%B0%A5%E9%A5%DF%A5%F3%A5%B0%2F%A3%C3%A1%F4%2F%A3%C3%A1%F4%A4%C7%BD%F1%A4%AB%A4%EC%A4%BF%A5%B9%A5%AF%A5%EA%A5%D7%A5%C8%A4%F2%BC%C2%B9%D4%A4%B9%A4%EB
    /// </summary>
    public class ScriptRunner : IDisposable
    {
        static Random rand = new Random();
        AppDomain compilerDomain = null;
        AppDomain managerDomain = null;
        ScriptRunnerLibrary.ScriptCompiler compiler = null;
        ScriptRunnerLibrary.AssemblyManager manager = null;

        static List<string> pathesToAssembly = new List<string>();
        public static IList<string> PathesToAssembly { get { return pathesToAssembly; } }

        public string PathToAssembly { get { return compiler != null ? compiler.PathToAssembly : null; } }
        public string PathToAssemblyWorkDirectory { get; private set; }

        /// <summary>
        /// 与えられたアセンブリを参照しつつスクリプトをコンパイルする
        /// </summary>
        /// <param name="scriptSource">Ｃ＃スクリプト</param>
        /// <param name="assemblyNames">参照するアセンブリの名前リスト</param>
        public ScriptRunner(string[] assemblyNames, bool loadAssembly, params string[] scriptSource)
        {
            LoadCompiler();
            var isSucceed = compiler.Compile(scriptSource, assemblyNames, compilerDomain.FriendlyName);
            if (!isSucceed) throw new Exception(compiler.GetErrorMessages());
            if (loadAssembly)
                LoadManager(compiler);
        }

        /// <summary>
        /// 標準のアセンブリを参照しつつスクリプトをコンパイルする
        /// </summary>
        /// <param name="scriptSource">Ｃ＃スクリプト</param>
        public ScriptRunner(bool loadAssembly, params string[] scriptSources)
        {
            LoadCompiler();
            var isSucceed = compiler.Compile(scriptSources, compilerDomain.FriendlyName);
            if (!isSucceed) throw new Exception(compiler.GetErrorMessages());
            if (loadAssembly)
                LoadManager(compiler, null);
        }

        public void ReloadAssembly(IEnumerable<IPermission> permissions)
        {
            LoadManager(compiler, permissions);
        }

        private string GenerateDomainName()
        {
            return
                Convert.ToString(rand.Next(), 16) +
                Convert.ToString(rand.Next(), 16) +
                Convert.ToString(rand.Next(), 16) +
                Convert.ToString(rand.Next(), 16);
        }

        /// <summary>
        /// ScriptCompilerをロード
        /// </summary>
        private void LoadCompiler()
        {
            // ドメインには乱数を使って一意の名前を付ける
            string domainName = "CompilerDomain_" + GenerateDomainName();
            compilerDomain = AppDomain.CreateDomain(domainName);

            compiler = compilerDomain.CreateInstanceAndUnwrap("ScriptRunner", "ScriptRunnerLibrary.ScriptCompiler") as ScriptRunnerLibrary.ScriptCompiler;

            const string parentWorkDirectory = "CompiledAssembliis";

            //作業用ディレクトリを設定
            if (!Directory.Exists(parentWorkDirectory))
                Directory.CreateDirectory(parentWorkDirectory);

            string workDirectory = Path.Combine(parentWorkDirectory, compilerDomain.FriendlyName);
            Directory.CreateDirectory(workDirectory);
            PathToAssemblyWorkDirectory = workDirectory;
        }

        /// <summary>
        /// AssemblyManagerをロード
        /// </summary>
        /// <param name="compiler">読み込むアセンブリを生成したコンパイラ</param>
        private void LoadManager(ScriptCompiler compiler, IEnumerable<IPermission> permissions = null)
        {
            string domainName = "ManagerDomain_" + GenerateDomainName();

            managerDomain = CreateSandBoxedDomain(domainName, compiler.PathToAssembly, permissions);

            manager = managerDomain.CreateInstanceAndUnwrap("ScriptRunner", "ScriptRunnerLibrary.AssemblyManager", true, 0, null,
                new object[] { compiler.PathToAssembly }, null, new object[0]) as ScriptRunnerLibrary.AssemblyManager;

            pathesToAssembly.Add(compiler.PathToAssembly);
        }


        /// <summary>
        /// AppDomainのサンドボックス化を行う
        /// </summary>
        /// <param name="domainName">AppDomainの名前</param>
        /// <param name="assemblyPath">読み込むアセンブリのパス</param>
        /// <param name="permissions">許可リスト。nullを指定するとフルアクセス</param>
        /// <returns></returns>
        private AppDomain CreateSandBoxedDomain(string domainName, string assemblyPath, IEnumerable<IPermission> permissions)
        {
            PermissionSet permSet = new PermissionSet(PermissionState.None);//ﾐﾄﾒﾗﾚﾅｲﾜｧ

            if (permissions == null)
            {
                permSet = new PermissionSet(PermissionState.Unrestricted);//フルアクセス
            }
            else
            {
                permSet.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, Path.GetFullPath(assemblyPath)));//アセンブリの読み込み許可
                permSet.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
                foreach (var perm in permissions)
                {
                    permSet.AddPermission(perm);
                }
            }
            //TODO その他のpermission調べる
            //http://msdn.microsoft.com/ja-jp/library/system.security.permissions(v=vs.110).aspx
            //インターネットアクセス制御とか無いの？
            //System.Security.AccessControl.AccessControlActionsが謎

            AppDomainSetup adSetup = AppDomain.CurrentDomain.SetupInformation;

            return AppDomain.CreateDomain(domainName, null, adSetup, permSet);
        }

        /// <summary>
        /// コンパイル時に生じたエラーメッセージを返す。
        /// </summary>
        /// <returns>エラーメッセージ</returns>
        public string ErrorMessage()
        {
            return compiler.GetErrorMessages();
        }


        /// <summary>
        /// コンパイルが成功したかどうかを確認する。
        /// </summary>
        /// <returns>コンパイルに成功していれば true</returns>
        public bool IsReady()
        {
            return compiler.GetErrorMessages().Length == 0;
        }

        string originalCurrentDirectory = null;
        void ChangeCurrentDirectory()
        {
            originalCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(PathToAssemblyWorkDirectory);
        }

        void ResetCurrentDirectory()
        {
            if (originalCurrentDirectory != null)
                Directory.SetCurrentDirectory(originalCurrentDirectory);
        }

        /// <summary>
        /// クラス関数を呼び出す
        /// </summary>
        /// <param name="ClassName">クラス名</param>
        /// <param name="FunctionName">クラス関数名</param>
        /// <param name="Parameters">クラス関数への引数</param>
        /// <returns></returns>
        public object InvokeClassFunction(string ClassName, string FunctionName,
            object[] Parameters)
        {
            ChangeCurrentDirectory();
            var result = manager.InvokeClassFunction(ClassName, FunctionName, Parameters);
            ResetCurrentDirectory();
            return result;
        }

        public void InvokeEntoryPoint(string className, object[] parameters = null)
        {
            InvokeClassFunction(className, "Main", parameters ?? new object[0]);
        }

        /// <summary>
        /// クラス名を指定してオブジェクトを作成する
        /// </summary>
        /// <param name="ClassName">クラス名</param>
        /// <param name="Parameters">コンストラクタへの引数</param>
        /// <returns></returns>
        public object CreateInstance(string ClassName, object[] Parameters)
        {
            ChangeCurrentDirectory();
            var result = manager.CreateInstance(ClassName, Parameters);
            ResetCurrentDirectory();
            return result;
        }

        /// <summary>
        /// オブジェクトのメンバ関数を呼び出す
        /// </summary>
        /// <param name="Object"><see cref="CreateInstance"/>
        /// で作ったオブジェクト</param>
        /// <param name="FunctionName">メンバ関数名</param>
        /// <param name="Parameters">関数への引数</param>
        /// <returns></returns>
        public object InvokeFunction(object Object, string FunctionName,
            object[] Parameters)
        {
            ChangeCurrentDirectory();
            var result = manager.InvokeFunction(Object, FunctionName, Parameters);
            ResetCurrentDirectory();
            return result;
        }

        /// <summary>
        /// オブジェクトのフィールドに値を代入する
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">フィールド名</param>
        /// <param name="Value">値</param>
        public void SetField(object Object, string FieldName, object Value)
        {
            manager.SetField(Object, FieldName, Value);
        }

        /// <summary>
        /// オブジェクトのフィールドから値を読み出す
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">フィールド名</param>
        /// <returns>値</returns>
        public object GetField(object Object, string FieldName)
        {
            return manager.GetField(Object, FieldName);
        }

        public object GetClassFiled(string className, string filedName)
        {
            return manager.GetClassFiled(className, filedName);
        }

        /// <summary>
        /// オブジェクトのプロパティに値を代入する
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">プロパティ名</param>
        /// <param name="Value">値</param>
        public void SetProperty(object Object, string PropertyName, object Value)
        {
            manager.SetProperty(Object, PropertyName, Value);
        }

        /// <summary>
        /// オブジェクトのプロパティから値を読み出す
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">プロパティ名</param>
        /// <returns>値</returns>
        public object GetProperty(object Object, string PropertyName)
        {
            return manager.GetProperty(Object, PropertyName);
        }

        private void UnloadLibrary()
        {
            // アプリケーション終了時に暗黙的に解放される
            // ことがあるため、エラー回避用に catch している
            if (compilerDomain != null)
            {
                try
                {
                    AppDomain.Unload(managerDomain);
                    managerDomain = null;

                    AppDomain.Unload(compilerDomain);
                    compilerDomain = null;
                }
                catch (CannotUnloadAppDomainException)
                {
                    ; // すでにアンロードされていた
                }
            }
        }

        public void Dispose()
        {
            UnloadLibrary();
        }
    }
}