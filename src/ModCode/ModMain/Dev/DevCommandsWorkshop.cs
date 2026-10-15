using System.Text;

namespace MOD_sWP6UL
{
    // 창작마당 모드 연동(G2a)의 설계 전 실험용 명령(실험 브랜치 feat/workshop-research).
    //   wsmods : path=<ModMgr.pathModExportData> loaded=<loadedModCount> cache=<isModCache>
    //            paths=<allModPaths의 t1|t2;…> mods=<modDatas의 soleID:name:ver:publishedFileID;…>
    //            ns=<loadModNamespace;…> order=<GetLoadOrderData의 modFolderName=isLoad;…>
    internal static class DevCommandsWorkshop
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "wsmods":
                    return State();
                default:
                    return null;
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
