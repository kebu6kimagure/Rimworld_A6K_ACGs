using System.Collections.Generic;
using System.Linq;
using Verse;

namespace A6k_CompVariant
{
    public static class A6k_VariantPresetPool
    {
        private static List<string> hatPresetPool = new List<string>();
        private static List<string> outerPresetPool = new List<string>();
        private const int PoolSize = 20;

        public static void InitializePools()
        {
            hatPresetPool.Clear();
            outerPresetPool.Clear();
            FillPool(hatPresetPool, Comp_VariantApparel.HatVariantPaths.Select(v => v.Path).ToList(), "Hat");
            FillPool(outerPresetPool, Comp_VariantApparel.OuterVariantPaths.Select(v => v.Path).ToList(), "Outerwear");
            Log.Message($"[A6k_CompVariant] Preset pools initialized. Hat pool: {hatPresetPool.Count}, Outer pool: {outerPresetPool.Count}");
        }

        private static void FillPool(List<string> pool, List<string> sourcePaths, string type)
        {
            if (sourcePaths == null || !sourcePaths.Any())
            {
                Log.Warning($"[A6k_CompVariant] No variant paths available for {type} pool initialization.");
                return;
            }
            for (int i = 0; i < PoolSize; i++)
            {
                pool.Add(sourcePaths.RandomElement());
            }
        }

        public static string GetPresetPath(string variantType)
        {
            List<string> pool = variantType == "Hat" ? hatPresetPool : outerPresetPool;
            List<string> sourcePaths = variantType == "Hat" ? Comp_VariantApparel.HatVariantPaths.Select(v => v.Path).ToList() : Comp_VariantApparel.OuterVariantPaths.Select(v => v.Path).ToList();
            if (pool == null || pool.Count == 0)
            {
                Log.Warning($"[A6k_CompVariant] Preset pool empty for {variantType}, fallback to direct random.");
                if (sourcePaths == null || !sourcePaths.Any())
                {
                    Log.Error($"[A6k_CompVariant] No source paths available for {variantType}. Returning empty path.");
                    return string.Empty; // 返回空路径，避免后续逻辑出错
                }
                return sourcePaths.RandomElement();
            }
            string path = pool[0];
            pool.RemoveAt(0);
            RefillPool(pool, sourcePaths, variantType);
            return path;
        }

        private static void RefillPool(List<string> pool, List<string> sourcePaths, string type)
        {
            if (sourcePaths == null || !sourcePaths.Any())
            {
                Log.Warning($"[A6k_CompVariant] No source paths to refill {type} pool.");
                return;
            }
            pool.Add(sourcePaths.RandomElement());
        }

        public static void InsertCustomPath(string path, string variantType)
        {
            List<string> pool = variantType == "Hat" ? hatPresetPool : outerPresetPool;
            if (pool != null)
            {
                pool.Insert(0, path);
                if (pool.Count > PoolSize)
                {
                    pool.RemoveAt(pool.Count - 1);
                }
                Log.Message($"[A6k_CompVariant] Inserted custom path {path} to {variantType} pool top.");
            }
        }
    }
}
