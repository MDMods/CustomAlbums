﻿using Il2CppGameLogic;
using Il2CppInterop.Runtime;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomAlbums.Utilities
{
    public static class Il2CppExtensions
    {
        public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this List<T> list)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in list)
            {
                il2Cpp.Add(item);
            }

            return il2Cpp;
        }

        public static Il2CppSystem.Collections.Generic.List<T> ToIl2Cpp<T>(this ReadOnlyCollection<T> collection)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.List<T>();
            foreach (var item in collection)
            {
                il2Cpp.Add(item);
            }

            return il2Cpp;
        }

        public static Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue> ToIl2Cpp<TKey, TValue>(this Dictionary<TKey, TValue> dictionary)
        {
            var il2Cpp = new Il2CppSystem.Collections.Generic.Dictionary<TKey, TValue>();
            foreach (var (key, value) in dictionary)
            {
                il2Cpp.Add(key, value);
            }

            return il2Cpp;
        }

        public static List<T> ToManaged<T>(this Il2CppSystem.Collections.Generic.List<T> list)
        {
            var managed = new List<T>();
            foreach (var item in list)
            {
                managed.Add(item);
            }

            return managed;
        }

        public static Dictionary<TKey, T> ToManaged<TKey, T>(
            this Il2CppSystem.Collections.Generic.Dictionary<TKey, T> dictionary)
        {
            var managed = new Dictionary<TKey, T>();
            foreach (var entry in dictionary)
            {
                managed.Add(entry.Key, entry.Value);
            }

            return managed;
        }
    }
}
