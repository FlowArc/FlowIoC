using System;
using System.Collections.Generic;
using UnityEngine;

namespace FlowIoC.BaseModule.Injectable.Utils
{
    public static class InjectionCaching
    {
        private static readonly Dictionary<Type, InjectionCacheData> CacheDataList = new Dictionary<Type, InjectionCacheData>(); //ConcurentDictionary kullanılabilir mi? Multithreading?

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => CacheDataList.Clear();

        public static void AddCacheData(Type type, List<Type> children)
        {
            if (!CacheDataList.ContainsKey(type))
                CacheDataList[type] = new InjectionCacheData(type, children);
        }

        public static InjectionCacheData GetCacheData(Type type)
        {
            CacheDataList.TryGetValue(type, out var data);
            return data;
        }
    }

    public class InjectionCacheData
    {
        public Type Main { get; }
        public List<Type> Children { get; }

        public InjectionCacheData(Type main, List<Type> children)
        {
            Main = main;
            Children = children;
        }
    }
}