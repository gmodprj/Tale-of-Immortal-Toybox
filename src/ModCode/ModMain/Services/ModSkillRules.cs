using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 모드 공법 목록 정리의 판정 규칙(게임 형식 없이 시험한다. 묶음 2, 05 문서 78절).
    internal static class ModSkillRules
    {
        // M1: 이름 키가 LocalText에 없을 때 그 글을 이름으로 쓸지. 모드가 이름 칸에 번역 키 대신 이름 글자를 그대로 넣은 경우(52개).
        // 비지 않고, "0"이 아니고, 자리 표시({…})를 품지 않고, 영문·숫자·밑줄만으로 된 키 꼴이 아니면 이름이다.
        // 키 꼴은 원래 글로 본다(서식 태그가 든 이름은 키 꼴이 아니다).
        public static bool IsLiteralName(string nameKey)
        {
            if (string.IsNullOrEmpty(nameKey) || nameKey == "0" || nameKey.IndexOf('{') >= 0)
            {
                return false;
            }
            foreach (char c in nameKey)
            {
                bool keyChar = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!keyChar)
                {
                    return true;
                }
            }
            return false;
        }

        // M2: 이름 키가 비었거나 "0"이고 설명도 없는 모드 행(459개). 무엇인지 알 수 없어 목록에서 숨긴다(사용자 결정 2026-10-23).
        public static bool IsBlankRow(string nameKey, string descText)
        {
            bool noName = string.IsNullOrEmpty(nameKey) || nameKey == "0";
            bool noDesc = string.IsNullOrEmpty(descText) || descText == "0";
            return noName && noDesc;
        }

        // M4: 모드 공법의 비급이 만들어지는 경지 범위. 못 만들면 0~0.
        // 실측(회차 msl-1023c, 경지 1·2·5·10, 10번 따로): 옵션 행이 있으면 절기·신통은 1~10, 무예/영술은 경지 1에서만 실패(2~10),
        // 신법은 어느 경지·어느 길로도 읽히지 않았다. 옵션 행이 0개면(이름 없는 심법 435 등) 만들 수 없다.
        // kindIndex는 SkillCatalog.Kinds의 순번(0 무예/영술, 1 절기, 2 신법, 3 심법, 4 신통).
        // multiSum은 옵션 행마다 min(multi, 11)의 합: 남는 칸을 같은 번호 되풀이로 채울 수 있는 칸 수. 비급의 옵션 칸은 많아야 11이므로
        // 11보다 작으면 칸을 다 못 채울 수 있어 못 만듦으로 본다(실측된 성공 97행은 모두 11 이상, 최종 리뷰 Important 1).
        public const int MaxSlots = 11;

        public static void BookGrades(int kindIndex, int optionRows, int multiSum, out int min, out int max)
        {
            min = 0;
            max = 0;
            if (optionRows <= 0 || kindIndex == 2 || multiSum < MaxSlots)
            {
                return;
            }
            min = kindIndex == 0 ? 2 : 1;
            max = 10;
        }

        // 비급 값의 옵션 구간 끝: 2번부터 첫 -1 앞까지. -1이 없으면 길이.
        public static int OptionEnd(int[] values)
        {
            for (int i = 2; i < values.Length; i++)
            {
                if (values[i] == -1)
                {
                    return i;
                }
            }
            return values.Length;
        }

        // 비급 값의 2번부터 end 앞까지는 옵션 번호다. 게임은 옵션을 (skillType, 기술 번호, number)로 행에서 찾으므로(05 문서 44절),
        // 원본에서 뽑은 번호 가운데 모드 공법(rows)에 없는 것을 모드 공법에 있는(아직 안 쓴) 번호로 바꾼다. 있는 번호는 그대로 둔다.
        //   첫 패스: 그 자리(i-2)의 가중치가 0보다 큰 안 쓴 번호 가운데 무작위(rng(n)은 [0,n)). 없으면 안 쓴 첫 번호.
        //   둘째 패스(모드 번호를 다 써도 남은 자리): 같은 번호를 중복 한도(Multi) 안에서 되풀이. 자리 가중치 > 0인 번호를 먼저, 없으면 한도 안의 아무 번호.
        // 실측(78절): 이름·설명 있는 모드 공법 103개 가운데 97개가 10번 모두 읽히고 게임 학습이 받아들였다. 바꾼 수를 돌려준다.
        public static int RemapPrefixNumbers(int[] values, int end, List<PrefixRow> rows, Func<int, int> rng)
        {
            int changed = 0;
            if (rows == null || rows.Count == 0)
            {
                return 0;
            }
            var modNumbers = new HashSet<int>();
            foreach (PrefixRow row in rows)
            {
                modNumbers.Add(row.Number);
            }
            var used = new HashSet<int>();
            for (int i = 2; i < end; i++)
            {
                if (modNumbers.Contains(values[i]))
                {
                    used.Add(values[i]);
                }
            }
            for (int i = 2; i < end; i++)
            {
                if (values[i] <= 0 || modNumbers.Contains(values[i]))
                {
                    continue;
                }
                var fit = new List<int>();
                int first = -1;
                foreach (PrefixRow row in rows)
                {
                    if (used.Contains(row.Number))
                    {
                        continue;
                    }
                    if (first < 0)
                    {
                        first = row.Number;
                    }
                    if (row.Weight(i - 2) > 0)
                    {
                        fit.Add(row.Number);
                    }
                }
                int pick = fit.Count > 0 ? fit[rng(fit.Count)] : first;
                if (pick >= 0)
                {
                    values[i] = pick;
                    used.Add(pick);
                    changed++;
                }
            }
            var count = new Dictionary<int, int>();
            for (int i = 2; i < end; i++)
            {
                if (modNumbers.Contains(values[i]))
                {
                    int c;
                    count.TryGetValue(values[i], out c);
                    count[values[i]] = c + 1;
                }
            }
            for (int i = 2; i < end; i++)
            {
                if (values[i] <= 0 || modNumbers.Contains(values[i]))
                {
                    continue;
                }
                int pick = -1;
                for (int pass = 0; pass < 2 && pick < 0; pass++)
                {
                    foreach (PrefixRow row in rows)
                    {
                        int c;
                        count.TryGetValue(row.Number, out c);
                        if (c < row.Multi && (pass == 1 || row.Weight(i - 2) > 0))
                        {
                            pick = row.Number;
                            break;
                        }
                    }
                }
                if (pick < 0)
                {
                    break;
                }
                values[i] = pick;
                changed++;
                int cc;
                count.TryGetValue(pick, out cc);
                count[pick] = cc + 1;
            }
            return changed;
        }
    }
}
