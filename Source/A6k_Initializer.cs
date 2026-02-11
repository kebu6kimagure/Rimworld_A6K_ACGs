using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace A6k_CompVariant
{
    [StaticConstructorOnStartup]
    public static class A6k_ModInitializer
    {
        static A6k_ModInitializer()
        {
            Log.Message("[A6k_CompVariant] 开始初始化变体路径和预设池。");
            A6k_PathManager.ReinitializeVariantPaths(); // 使用新方法初始化路径
                                                        // 延迟执行预设池初始化，确保路径列表已填充
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                A6k_VariantPresetPool.InitializePools();
                Log.Message("[A6k_CompVariant] 变体路径和预设池初始化完成，确保后续模块能读取到数据。");
            });
        }
    }
}
