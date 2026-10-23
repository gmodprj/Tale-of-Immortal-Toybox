using System;
using System.IO;
using System.Text;

namespace MOD_sWP6UL
{
    // 폴더 안 파일들의 표지(이름·크기·수정 시각). 같으면 파일을 다시 읽지 않아도 된다(묶음 3, 프리셋 탭).
    internal static class FileStamp
    {
        // dir 안 pattern 파일을 이름순으로 "이름|크기|수정 시각(UTC ticks)"을 ;로 잇는다. 폴더가 없으면 "-".
        public static string Of(string dir, string pattern)
        {
            if (!Directory.Exists(dir))
            {
                return "-";
            }
            string[] files = Directory.GetFiles(dir, pattern);
            Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            foreach (string file in files)
            {
                var info = new FileInfo(file);
                sb.Append(info.Name).Append('|').Append(info.Length).Append('|').Append(info.LastWriteTimeUtc.Ticks).Append(';');
            }
            return sb.ToString();
        }
    }
}
