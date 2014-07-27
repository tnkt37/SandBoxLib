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
    public class ScriptManager : MarshalByRefObject
    {
        Assembly assembly;

        public void LoadAssembly(string assemblyPath)
        {
            assembly = Assembly.LoadFrom(assemblyPath);
        }

        // 内部で使う
        private Type getClassReference(string ClassName)
        {
            return assembly.GetType(ClassName);
        }

        /// <summary>
        /// クラス関数を呼び出す。
        /// </summary>
        /// <param name="ClassName">クラス名</param>
        /// <param name="FunctionName">クラス関数名</param>
        /// <param name="Parameters">パラメータ</param>
        /// <returns></returns>
        public object InvokeClassFunction(string ClassName, string FunctionName,
            object[] Parameters)
        {
            // 渡された引数の型をチェック
            Type[] argumentTypes = new Type[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
                argumentTypes[i] = Parameters[i].GetType();

            // クラスリファレンスを取得
            Type type = getClassReference(ClassName);
            if (type == null)
                throw new Exception(ClassName + "という名前のクラスは存在しません");

            // クラス関数を取得
            MethodInfo mi = type.GetMethod(FunctionName, argumentTypes);
            if (mi == null)
                throw new Exception("NoSuchClassFunctionError");

            // 呼び出し
            return mi.Invoke(null, Parameters);
        }

        /// <summary>
        /// クラスのインスタンスを作成する。
        /// </summary>
        /// <param name="ClassName">クラス名</param>
        /// <param name="Parameters">コンストラクタに渡すパラメータ</param>
        /// <returns>クラスのインスタンス</returns>
        public object CreateInstance(string ClassName, object[] Parameters)
        {
            // 渡された引数の型をチェック
            Type[] argumentTypes = new Type[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
                argumentTypes[i] = Parameters[i].GetType();

            // クラスリファレンスを取得
            Type type = getClassReference(ClassName);
            if (type == null)
                throw new Exception(ClassName + "という名前のクラスは存在しません");

            // コンストラクタの取得
            ConstructorInfo constructorInfo = type.GetConstructor(argumentTypes);
            if (constructorInfo == null)
                throw new Exception("NoSuchClassFunctionError");

            // 呼び出し
            return constructorInfo.Invoke(Parameters);
        }

        /// <summary>
        /// オブジェクトのメンバ関数を呼びだす。
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FunctionName">関数名</param>
        /// <param name="Parameters">パラメータ</param>
        /// <returns></returns>
        public object InvokeFunction(object Object, string FunctionName,
            object[] Parameters)
        {
            // 渡された引数の型をチェック
            Type[] argumentTypes = new Type[Parameters.Length];
            for (int i = 0; i < Parameters.Length; i++)
                argumentTypes[i] = Parameters[i].GetType();

            // 型情報を取得
            Type type = Object.GetType();

            // メンバ関数の取得
            MethodInfo methodInfo = type.GetMethod(FunctionName, argumentTypes);
            if (methodInfo == null)
                throw new Exception("NoSuchClassFunctionError");

            // 呼び出し
            return methodInfo.Invoke(Object, Parameters);
        }

        /// <summary>
        /// オブジェクトのフィールドに値を代入する
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">フィールド名</param>
        /// <param name="Value">値</param>
        public void SetField(object Object, string FieldName, object Value)
        {
            Type type = Object.GetType();
            FieldInfo fieldInfo = type.GetField(FieldName);
            fieldInfo.SetValue(Object, Value);
        }

        /// <summary>
        /// オブジェクトのフィールドから値を読み出す
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">フィールド名</param>
        /// <returns>値</returns>
        public object GetField(object Object, string FieldName)
        {
            Type type = Object.GetType();
            FieldInfo fieldInfo = type.GetField(FieldName);
            return fieldInfo.GetValue(Object);
        }

        public object GetStaticFiled(string className, string filedName)
        {
            var type = getClassReference(className);
            type.GetField(filedName);
            if (type == null)
                throw new Exception(className + "という名前のクラスは存在しません");

            var info = type.GetField(filedName);
            if (info == null)
                throw new Exception(filedName + "という名前の静的変数は存在しまえせん");
            return info.GetValue(null);
        }

        /// <summary>
        /// オブジェクトのプロパティに値を代入する
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">プロパティ名</param>
        /// <param name="Value">値</param>
        public void SetProperty(object Object, string PropertyName, object Value)
        {
            Type type = Object.GetType();
            PropertyInfo propertyInfo = type.GetProperty(PropertyName);
            propertyInfo.SetValue(Object, Value, null);
        }

        /// <summary>
        /// オブジェクトのプロパティから値を読み出す
        /// </summary>
        /// <param name="Object">対象となるオブジェクト</param>
        /// <param name="FieldName">プロパティ名</param>
        /// <returns>値</returns>
        public object GetProperty(object Object, string PropertyName)
        {
            Type type = Object.GetType();
            PropertyInfo propertyInfo = type.GetProperty(PropertyName);
            return propertyInfo.GetValue(Object, null);
        }
    }
}