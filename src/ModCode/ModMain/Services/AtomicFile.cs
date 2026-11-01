using System;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 사용자가 손으로 고치는 파일(번역표 셋·config.txt·martial-grid.txt·즐겨찾기)을 쓰는 도중 게임이 죽어도 파일이 사라지지 않게,
    // 임시 파일(<path>.tmp)에 다 쓴 뒤 바꿔치기한다(FavoriteStore.Change가 쓰던 꼴. 13 문서 A7, 05 문서 92절).
    // 쓰기 도중의 예외는 그대로 던진다(호출부가 로그로 받는다). 임시 파일은 대상이 남아 있을 때만 지운다 —
    // ReplaceFile이 대상을 잃은 채 실패하면(오류 1176) 임시 파일이 새 내용의 유일한 사본이라, 대상 이름으로 되살린다(최종 리뷰).
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
            }
            catch (Exception)
            {
                // 임시 파일조차 못 썼다: 대상은 건드리지 않았으니 조각만 지운다.
                TryDelete(tmp);
                throw;
            }
            try
            {
                if (File.Exists(path))
                {
                    File.Replace(tmp, path, null);
                }
                else
                {
                    File.Move(tmp, path);
                }
            }
            catch (Exception)
            {
                Recover(path, tmp);
                throw;
            }
            TryDelete(tmp);
        }

        // 바꿔치기가 실패한 뒤의 정리: 대상이 사라졌고 임시 파일이 남아 있으면 임시 파일을 대상 이름으로 옮긴다(새 내용으로 살림).
        // 대상이 남아 있으면 임시 파일만 지운다. 둘 다 실패해도 던지지 않는다(원래 예외가 더 중요하다).
        public static void Recover(string path, string tmp)
        {
            try
            {
                if (!File.Exists(path) && File.Exists(tmp))
                {
                    File.Move(tmp, path);
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            if (File.Exists(path))
            {
                TryDelete(tmp);
            }
        }

        private static void TryDelete(string file)
        {
            try
            {
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
