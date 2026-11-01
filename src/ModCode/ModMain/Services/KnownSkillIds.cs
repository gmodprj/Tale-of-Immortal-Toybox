using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 기술 본표 셋(공격 기술·신법·심법)에 있는 번호들(05 문서 97.8절). 게임 형식 없이 판정만 한다.
    // 본표 행 수가 바뀌면(모드 표가 더 실리면) 다시 채우도록 행 수를 함께 기억한다.
    internal sealed class KnownSkillIds
    {
        public const int Attack = 0;
        public const int Step = 1;
        public const int Ability = 2;

        private readonly HashSet<int>[] ids = { new HashSet<int>(), new HashSet<int>(), new HashSet<int>() };
        private readonly int[] counts = { -1, -1, -1 };

        // 그 표의 지금 행 수와 기억한 행 수가 다르면 다시 채워야 한다.
        public bool Stale(int table, int rowCount)
        {
            return counts[table] != rowCount;
        }

        public void Fill(int table, IEnumerable<int> rowIds, int rowCount)
        {
            ids[table].Clear();
            foreach (int id in rowIds)
            {
                ids[table].Add(id);
            }
            counts[table] = rowCount;
        }

        // 본표가 비었으면(아직 안 실림) 막지 않는다. 그 밖에는 번호가 없을 때만 막는다.
        public bool Missing(int table, int id)
        {
            return ids[table].Count > 0 && !ids[table].Contains(id);
        }
    }
}
