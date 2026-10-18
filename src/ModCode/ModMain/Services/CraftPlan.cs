using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 기예 재료 한 벌의 계산(docs/superpowers/specs/2026-10-18-craft-materials-design.md 6절, 05 문서 61절).
    // 게임 형식을 쓰지 않는다(단위 시험 tests/ModMain.Tests). 게임 표를 읽어 입력을 만드는 것은 CraftMaterialService.
    internal static class CraftPlan
    {
        // 지급할 줄 하나. Id가 0이면 지급하지 않는 안내 줄(Note).
        public sealed class Line
        {
            public int Id;
            public int Count;
            public string Note;
        }

        public sealed class Herb
        {
            public int Id;
            public int Level;
            public int[] Elements;  // 화·수·뇌·풍·토·목·음·양
        }

        // 고를 후보. 연기 보조 재료는 Class·Mixed(불순물), 단로는 Mixed 칸에 basRequire, 불씨는 Level을 쓴다.
        public sealed class Candidate
        {
            public int Id;
            public int Class;
            public int Level;
            public int Mixed;
        }

        public static readonly string[] ElementNames = { "화", "수", "뇌", "풍", "토", "목", "음", "양" };

        // 연단: 주약 × 개수, 그리고 요구 오행에서 주약이 채우지 못한 몫을 그 오행 하나만 가진 약재로(가장 낮은 단계, 같으면 값이 큰 것, 그다음 번호가 작은 것).
        public static List<Line> PillSet(int[] need, IList<int> mainIds, IList<int> mainCounts, Func<int, int[]> elementsOf, IList<Herb> herbs)
        {
            var lines = new List<Line>();
            var supplied = new long[8];
            for (int i = 0; i < mainIds.Count && i < mainCounts.Count; i++)
            {
                if (mainIds[i] == 0 || mainCounts[i] <= 0)
                {
                    continue;
                }
                lines.Add(new Line { Id = mainIds[i], Count = mainCounts[i] });
                int[] e = elementsOf(mainIds[i]) ?? new int[8];
                for (int j = 0; j < 8 && j < e.Length; j++)
                {
                    supplied[j] += (long)e[j] * mainCounts[i];
                }
            }
            for (int j = 0; j < 8 && j < need.Length; j++)
            {
                long deficit = need[j] - supplied[j];
                if (deficit <= 0)
                {
                    continue;
                }
                Herb best = null;
                foreach (Herb h in herbs)
                {
                    if (!Only(h.Elements, j))
                    {
                        continue;
                    }
                    if (best == null || h.Level < best.Level
                        || (h.Level == best.Level && (h.Elements[j] > best.Elements[j] || (h.Elements[j] == best.Elements[j] && h.Id < best.Id))))
                    {
                        best = h;
                    }
                }
                if (best == null)
                {
                    lines.Add(new Line { Note = ElementNames[j] + " " + deficit + " (채울 약재 없음)" });
                    continue;
                }
                lines.Add(new Line { Id = best.Id, Count = (int)((deficit + best.Elements[j] - 1) / best.Elements[j]) });
            }
            return lines;
        }

        private static bool Only(int[] elements, int index)
        {
            if (elements == null || elements.Length < 8 || elements[index] <= 0)
            {
                return false;
            }
            for (int i = 0; i < 8; i++)
            {
                if (i != index && elements[i] != 0)
                {
                    return false;
                }
            }
            return true;
        }

        // 쓸 수 있는(basRequire ≤ 자질) 단로 가운데 basRequire가 가장 낮은 것(같으면 번호가 작은 것). 없으면 0.
        public static int PickFurnace(IList<Candidate> furnaces, int skill)
        {
            Candidate best = null;
            foreach (Candidate f in furnaces)
            {
                if (f.Mixed > skill)
                {
                    continue;
                }
                if (best == null || f.Mixed < best.Mixed || (f.Mixed == best.Mixed && f.Id < best.Id))
                {
                    best = f;
                }
            }
            return best == null ? 0 : best.Id;
        }

        // 연기: 핵심 1 + 보조 재료 정확히 num개(종류 안에서 불순물이 가장 작은 것, 같으면 번호가 작은 것) + 가장 낮은 불씨 1.
        public static List<Line> ForgeSet(int core, int num, IList<int> types, IList<Candidate> materials, IList<Candidate> fires)
        {
            var lines = new List<Line> { new Line { Id = core, Count = 1 } };
            Candidate aux = null;
            foreach (Candidate m in materials)
            {
                if (!types.Contains(m.Class))
                {
                    continue;
                }
                if (aux == null || m.Mixed < aux.Mixed || (m.Mixed == aux.Mixed && m.Id < aux.Id))
                {
                    aux = m;
                }
            }
            lines.Add(aux == null ? new Line { Note = "(보조 재료를 찾지 못함)" } : new Line { Id = aux.Id, Count = num });
            Candidate fire = null;
            foreach (Candidate f in fires)
            {
                if (fire == null || f.Level < fire.Level || (f.Level == fire.Level && f.Id < fire.Id))
                {
                    fire = f;
                }
            }
            lines.Add(fire == null ? new Line { Note = "(불씨를 찾지 못함)" } : new Line { Id = fire.Id, Count = 1 });
            return lines;
        }

        // 부적: requier1("a|b")의 첫 번호 × count.
        public static List<Line> RuneSet(string requier1, int count)
        {
            var lines = new List<Line>();
            int id;
            string first = (requier1 ?? "").Split('|')[0];
            if (int.TryParse(first, out id) && id != 0 && count > 0)
            {
                lines.Add(new Line { Id = id, Count = count });
            }
            else
            {
                lines.Add(new Line { Note = "(부적 재료를 찾지 못함)" });
            }
            return lines;
        }

        public static List<Line> Times(List<Line> set, int k)
        {
            var result = new List<Line>();
            foreach (Line l in set)
            {
                result.Add(l.Id == 0 ? l : new Line { Id = l.Id, Count = (int)Math.Min(int.MaxValue, (long)l.Count * k), Note = l.Note });
            }
            return result;
        }

        // 겹쳐지는 재료는 한 칸에 겹친다(05 문서 61절 E5): 가방에 아직 없는 번호마다 한 칸.
        // 겹쳐지지 않는 재료(isOverlay 0, 예: 연기 핵심 재료)는 가방에 있든 없든 한 개가 한 칸(리뷰 Important 1, BagExportService.NeededSlots와 같은 규칙).
        public static int NewSlots(IEnumerable<Line> set, Func<int, bool> inBag, Func<int, bool> stacks)
        {
            var seen = new HashSet<int>();
            long slots = 0;
            foreach (Line l in set)
            {
                if (l.Id == 0)
                {
                    continue;
                }
                if (!stacks(l.Id))
                {
                    slots += l.Count;
                }
                else if (!inBag(l.Id) && seen.Add(l.Id))
                {
                    slots++;
                }
            }
            return (int)Math.Min(int.MaxValue, slots);
        }
    }
}
