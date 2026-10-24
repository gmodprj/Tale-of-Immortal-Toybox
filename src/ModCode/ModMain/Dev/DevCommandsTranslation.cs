namespace MOD_sWP6UL
{
    // 한글화의 검증용 명령(tools/research/ui-check.ps1의 translation 부분).
    //   rloc           : TranslationService.Apply의 마지막 결과 file=… rows=… applied=… fallback=… unused=… broken=…
    //   rloc apply     : 지금 파일로 다시 적용하고 결과
    //   rloc dump      : 사전에 없는 모드 행을 translations\untranslated.txt에 "키<TAB><TAB>중국어"로. written=<수>
    //   rloc key <키>  : ls=<GameTool.LS> kr=<행의 kr> ch=<행의 ch>
    internal static class DevCommandsTranslation
    {
        public static string Execute(string[] p)
        {
            if (p[0] != "rloc")
            {
                return null;
            }
            if (p.Length == 1)
            {
                return TranslationService.LastSummary;
            }
            switch (p[1])
            {
                case "apply":
                    return TranslationService.Apply();
                case "dump":
                    return "written=" + TranslationService.DumpUntranslated();
                case "key":
                    return TranslationService.Describe(p[2]);
                default:
                    return "unknown rloc " + p[1];
            }
        }
    }
}
