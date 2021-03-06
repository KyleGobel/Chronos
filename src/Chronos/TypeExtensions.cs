﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Chronos
{
    public static class TypeExtensions
    {
        public static Type GetNullableType(this Type type)
        {
            type = Nullable.GetUnderlyingType(type);
            if (type.IsValueType)
                return typeof (Nullable<>).MakeGenericType(type);
            else
                return type;
        }

        public static Type GetStaticClassFromCurrentDomain(this string fullname)
        {
          var assTypes = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly item in assemblies)
            {
                assTypes.AddRange(item.GetTypes().Where(x => x.IsClass && x.IsSealed && x.IsAbstract));
            }

            return assTypes.FirstOrDefault(x => x.FullName == fullname);
        }
        public static void RunOnDynamic<T>(this T target, Action<dynamic> action)
        {
            dynamic d = target;
            try
            {
                action(d);

            }
            catch (Exception)
            {
            }
        }
        /// <summary>
        /// This Methode extends the System.Type-type to get all extended methods. It searches hereby in all assemblies which are known by the current AppDomain.
        /// </summary>
        /// <remarks>
        /// Insired by Jon Skeet from his answer on http://stackoverflow.com/questions/299515/c-sharp-reflection-to-identify-extension-methods
        /// </remarks>
        /// <returns>returns MethodInfo[] with the extended Method</returns>

        public static MethodInfo[] GetExtensionMethods(this Type t)
        {
            List<Type> AssTypes = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (Assembly item in assemblies)
            {
                AssTypes.AddRange(item.GetTypes());
            }

            var query = from type in AssTypes
                where type.IsSealed && !type.IsGenericType && !type.IsNested
                from method in type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                where method.IsDefined(typeof(ExtensionAttribute), false)
                where method.GetParameters()[0].ParameterType == t
                select method;
            return query.ToArray<MethodInfo>();
        }

        /// <summary>
        /// Extends the System.Type-type to search for a given extended MethodeName.
        /// </summary>
        /// <param name="MethodeName">Name of the Methode</param>
        /// <returns>the found Methode or null</returns>
        public static MethodInfo GetExtensionMethod(this Type t, string MethodeName)
        {
            var mi = from methode in t.GetExtensionMethods()
                where methode.Name == MethodeName
                select methode;
            if (mi.Count<MethodInfo>() <= 0)
                return null;
            else
                return mi.First<MethodInfo>();
        }
    }
}