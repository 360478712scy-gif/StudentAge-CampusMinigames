using System;
using System.IO;
using BepInEx.Logging;
using Sdk.PlatformAPI;
using UnityEngine;

namespace StudentAge.CampusUno
{
    /// <summary>
    /// 原版 PathDefine 是静态类：第一次被访问时用 Platform.Current.GetUserId() 算出存档目录并固定下来。
    /// 如果这一次访问发生在 SteamManager 初始化之前（任何插件在 Awake 阶段碰到 PathDefine 就会），
    /// GetUserId 返回 "user"，整局游戏都会读写 Saves/user/：玩家看到的就是"存档全没了、启用的 Mod 列表也没了"。
    ///
    /// 这里在 Steam 就绪后核对一次，发现被固定成 user 目录就改回真正的 SteamID 目录。
    /// 非 Steam 平台（GetUserId 本来就是 user）不做任何事。
    /// </summary>
    internal static class SavePathGuard
    {
        static bool done;

        internal static void Tick(ManualLogSource log)
        {
            if (done) return;
            try
            {
                if (!SteamManager.Initialized) return;
                string userId = Platform.Current.GetUserId();
                if (string.IsNullOrEmpty(userId) || userId == "user") return;
                done = true;

                string root = Application.persistentDataPath;
                string expected = Path.Combine(root, "Saves", userId);
                string current = PathDefine.SAVE_PATH ?? string.Empty;
                if (string.Equals(Normalize(current), Normalize(expected), StringComparison.OrdinalIgnoreCase)) return;

                log.LogWarning("存档目录在 Steam 初始化前被固定为 " + current + "，已改回 " + expected + "。若本次已在错误目录存过档，旧文件在 " + current);
                PathDefine.SAVE_PATH = expected;
                PathDefine.TEST_SAVE_PATH = Path.Combine(root, "Saves_Test", userId);
                PathDefine.IMG_PATH = Path.Combine(expected, "Images");
                PathDefine.MUSIC_PATH = Path.Combine(expected, "Musics");
                Directory.CreateDirectory(expected);
            }
            catch (Exception e)
            {
                done = true;
                log.LogWarning("检查存档目录失败：" + e.Message);
            }
        }

        static string Normalize(string path)
        {
            try { return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar); }
            catch { return path; }
        }
    }
}
