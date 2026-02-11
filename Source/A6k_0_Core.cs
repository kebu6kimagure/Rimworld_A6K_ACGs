using HarmonyLib;
using RimWorld;
using System;
using UnityEngine;
using Verse;

namespace A6k_CompVariant
{
    [StaticConstructorOnStartup]
    public class A6kCompVariantMod : Mod
    {
        public static A6kCompVariantSettings Settings { get; private set; }

        public A6kCompVariantMod(ModContentPack content) : base(content)
        {
            try
            {
                Log.Message("[A6k_CompVariant] Mod 初始化成功！");

                Settings = GetSettings<A6kCompVariantSettings>();
                if (Settings != null)
                {
                    Settings.SettingsChanged += OnSettingsChanged;
                    OnSettingsChanged();
                }

                // 移除PatchAll，改为统一在A6k_HarmonyPatches_Init中管理补丁
                Log.Message("[A6k_CompVariant] Harmony补丁初始化已移至A6k_HarmonyPatches_Init。");
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] Mod 初始化失败: {ex.Message}\n{ex.StackTrace}");
            }
        }

        public override void DoSettingsWindowContents(Rect inRect) => Settings?.DoSettingsWindowContents(inRect);
        public override string SettingsCategory() => "A6k 变体设置";

        private static void OnSettingsChanged()
        {
            try
            {
                Log.Message("[A6k_CompVariant] 设置变更，处理更新。");
                A6k_RecipeHandler.RemoveRecipesForSelected(); // 直接调用，内部已处理延迟
                A6k_CacheHandler.ClearStyleDiversityCache();
                A6k_VariantPresetPool.InitializePools();
            }
            catch (Exception ex)
            {
                Log.Error($"[A6k_CompVariant] 设置变更处理失败: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
