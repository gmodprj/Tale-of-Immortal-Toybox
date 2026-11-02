using System;
using System.Collections.Generic;
using System.IO;

namespace MOD_sWP6UL
{
    // Fatury 그림 폴더의 대체 경로 판정(05 문서 98절). 게임 형식 없이 파일 시스템만 본다.
    // Fatury는 구독 모드의 그림을 <모드 폴더>\ModAssets\Fatury에서만 찾는다. 꾸러미가 debug\Mod_<ID>\ 안에 든 모드
    // ([剑来] 2818763591, 등선 모녀 2905956493)는 거기 그림이 없어 의상·입화가 빈다.
    internal static class FaturyPathRules
    {
        // home이 없는 "...\<모드 폴더>\ModAssets\Fatury"이면 그 모드 폴더의 debug\*\ModAssets\Fatury 가운데 있는 것 하나(이름순 첫째)를 준다.
        // 그 밖에는 null(원래 경로를 쓴다).
        public static string Alternative(string home, Func<string, bool> exists, Func<string, IEnumerable<string>> subdirs)
        {
            if (string.IsNullOrEmpty(home) || exists(home))
            {
                return null;
            }
            string fatury = Path.GetFileName(home.TrimEnd('\\', '/'));
            string assets = Path.GetDirectoryName(home.TrimEnd('\\', '/'));
            if (!string.Equals(fatury, "Fatury", StringComparison.OrdinalIgnoreCase) || assets == null
                || !string.Equals(Path.GetFileName(assets), "ModAssets", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            string modRoot = Path.GetDirectoryName(assets);
            if (modRoot == null)
            {
                return null;
            }
            string debug = Path.Combine(modRoot, "debug");
            if (!exists(debug))
            {
                return null;
            }
            var found = new List<string>();
            foreach (string dir in subdirs(debug))
            {
                string candidate = Path.Combine(dir, "ModAssets", "Fatury");
                if (exists(candidate))
                {
                    found.Add(candidate);
                }
            }
            if (found.Count == 0)
            {
                return null;
            }
            found.Sort(StringComparer.OrdinalIgnoreCase);
            return found[0];
        }
    }
}
