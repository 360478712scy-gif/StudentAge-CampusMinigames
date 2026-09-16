using System;
using System.Reflection;
using HarmonyLib;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// 合并为单个 DLL 后，两个 BepInEx 插件各自只打自己命名空间下的 Harmony 补丁，
    /// 避免同一批补丁被两个 Harmony 实例重复应用。
    /// </summary>
    internal static class HarmonyScope
    {
        internal static void PatchNamespace(Harmony harmony, Assembly assembly, string ns)
        {
            foreach (Type type in SafeTypes(assembly))
            {
                if (type == null || !string.Equals(type.Namespace, ns, StringComparison.Ordinal)) continue;
                if (type.GetCustomAttributes(typeof(HarmonyPatch), true).Length == 0) continue;
                harmony.CreateClassProcessor(type).Patch();
            }
        }

        /// <summary>UP 不在时，实现 UP 接口的类型无法加载；跳过它们而不是让整个插件初始化失败。</summary>
        internal static Type[] SafeTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types ?? new Type[0]; }
        }
    }
}
