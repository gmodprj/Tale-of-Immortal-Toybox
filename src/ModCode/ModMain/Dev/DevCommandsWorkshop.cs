using System;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 창작마당 모드 연동(G2a)의 설계 전 실험용 명령(실험 브랜치 feat/workshop-research).
    //   wsmods : path=<ModMgr.pathModExportData> loaded=<loadedModCount> cache=<isModCache>
    //            paths=<allModPaths의 t1|t2;…> mods=<modDatas의 soleID:name:ver:publishedFileID;…>
    //            ns=<loadModNamespace;…> order=<GetLoadOrderData의 modFolderName=isLoad;…>
    //   wsexcel <출력 파일> <모드 폴더> : ModImportTool.LoadModExportData로 그 폴더의 표(ModExportData.Data.items)를 풀어 "표<TAB>id" 줄로 쓴다.
    //                                     설치하지 않은 모드(받은 폴더)의 난독화된 ModExcel도 읽는다(05 문서 71절: 모드 사이 번호 겹침 조사).
    //                                     폴더는 마지막 인자이고 공백을 담을 수 있다. 게임 설정에 합치지 않는다(ModMgr.LoadMod는 합치고,
    //                                     이미 실린 모드를 다시 부르면 예외가 나거나 게임이 멈췄다: 회차 wg-1020c·d).
    internal static class DevCommandsWorkshop
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "wsmods":
                    return State();
                case "wsexcel":
                    return p.Length < 3 ? "usage: wsexcel <출력 파일> <폴더>" : ExcelOne(string.Join(" ", p, 2, p.Length - 2), p[1]);
                default:
                    return null;
            }
        }

        private static string ExcelOne(string folder, string outFile)
        {
            try
            {
                // 폴더가 아니라 그 안의 ModExportData.cache를 받는다(폴더를 주면 UnauthorizedAccessException).
                string file = folder.EndsWith(".cache", StringComparison.OrdinalIgnoreCase) ? folder : Path.Combine(folder, "ModExportData.cache");
                ModExportData.Data data = ModImportTool.LoadModExportData(file);
                if (data == null || data.items == null)
                {
                    return "data=null";
                }
                // cache 밖의 ModExcel\**\*.json(하위 폴더 표)은 data에 들지 않는다. 게임의 복호화(ModTool.LoadTextInFile)로 풀어
                // <출력 파일>.excel\<상대 경로>에 평문으로 쓴다(05 문서 72절: 만고신화의 XW\RoleGrade.json). ImportJson은 data가 아니라 설정에 바로 넣어 쓰지 않는다.
                int jsons = 0, jsonErrors = 0;
                string excelDir = Path.Combine(Path.GetDirectoryName(file), "ModExcel");
                if (Directory.Exists(excelDir))
                {
                    foreach (string json in Directory.GetFiles(excelDir, "*.json", SearchOption.AllDirectories))
                    {
                        try
                        {
                            string text = ModTool.LoadTextInFile(json, true);
                            string target = Path.Combine(outFile + ".excel", json.Substring(excelDir.Length + 1));
                            Directory.CreateDirectory(Path.GetDirectoryName(target));
                            File.WriteAllText(target, text ?? "", new UTF8Encoding(false));
                            jsons++;
                        }
                        catch (Exception)
                        {
                            jsonErrors++;
                        }
                    }
                }
                var sb = new StringBuilder();
                var full = new StringBuilder();
                int tables = 0, rows = 0;
                var e = data.items.GetEnumerator();
                while (e.MoveNext())
                {
                    tables++;
                    var list = e.Current.Value;
                    for (int i = 0; list != null && i < list.Count; i++)
                    {
                        var row = list[i];
                        string id = row != null && row.ContainsKey("id") ? row["id"] : "?";
                        sb.Append(e.Current.Key).Append('\t').Append(id).Append('\n');
                        rows++;
                        // 경지 표는 칸까지 남긴다(바닐라 행을 바꾸는지 보려고).
                        if (e.Current.Key == "RoleGrade" && row != null)
                        {
                            var f = row.GetEnumerator();
                            while (f.MoveNext())
                            {
                                full.Append(f.Current.Key).Append('=').Append(f.Current.Value).Append('\t');
                            }
                            full.Append('\n');
                        }
                    }
                }
                File.WriteAllText(outFile, sb.ToString(), new UTF8Encoding(false));
                if (full.Length > 0)
                {
                    File.WriteAllText(outFile + ".RoleGrade.txt", full.ToString(), new UTF8Encoding(false));
                }
                return "tables=" + tables + " rows=" + rows + " jsons=" + jsons + " jsonErrors=" + jsonErrors;
            }
            catch (Exception ex)
            {
                return "error " + ex.GetType().Name + ": " + ex.Message;
            }
        }

        private static string State()
        {
            ModMgr mod = g.mod;
            if (mod == null)
            {
                return "g.mod=null";
            }
            var sb = new StringBuilder();
            sb.Append("path=").Append(ModMgr.pathModExportData);
            sb.Append(" loaded=").Append(mod.loadedModCount);
            sb.Append(" cache=").Append(mod.isModCache);
            sb.Append(" paths=");
            var paths = mod.allModPaths;
            if (paths != null)
            {
                for (int i = 0; i < paths.Count; i++)
                {
                    var d = paths[i];
                    sb.Append(d.t1).Append('|').Append(d.t2).Append(';');
                }
            }
            sb.Append(" mods=");
            var datas = mod.modDatas;
            if (datas != null)
            {
                for (int i = 0; i < datas.Count; i++)
                {
                    var d = datas[i];
                    sb.Append(d.soleID).Append(':').Append(d.name).Append(':').Append(d.ver).Append(':').Append(d.publishedFileID).Append(';');
                }
            }
            sb.Append(" ns=");
            var ns = mod.loadModNamespace;
            if (ns != null)
            {
                for (int i = 0; i < ns.Count; i++)
                {
                    sb.Append(ns[i]).Append(';');
                }
            }
            sb.Append(" order=");
            var order = mod.GetLoadOrderData();
            if (order != null && order.mods != null)
            {
                for (int i = 0; i < order.mods.Count; i++)
                {
                    var it = order.mods[i];
                    sb.Append(it.modFolderName).Append('=').Append(it.isLoad).Append(';');
                }
            }
            return sb.ToString();
        }
    }
}
