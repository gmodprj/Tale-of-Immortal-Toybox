using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 이미 있는 프리셋 폴더에 새로 생긴 기본 프리셋만 풀기(docs/superpowers/specs/2026-10-20-preset-drafts-design.md 6.1절).
    // 폴더의 .defaults에 풀어 놓은 기본 파일 이름을 적어 두고, 거기에 없는 이름만 새 기본값으로 본다.
    // 사용자가 지운 기본 파일은 기록에 남아 있으므로 다시 풀지 않는다. 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests).
    internal static class PresetDefaults
    {
        public const string RecordName = ".defaults";

        // 기록이 없는 옛 폴더(2026-10-20 이전)가 알고 있던 기본 파일. 이 밖의 내장 이름은 새 기본값이다.
        public static readonly string[] Legacy =
        {
            "01-풍검.json", "02-수검.json", "03-화검.json", "04-화권.json", "05-도.json",
            "06-수영술.json", "07-창-과룡문.json", "08-화영술.json", "09-목화.json", "10-뇌영술.json",
        };

        // 풀어야 할 내장 파일 이름(이름순). record가 null이면 옛 폴더로 보고 Legacy를 알고 있던 것으로 본다.
        public static List<string> ToWrite(IList<string> embedded, IList<string> record, ICollection<string> onDisk)
        {
            var known = new HashSet<string>(record ?? (IList<string>)Legacy, StringComparer.OrdinalIgnoreCase);
            var disk = new HashSet<string>(onDisk, StringComparer.OrdinalIgnoreCase);
            var result = new List<string>();
            foreach (string name in embedded)
            {
                if (!known.Contains(name) && !disk.Contains(name))
                {
                    result.Add(name);
                }
            }
            result.Sort(StringComparer.OrdinalIgnoreCase);
            return result;
        }
    }
}
