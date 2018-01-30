using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BrainCommon
{
    /// <summary>
    /// 反射帮助类。
    /// </summary>
    public static class ReflectionHelper
    {
        private static readonly Type[] EmptyGenericTypeList = new Type[0];
        private static readonly object[] EmptyConstructorArgumentList = new object[0];

        /// <summary>
        /// 获取实现了指定接口类型的基类实例。
        /// </summary>
        /// <typeparam name="T">接口类型</typeparam>
        /// <param name="assembly">指定的程序集</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">assembly is null</exception>
        public static T[] GetImplementObjects<T>(Assembly assembly)
            where T : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            return assembly.GetTypes()
                .Where(c => c.IsClass && !c.IsAbstract && c.GetInterfaces().Contains(typeof(T)))
                .Select(CallCtr<T>)
                .ToArray();
        }

        public static object CreateInstance(string typeName)
        {
            var c = SearchType(typeName);
            return c.GetConstructor(EmptyGenericTypeList)?.Invoke(EmptyConstructorArgumentList);
        }

        public static T CallCtr<T>(Type c)
            where T : class
        {
            return c.GetConstructor(EmptyGenericTypeList)?.Invoke(EmptyConstructorArgumentList) as T;
        }

        public static T CallCtr<T>(Type c, params object[] args)
            where T : class
        {
            var ctrParamTypes = new Type[args.Length];
            for (var i = 0; i < args.Length; i++)
            {
                ctrParamTypes[i] = args[i].GetType();
            }
            return c.GetConstructor(ctrParamTypes)?.Invoke(args) as T;
        }

        public static T CallCtr<T>(Type c, Type[] ctrParamTypes, object[] args)
            where T : class
        {
            return c.GetConstructor(ctrParamTypes)?.Invoke(args) as T;
        }

        public static IEnumerable<Type> GetImplementedTypes<T>(Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));
            return assembly.GetTypes()
                .Where(c => c.IsClass && !c.IsAbstract && c.GetInterfaces().Contains(typeof(T)));
        }

        public static IEnumerable<Type> GetImplementationTypes<T>(Assembly assembly)
            where T : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var type = typeof(T);
            return assembly.GetTypes()
                .Where(c => (c.IsClass && !c.IsAbstract && c.IsSubclassOf(type)));
        }

        public static IEnumerable<T> GetImplementations<T>(Assembly assembly)
            where T : class
        {
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var type = typeof(T);
            return assembly.GetTypes()
                .Where(c => (c.IsClass && !c.IsAbstract && c.IsSubclassOf(type)))
                .Select(CallCtr<T>);
        }

        public static List<T> GetAllImplementationsInAssembly<T>() where T : class
        {
            var assemblyLst = AppDomain.CurrentDomain.GetAssemblies();
            var enumList = new List<T>();
            for (var i = 0; i < assemblyLst.Length; i++)
            {
                var lst = GetImplementations<T>(assemblyLst[i]);
                enumList.AddRange(lst);
            }
            return enumList;
        }

        public static List<T> GetAllInterfaceImpl<T>() where T : class
        {
            var assemblyLst = AppDomain.CurrentDomain.GetAssemblies();
            var enumList = new List<T>();
            for (var i = 0; i < assemblyLst.Length; i++)
            {
                var lst = GetImplementedTypes<T>(assemblyLst[i]).Select(CallCtr<T>);
                enumList.AddRange(lst);
            }
            return enumList;
        }

        public static Type GetType(string fullName)
        {
            try
            {
                return Type.GetType(fullName);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            return null;
        }

        private static Assembly _lastSearchHit;
        /// <summary>
        /// use Type.GetType when possible
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public static Type SearchType(string typeName)
        {
            var result = _lastSearchHit?.GetType(typeName);
            if (result != null) return result;

            var assemblyLst = AppDomain.CurrentDomain.GetAssemblies();
            for (var i = 0; i < assemblyLst.Length; i++)
            {
                try
                {
                    result = assemblyLst[i].GetType(typeName);
                    if (result != null)
                    {
                        _lastSearchHit = assemblyLst[i];
                        return result;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            return null;
        }
    }}