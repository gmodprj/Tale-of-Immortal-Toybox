using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 극 대화 창의 배경 그림이 흰 판으로 나오는 까닭 조사(05 문서 94절). 표와 그림을 읽기만 한다.
    //   rdrama open <행 번호> | close | ui | fix on|off   대화 줄을 열고 닫기, 열린 극 창(기연·큰 그림)의 배경 Image 상태, 흰 배경 고치기 켜고 끄기(95절).
    //   rdrama bg=<배경 이름>      그 배경을 쓰는 줄들(아래와 같은 형식).
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
                if (p[1] == "open")
                {
                    // 대화 줄 하나를 연다(DramaTool.OpenDrama). 선택지는 누르지 않는다. 닫기는 rdrama close.
                    int id;
                    return p.Length < 3 || !int.TryParse(p[2], out id) ? "usage: rdrama open <행 번호>" : "opened=" + DramaTool.OpenDrama(id, new DramaData());
                }
                if (p[1] == "close")
                {
                    // DramaTool.CloseDrame만으로는 큰 그림 창이 남았다(회차 dbg-d-1031). 극 창을 직접 닫는다.
                    DramaTool.CloseDrame();
                    g.ui.CloseUI(UIType.DramaFortuitous, true);
                    g.ui.CloseUI(UIType.DramaDialogueBigTexture, true);
                    g.ui.CloseUI(UIType.DramaDialogue, true);
                    g.ui.CloseUI(UIType.DramaBigTexture, true);
                    return "closed open=" + DramaTool.IsOpenDrama();
                }
                if (p[1] == "fort")
                {
                    // 그림 파일이 있는 모드 배경이나 접두사 없는 게임 배경을 쓰는 기연 줄(uiType 2)을 몇 개 보인다(대조용).
                    return FortuitousSamples(rows);
                }
                if (p[1] == "fix")
                {
                    if (p.Length >= 3)
                    {
                        DramaBackgroundService.Enabled = p[2] == "on";
                    }
                    return "fix=" + (DramaBackgroundService.Enabled ? "on" : "off") + " " + DramaBackgroundService.LastNote;
                }
                if (p[1] == "ui")
                {
                    return UiState();
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

        // 열린 극 창(기연 창·큰 그림 대화 창)의 배경 Image 상태와, 열린 창 이름 가운데 Drama가 든 것.
        private static string UiState()
        {
            var sb = new StringBuilder();
            sb.Append("open=").Append(DramaTool.IsOpenDrama()).Append(" windows=");
            foreach (var pair in g.ui.allUI)
            {
                if (pair.Value != null && pair.Value.Count > 0 && pair.Key.IndexOf("Drama", StringComparison.Ordinal) >= 0)
                {
                    sb.Append(pair.Key).Append(';');
                }
            }
            UIDramaFortuitous fortuitous = g.ui.GetUI<UIDramaFortuitous>(UIType.DramaFortuitous);
            if (fortuitous != null)
            {
                sb.Append(" | fortuitous ").Append(Row(fortuitous.item))
                  .Append(" | imgBG1 ").Append(ImageState(fortuitous.imgBG1)).Append(" | imgBG2 ").Append(ImageState(fortuitous.imgBG2));
            }
            UIDramaDialogueBigTexture big = g.ui.GetUI<UIDramaDialogueBigTexture>(UIType.DramaDialogueBigTexture);
            if (big != null)
            {
                sb.Append(" | bigtexture ").Append(Row(big.item))
                  .Append(" | imgBG1 ").Append(ImageState(big.imgBG1)).Append(" | imgBG2 ").Append(ImageState(big.imgBG2));
            }
            sb.Append(" | ").Append(DramaBackgroundService.LastNote);
            return sb.ToString();
        }

        private static string FortuitousSamples(List<ConfDramaDialogueItem> rows)
        {
            string vanilla = null, mod = null;
            int count = 0;
            foreach (ConfDramaDialogueItem row in rows)
            {
                if (row.uiType != 2 || string.IsNullOrEmpty(row.backgroud) || row.backgroud == "0")
                {
                    continue;
                }
                count++;
                if (vanilla == null && row.backgroud.IndexOf('_') < 0)
                {
                    vanilla = "id=" + row.id + " bg=" + row.backgroud;
                }
                else if (mod == null && row.backgroud.IndexOf('_') > 0 && !Check(row.backgroud).EndsWith("files=none"))
                {
                    mod = "id=" + row.id + " bg=" + row.backgroud;
                }
            }
            return "fortuitousRows=" + count + " vanilla " + (vanilla ?? "none") + " ; mod " + (mod ?? "none");
        }

        private static string Row(ConfDramaDialogueItem item)
        {
            return item == null ? "id=- bg=-" : "id=" + item.id + " bg=" + item.backgroud;
        }

        private static string ImageState(UnityEngine.UI.Image img)
        {
            if (img == null)
            {
                return "null";
            }
            Color c = img.color;
            Sprite s = img.sprite;
            return "sprite=" + (s == null ? "null" : s.name + " " + (s.texture == null ? "-" : s.texture.width + "x" + s.texture.height))
                + " color=" + c.r.ToString("0.##") + "," + c.g.ToString("0.##") + "," + c.b.ToString("0.##") + "," + c.a.ToString("0.##")
                + " enabled=" + img.enabled + " active=" + img.gameObject.activeInHierarchy;
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
                if (row.dialogue == key || row.id.ToString() == key || (key.StartsWith("bg=") && row.backgroud == key.Substring(3)))
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
