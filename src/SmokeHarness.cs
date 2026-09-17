using System;
using System.Collections;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Config;
using Sdk;

namespace StudentAge.CampusUno
{
    // Opt-in diagnostics for an isolated copy; social fixtures mutate memory only and redirect save paths.
    internal sealed class SmokeHarness : MonoBehaviour
    {
        IEnumerator Start()
        {
            string dir = Path.Combine(BepInEx.Paths.GameRootPath, "CampusUnoQA"); Directory.CreateDirectory(dir);
            yield return new WaitForSecondsRealtime(18);
            var plugin = Plugin.Instance;
            plugin.Table=plugin.gameObject.AddComponent<UnoTable>();plugin.Table.Open("同桌",null);
            float readyDeadline=Time.realtimeSinceStartup+30;
            while(plugin.Table!=null && !plugin.Table.Prepared && Time.realtimeSinceStartup<readyDeadline)yield return null;
            if(plugin.Table==null || !plugin.Table.Prepared){File.WriteAllText(Path.Combine(dir,"failed.txt"),"Native skin did not finish loading");yield break;}
            yield return new WaitForSecondsRealtime(2);
            if (plugin.Table == null) { File.WriteAllText(Path.Combine(dir, "failed.txt"), "Table did not open"); yield break; }
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "01-ready.png"));
            yield return new WaitForSecondsRealtime(1);
            var guideButton=Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t=>t.text=="玩法说明"));guideButton.onClick.Invoke();
            yield return new WaitForSecondsRealtime(.5f);
            for(int page=1;page<=3;page++){
                ScreenCapture.CaptureScreenshot(Path.Combine(dir,"01-guide-"+page+".png"));yield return new WaitForSecondsRealtime(.2f);
                if(plugin.Table.Engine!=null)throw new Exception("Guide unexpectedly started the game");
                if(page<3)Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.name=="btn_next").onClick.Invoke();
            }
            Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.name=="btn_prev").onClick.Invoke();
            if((int)AccessTools.Field(typeof(UnoTable),"guidePage").GetValue(plugin.Table)!=1)throw new Exception("Guide previous page failed");
            Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.name=="btn_close").onClick.Invoke();
            Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t=>t.text=="开始游戏")).onClick.Invoke();
            var table = plugin.Table; var engine = table.Engine;
            for(int frame=0;frame<36;frame++){ ScreenCapture.CaptureScreenshot(Path.Combine(dir,"deal-"+frame.ToString("000")+".png")); yield return new WaitForSecondsRealtime(.05f); }
            engine.Fixture(new[] { new Card(Suit.Red, Face.Three), new Card(Suit.Blue, Face.DrawTwo), new Card(Suit.Green, Face.Reverse), new Card(Suit.Wild, Face.Wild), new Card(Suit.Yellow, Face.Seven), new Card(Suit.Red, Face.Skip), new Card(Suit.Wild, Face.DrawFour) },
                new[] { new Card(Suit.Blue, Face.One), new Card(Suit.Blue, Face.Two), new Card(Suit.Green, Face.One), new Card(Suit.Yellow, Face.One), new Card(Suit.Green, Face.Three), new Card(Suit.Yellow, Face.Five), new Card(Suit.Red, Face.Seven) }, new Card(Suit.Red, Face.Five), Suit.Red);
            AccessTools.Method(typeof(UnoTable), "Redraw").Invoke(table, null);
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "02-match.png"));
            yield return new WaitForSecondsRealtime(.5f);
            var wildView=Resources.FindObjectsOfTypeAll<CardVisual>().First(v=>v.gameObject.activeInHierarchy && v.Interactive && v.Seat==0 && v.Index==3);
            wildView.OnPointerEnter(null);yield return new WaitForSecondsRealtime(.3f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"02-hover.png"));yield return new WaitForSecondsRealtime(.15f);
            wildView.GetComponentInChildren<Button>().onClick.Invoke();wildView.OnPointerExit(null);
            yield return new WaitForSecondsRealtime(.4f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "03-wild.png"));
            yield return new WaitForSecondsRealtime(.3f);
            var choose = Resources.FindObjectsOfTypeAll<Button>().FirstOrDefault(b => b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t => t.text == "数学") && b.transform.parent.name == "Paper");
            if (choose == null) { File.WriteAllText(Path.Combine(dir,"failed.txt"), "Choose-subject button not found"); yield break; }
            choose.onClick.Invoke();
            if (engine.Color != Suit.Blue || engine.Player.Count != 6) { File.WriteAllText(Path.Combine(dir,"failed.txt"), "Wild card UI did not play"); yield break; }
            for(int frame=0;frame<90;frame++){ ScreenCapture.CaptureScreenshot(Path.Combine(dir,"motion-"+frame.ToString("000")+".png")); yield return new WaitForSecondsRealtime(.05f); }
            while ((bool)AccessTools.Property(typeof(UnoTable), "Animating").GetValue(table,null)) yield return null;
            engine.Fixture(new[]{new Card(Suit.Red,Face.One),new Card(Suit.Blue,Face.Two)},new[]{new Card(Suit.Yellow,Face.Three)},new Card(Suit.Green,Face.Nine),Suit.Green);
            AccessTools.Method(typeof(UnoTable), "Redraw").Invoke(table,null);
            var deck=(System.Collections.Generic.List<Card>)AccessTools.Field(typeof(UnoGame),"deck").GetValue(engine);deck.Add(new Card(Suit.Green,Face.Three));
            var draw=Resources.FindObjectsOfTypeAll<Button>().First(b=>b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t=>t.text=="摸一张"));
            draw.onClick.Invoke();
            for(int frame=0;frame<24;frame++){ScreenCapture.CaptureScreenshot(Path.Combine(dir,"draw-"+frame.ToString("000")+".png"));yield return new WaitForSecondsRealtime(.05f);}
            while ((bool)AccessTools.Property(typeof(UnoTable), "Animating").GetValue(table,null)) yield return null;
            if(engine.Player.Count!=3 || engine.PendingDrawIndex!=2 || engine.Turn!=0)throw new Exception("Draw auto-played instead of awaiting choice");
            ScreenCapture.CaptureScreenshot(Path.Combine(dir,"03-manual-draw.png"));yield return new WaitForSecondsRealtime(.2f);
            draw.onClick.Invoke();if(engine.Turn!=1 || engine.Player.Count!=3)throw new Exception("Pass button failed");
            var oldHints=new[]{"只剩一张，记得报到","摸到可出的牌自动打出","当前科目"};
            if(Resources.FindObjectsOfTypeAll<Text>().Any(t=>t.gameObject.activeInHierarchy && oldHints.Contains(t.text)))throw new Exception("Obsolete HUD remains");
            engine.Fixture(new[]{new Card(Suit.Red,Face.One),new Card(Suit.Blue,Face.Two)},new[]{new Card(Suit.Yellow,Face.Three)},new Card(Suit.Green,Face.Nine),Suit.Green);
            engine.Tick(600,false);
            if(engine.Result!=Outcome.Playing || engine.Player.Count!=2)throw new Exception("Untimed match ended or forced a player draw");
            AccessTools.Method(typeof(UnoTable),"Redraw").Invoke(table,null);
            if(Resources.FindObjectsOfTypeAll<Button>().Any(b=>b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t=>t.text=="退出牌局" || t.text=="收牌")))throw new Exception("Unexpected in-round exit button");
            engine.Fixture(new[]{new Card(Suit.Red,Face.Three),new Card(Suit.Blue,Face.Two)},new[]{new Card(Suit.Red,Face.One)},new Card(Suit.Red,Face.Two),Suit.Red);
            engine.Play(0,0,Suit.Red);engine.Play(1,0,Suit.Red);
            while ((bool)AccessTools.Property(typeof(UnoTable), "Animating").GetValue(table,null)) yield return null;
            yield return new WaitForSecondsRealtime(.5f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "04-result.png"));
            yield return new WaitForSecondsRealtime(1);
            var close = Resources.FindObjectsOfTypeAll<Button>().First(b => b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t => t.text == "收好卡牌，回到校园")); close.onClick.Invoke();
            yield return null;
            File.WriteAllText(Path.Combine(dir, "result.json"), "{\"runtime\":\"Unity 2020 Windows x64 under CrossOver\",\"practice\":true,\"nativeGuidePages\":true,\"manualDrawPass\":true,\"oldHudRemoved\":true,\"wildButton\":true,\"closed\":" + (plugin.Table == null ? "true" : "false") + ",\"socialTested\":false,\"timeScale\":" + Time.timeScale.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}");
            plugin.Log("UNO_SMOKE_OK");
            if (File.Exists(Path.Combine(dir, "save-fixture", "fixture.save"))) yield return SocialAudit(dir);
        }

        // Older builds keep PathDefine paths in static fields; newer builds compute them on demand (read-only).
        static bool RedirectSavePaths(string saveDir)
        {
            var type = typeof(PathDefine);
            var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;
            var names = new[] { "SAVE_PATH", "TEST_SAVE_PATH", "IMG_PATH", "MUSIC_PATH" };
            var values = new[] { saveDir, saveDir, Path.Combine(saveDir, "Images"), Path.Combine(saveDir, "Musics") };
            var fields = new System.Reflection.FieldInfo[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                fields[i] = type.GetField(names[i], flags);
                if (fields[i] == null || fields[i].IsInitOnly) return false;
            }
            for (int i = 0; i < fields.Length; i++) fields[i].SetValue(null, values[i]);
            return true;
        }

        IEnumerator SocialAudit(string dir)
        {
            var plugin = Plugin.Instance;
            int initialErrors = plugin.ErrorCount;
            // Redirect all native save paths before loading the copied fixture. Never save to the user's folder.
            string saveDir = Path.Combine(dir, "save-fixture");
            if (!RedirectSavePaths(saveDir))
            {
                Debug.LogWarning("UNO smoke | social audit skipped: this game build computes save paths from Steam and they cannot be redirected");
                yield break;
            }
            bool loaded = false, done = false;
            SaveMgrEx.LoadAynsc(saveDir, "fixture.save", 16, ok => { loaded = ok; done = true; });
            float until = Time.realtimeSinceStartup + 20;
            while (!done && Time.realtimeSinceStartup < until) yield return null;
            if (!loaded) { File.WriteAllText(Path.Combine(dir, "social-failed.txt"), "Fixture load failed"); yield break; }
            var manager = Singleton<RoleMgr>.Ins;
            var player = manager.GetRole();
            var npcs = Cfg.PersonGrowCfgMap.Keys.Select(id => manager.GetRole(id)).Where(r => r != null && r.id != 0).Take(2).ToArray();
            if (npcs.Length < 2) { File.WriteAllText(Path.Combine(dir, "social-failed.txt"), "Fixture lacks two available NPCs"); yield break; }
            // Explicit in-memory fixtures: recognized NPCs on a school day. No modified fixture is saved.
            foreach (var npc in npcs) npc.Relation = 3;
            var holidaySeasons = Cfg.SeasonTypeCfgMap[501].seasons.ToArray(); Cfg.SeasonTypeCfgMap[501].seasons.Clear();
            int stage = plugin.GameId.Value * 100 + 1;
            var cfg = Cfg.MinigameActionCfgMap[stage]; cfg.cost = 4; cfg.needRelation = 0; cfg.startTalk = cfg.winTalk = cfg.loseTalk = 0;
            player.UpdateAttr(3, 100, 1, "UNO QA");
            foreach (var npc in npcs) Cfg.PersonGrowCfgMap[npc.id].minigame = plugin.GameId.Value;
            var data = Singleton<FuncMgr>.Ins.GetMiniGameData();
            if (data.GetCost(npcs[0].id) != 4 || data.IsFinish(npcs[0].id)) throw new Exception("Social metadata query failed");
            float trust = player.GetAttr(3), favor0 = npcs[0].GetFavor(), favor1 = npcs[1].GetFavor(), expectedFavor = npcs[0].GetRealAddFavor(2);
            // First NPC wins through the patched native SocialGame entry.
            if (!data.SocialGame(npcs[0].id, 0)) throw new Exception("Native social entry rejected fixture");
            if (data.SocialGame(npcs[1].id, 0)) throw new Exception("Concurrent session accepted");
            plugin.Table.Begin();
            if (Math.Abs(player.GetAttr(3) - (trust - 4)) > .01f) throw new Exception("Trust was not charged exactly once");
            plugin.Table.Engine.Fixture(new[] { new Card(Suit.Red, Face.One) }, new[] { new Card(Suit.Blue, Face.Two) }, new Card(Suit.Red, Face.Three), Suit.Red);
            plugin.Table.Engine.Play(0, 0, Suit.Red);
            yield return new WaitForSecondsRealtime(2.8f);
            ScreenCapture.CaptureScreenshot(Path.Combine(dir, "05-social-win.png"));
            yield return new WaitForSecondsRealtime(.4f);
            var close = Resources.FindObjectsOfTypeAll<Button>().First(b => b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t => t.text == "收好卡牌，回到校园")); close.onClick.Invoke(); close.onClick.Invoke();
            yield return null;
            if (Math.Abs(npcs[0].GetFavor() - (favor0 + expectedFavor)) > .01f || npcs[1].GetFavor() != favor1) throw new Exception("Favor went to wrong NPC or did not follow native modifiers");
            // Another NPC: prepare/cancel charges nothing; then play/lose, awarding nothing.
            if (!data.SocialGame(npcs[1].id, 0)) throw new Exception("Second NPC entry failed");
            plugin.Table.Abort(); yield return null;
            if (Math.Abs(player.GetAttr(3) - (trust - 4)) > .01f) throw new Exception("Ready cancellation charged trust");
            if (!data.SocialGame(npcs[1].id, 0)) throw new Exception("Second NPC replay failed");
            plugin.Table.Begin();
            plugin.Table.Engine.Fixture(new[]{new Card(Suit.Red,Face.Three),new Card(Suit.Blue,Face.Two)},new[]{new Card(Suit.Red,Face.One)},new Card(Suit.Red,Face.Two),Suit.Red);
            plugin.Table.Engine.Play(0,0,Suit.Red);plugin.Table.Engine.Play(1,0,Suit.Red);
            yield return new WaitForSecondsRealtime(3.2f);
            close = Resources.FindObjectsOfTypeAll<Button>().First(b => b.gameObject.activeInHierarchy && b.GetComponentsInChildren<Text>().Any(t => t.text == "收好卡牌，回到校园")); close.onClick.Invoke();
            yield return null;
            if (Math.Abs(player.GetAttr(3) - (trust - 8)) > .01f || npcs[1].GetFavor() != favor1) throw new Exception("Second NPC loss settlement incorrect");
            if (data.IsFinish(npcs[0].id) || data.IsFinish(npcs[1].id)) throw new Exception("Repeatable games became finished");
            if (plugin.ErrorCount != initialErrors) throw new Exception("Native settlement raised an error; do not mark audit passed");
            File.WriteAllText(Path.Combine(dir, "social-result.json"), "{\"nativeSocialEntry\":true,\"twoNpcs\":true,\"exactTrust\":true,\"favorTarget\":true,\"cancelBeforeStart\":true,\"repeatable\":true,\"concurrentRejected\":true,\"naturalMapClick\":false,\"saveWritten\":false}");
            plugin.Log("UNO_SOCIAL_AUDIT_OK");
            Cfg.SeasonTypeCfgMap[501].seasons.AddRange(holidaySeasons);
        }
    }
}
