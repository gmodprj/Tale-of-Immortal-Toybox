using System;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 사용자가 손으로 고치는 파일(번역표 셋·config.txt·martial-grid.txt·즐겨찾기)을 쓰는 도중 게임이 죽어도 파일이 사라지지 않게,
    // 임시 파일(<path>.tmp)에 다 쓴 뒤 바꿔치기한다(FavoriteStore.Change가 쓰던 꼴. 13 문서 A7, 05 문서 92절).
    // 쓰기 도중의 예외는 그대로 던진다(호출부가 로그로 받는다). 임시 파일은 지운다.
    internal static class AtomicFile
    {
        public static void WriteAllText(string path, string text, Encoding encoding)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string tmp = path + ".tmp";
            try
            {
                File.WriteAllText(tmp, text, encoding);
                if (File.Exists(path))
                {
                    File.Replace(tmp, path, null);
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
            finally
            {
                if (File.Exists(tmp))
                {
                    try
                    {
                        File.Delete(tmp);
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }
    }
}
