using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 극 대화 창의 배경 그림이 흰 판으로 나오는 까닭 조사(05 문서 94절). 표와 그림을 읽기만 한다.
    //   rdrama <대사 키|행 번호>   dramaDialogue에서 dialogue 칸이 그 키이거나 id가 그 번호인 줄과, nextDialogue로 그 줄을 가리키는 줄.
    //                               줄마다 id·uiType·backgroud·npcLeft·npcRight·speaker·nextDialogue·options와 배경 점검(아래).
    //   rdrama bgs <출력 파일>      backgroud 칸의 서로 다른 값 전부를 점검해 파일에 "값<TAB>줄 수<TAB>점검" 줄로 쓴다(그림은 읽지 않는다).
    // 배경 점검: 이름의 '_' 앞(모드ID 접두사)이 실린 모드인지(ModMgr.IsLoadMod)와 그 뿌리(GetModPathRoot), 모드 폴더들의
    // Assets\Texture\FortuiousBigBG·Fortuitous에 "<값>.png"가 있는 곳. 한 줄 명령에서는 ModMgr.LoadSpriteBigTex(값)도 불러 그림 크기를 본다.
    internal static class DevCommandsDrama
    {
        private static readonly string[] TexDirs = { "FortuiousBigBG", "Fortuitous" };

        public static string Execute(string[] p)
        {
            if (p[0] != "rdrama")
            {
                return null;
            }
            if (p.Length < 2)
            {
                return "usage: rdrama <대사 키|행 번호> | rdrama bgs <출력 파일>";
            }
            try
            {
                List<ConfDramaDialogueItem> rows = Rows();
                if (rows == null)
                {
                    return "dramaDialogue=null";
                }
                if (p[1] == "bgs")
                {
                    return p.Length < 3 ? "usage: rdrama bgs <출력 파일>" : AllBackgrounds(rows, string.Join(" ", p, 2, p.Length - 2));
                }
                return Find(rows, p[1]);
            }
            catch (Exception e)
            {
                return "error " + e.GetType().Name + ": " + e.Message;
            }
        }

        private static List<ConfDramaDialogueItem> Rows()
        {
            object conf = g.conf.dramaDialogue;
            if (conf == null)
            {
                return null;
            }
            // _allConfList가 List<T>다(TextTranslationService.ApplyTable과 같은 길).
            PropertyInfo listProperty = conf.GetType().GetProperty("_allConfList") ?? conf.GetType().GetProperty("allConfList");
            object list = listProperty == null ? null : listProperty.GetValue(conf, null);
            if (list == null)
            {
                return null;
            }
            int count = (int)list.GetType().GetProperty("Count").GetValue(list, null);
            PropertyInfo item = list.GetType().GetProperty("Item");
            var rows = new List<ConfDramaDialogueItem>(count);
            for (int i = 0; i < count; i++)
            {
                var row = item.GetValue(list, new object[] { i }) as ConfDramaDialogueItem;
                if (row != null)
                {
                    rows.Add(row);
                }
            }
            return rows;
        }

        private static string Find(List<ConfDramaDialogueItem> rows, string key)
        {
            var hits = new List<ConfDramaDialogueItem>();
            foreach (ConfDramaDialogueItem row in rows)
            {
                if (row.dialogue == key || row.id.ToString() == key)
                {
                    hits.Add(row);
                }
            }
            var sb = new StringBuilder();
            sb.Append("rows=").Append(rows.Count).Append(" hits=").Append(hits.Count);
            foreach (ConfDramaDialogueItem hit in hits)
            {
                sb.Append("\n").Append(Describe(hit, true));
                string id = hit.id.ToString();
                foreach (ConfDramaDialogueItem row in rows)
                {
                    if (row.nextDialogue == id)
                    {
                        sb.Append("\n  from ").Append(Describe(row, false));
                    }
                }
            }
            return sb.ToString();
        }

        private static string Describe(ConfDramaDialogueItem row, bool load)
        {
            var sb = new StringBuilder();
            sb.Append("id=").Append(row.id).Append(" uiType=").Append(row.uiType).Append(" bg=").Append(row.backgroud)
              .Append(" npcLeft=").Append(row.npcLeft).Append(" npcRight=").Append(row.npcRight).Append(" speaker=").Append(row.speaker)
              .Append(" dialogue=").Append(row.dialogue).Append(" next=").Append(row.nextDialogue).Append(" options=").Append(row.options);
            if (!string.IsNullOrEmpty(row.backgroud) && row.backgroud != "0")
            {
                sb.Append(" | ").Append(Check(row.backgroud));
                if (load)
                {
                    sb.Append(" | ").Append(LoadInfo(row.backgroud));
                }
            }
            return sb.ToString();
        }

        private static string Check(string bg)
        {
            var sb = new StringBuilder();
            int cut = bg.IndexOf('_');
            string prefix = cut > 0 ? bg.Substring(0, cut) : "";
            sb.Append("prefix=").Append(prefix == "" ? "-" : prefix);
            if (prefix != "")
            {
                bool loaded = false;
                string root = null;
                try { loaded = g.mod.IsLoadMod(prefix); } catch (Exception) { }
                try { root = g.mod.GetModPathRoot(prefix); } catch (Exception) { }
                sb.Append(" loaded=").Append(loaded).Append(" root=").Append(string.IsNullOrEmpty(root) ? "-" : root);
            }
            sb.Append(" files=");
            int found = 0;
            string top = ModMgr.pathModExportData;
            if (!string.IsNullOrEmpty(top) && Directory.Exists(top))
            {
                foreach (string folder in Directory.GetDirectories(top))
                {
                    foreach (string sub in TexDirs)
                    {
                        string file = Path.Combine(folder, "Assets", "Texture", sub, bg + ".png");
                        if (File.Exists(file))
                        {
                            sb.Append(Path.GetFileName(folder)).Append('/').Append(sub).Append(';');
                            found++;
                        }
                    }
                }
            }
            if (found == 0)
            {
                sb.Append("none");
            }
            return sb.ToString();
        }

        // 대화 창이 배경을 읽는 길은 미확인이라(IL2CPP 본문 없음) 경로 형식 몇 가지를 g.res.Load와 ModMgr.LoadSpriteBigTex로 시험한다.
        // 만고신화의 파일이 있는 배경(대조)에서 그림이 나오는 형식이 게임이 쓰는 길이다.
        private static readonly string[] PathForms = { "{0}", "Texture/FortuiousBigBG/{0}", "Texture/BG/{0}", "Texture/Fortuitous/{0}" };

        private static string LoadInfo(string bg)
        {
            var sb = new StringBuilder();
            foreach (string form in PathForms)
            {
                string path = string.Format(form, bg);
                sb.Append(" [").Append(path).Append("] res=").Append(Describe(() => g.res.Load(path)));
                sb.Append(" big=").Append(Describe(() => g.mod.LoadSpriteBigTex(path)));
            }
            // 모드 폴더의 그림 파일을 게임의 복호화로 직접 풀어 본다(ModTool.LoadSpriteInFile(file, true)).
            string top = ModMgr.pathModExportData;
            if (!string.IsNullOrEmpty(top) && Directory.Exists(top))
            {
                foreach (string folder in Directory.GetDirectories(top))
                {
                    foreach (string sub in TexDirs)
                    {
                        string file = Path.Combine(folder, "Assets", "Texture", sub, bg + ".png");
                        if (File.Exists(file))
                        {
                            sb.Append(" [file ").Append(Path.GetFileName(folder)).Append('/').Append(sub).Append("] ")
                              .Append(Describe(() => ModTool.LoadSpriteInFile(file, true)));
                        }
                    }
                }
            }
            return sb.ToString();
        }

        private static string Describe(Func<UnityEngine.Object> load)
        {
            try
            {
                UnityEngine.Object o = load();
                if (o == null)
                {
                    return "null";
                }
                Sprite s = o.TryCast<Sprite>();
                if (s != null)
                {
                    return "Sprite " + (s.texture == null ? "-" : s.texture.width + "x" + s.texture.height);
                }
                Texture t = o.TryCast<Texture>();
                if (t != null)
                {
                    return "Texture " + t.width + "x" + t.height;
                }
                return o.GetIl2CppType().Name;
            }
            catch (Exception e)
            {
                return "error " + e.GetType().Name;
            }
        }

        private static string AllBackgrounds(List<ConfDramaDialogueItem> rows, string outFile)
        {
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            foreach (ConfDramaDialogueItem row in rows)
            {
                string bg = row.backgroud;
                if (string.IsNullOrEmpty(bg) || bg == "0")
                {
                    continue;
                }
                int c;
                if (!counts.TryGetValue(bg, out c))
                {
                    order.Add(bg);
                }
                counts[bg] = c + 1;
            }
            var sb = new StringBuilder();
            int missing = 0;
            foreach (string bg in order)
            {
                string check = Check(bg);
                if (check.EndsWith("files=none"))
                {
                    missing++;
                }
                sb.Append(bg).Append('\t').Append(counts[bg]).Append('\t').Append(check).Append('\n');
            }
            File.WriteAllText(outFile, sb.ToString(), new UTF8Encoding(false));
            return "rows=" + rows.Count + " backgrounds=" + order.Count + " nofile=" + missing + " out=" + outFile;
        }
    }
}
