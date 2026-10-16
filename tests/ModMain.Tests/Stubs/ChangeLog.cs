using System.IO;

namespace MOD_sWP6UL
{
    // 시험용 대역. 모드의 ChangeLog.DataDir은 Unity의 persistentDataPath를 쓰므로 링크하지 않고, ExportFiles가 쓰는 DataDir만 임시 폴더로 준다.
    internal static class ChangeLog
    {
        public static string DataDir
        {
            get { return Path.Combine(Path.GetTempPath(), "tltoybox-tests"); }
        }
    }
}
