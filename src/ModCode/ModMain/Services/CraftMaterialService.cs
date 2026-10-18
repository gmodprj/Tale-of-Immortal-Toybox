using System;
using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 기예 제작 재료 지급(docs/superpowers/specs/2026-10-18-craft-materials-design.md, 05 문서 61·62절).
    // 게임 표를 읽어 처방마다 한 벌(CraftPlan)을 만들고, 가방 칸을 본 뒤 플레이어에게 지급한다. 기예 기록이 세계 자료라 플레이어만.
    internal static class CraftMaterialService
    {
        public sealed class Recipe
        {
            public int Kind;            // 0 연단 · 1 연기 · 2 부적
            public string Key;          // 요소 이름 꼬리: 연단·연기 = 처방 번호, 부적 = <도보>_<단계>
            public int BookId;
            public int Grade;
            public string Name;
            public bool Learned;
            public List<CraftPlan.Line> Set;
            public string Warning;      // 지급으로 채울 수 없는 인물 조건 안내. 없으면 null
            public string Detail;       // 아래 칸에 보일 계산(연단의 오행)
        }

        public static readonly string[] KindNames = { "연단", "연기", "부적" };
        private const int FireClass = 522;

        private static List<CraftPlan.Herb> herbs;

        private static WorldUnitBase Me()
        {
            return g.world == null ? null : g.world.playerUnit;
        }

        // 약재의 오행(화·수·뇌·풍·토·목·음·양): ItemHerbal.effectValue → RoleEffect(effectType 102)의 끝 여덟 수.
        public static int[] Elements(int herbId)
        {
            var v = new int[8];
            ConfItemHerbalItem herb = g.conf.itemHerbal.GetItem(herbId);
            if (herb == null || string.IsNullOrEmpty(herb.effectValue))
            {
                return v;
            }
            foreach (string effect in herb.effectValue.Split('|'))
            {
                int eid;
                if (!int.TryParse(effect, out eid))
                {
                    continue;
                }
                ConfRoleEffectItem row = g.conf.roleEffect.GetItem(eid);
                if (row == null || row.effectType != 102 || row.value == null)
                {
                    continue;
                }
                string[] parts = row.value.Split('_');
                for (int i = 0; i < 8 && parts.Length - 8 + i >= 0; i++)
                {
                    int n;
                    if (int.TryParse(parts[parts.Length - 8 + i], out n))
                    {
                        v[i] += n;
                    }
                }
            }
            return v;
        }

        private static List<CraftPlan.Herb> Herbs()
        {
            if (herbs == null)
            {
                herbs = new List<CraftPlan.Herb>();
                var rows = g.conf.itemHerbal._allConfList;
                for (int i = 0; i < rows.Count; i++)
                {
                    if (Hidden(rows[i].id))
                    {
                        continue;
                    }
                    ItemCatalog.Entry e = ItemCatalog.Get(rows[i].id);
                    herbs.Add(new CraftPlan.Herb { Id = rows[i].id, Level = e == null ? 99 : e.Level, Elements = Elements(rows[i].id) });
                }
            }
            return herbs;
        }

        private static List<int> Ints(string text)
        {
            var list = new List<int>();
            foreach (string s in (text ?? "").Split('|'))
            {
                int v;
                list.Add(int.TryParse(s, out v) ? v : 0);
            }
            return list;
        }

        // 게임이 숨긴 아이템(ItemProps.hide = 1. 예: 단로 표의 환령과 700203·700204, 요구 자질 0)은 후보에서 뺀다(05 문서 62절, mat-1018c).
        private static bool Hidden(int id)
        {
            ConfItemPropsItem item = g.conf.itemProps.GetItem(id);
            return item == null || item.hide == 1;
        }

        private static string NameOf(int id, string fallback)
        {
            ItemCatalog.Entry e = ItemCatalog.Get(id);
            return e != null && !string.IsNullOrEmpty(e.Name) ? e.Name : fallback;
        }

        public static List<Recipe> Recipes(int kind, bool learnedOnly, string query)
        {
            var all = kind == 0 ? Pills() : kind == 1 ? Forges() : Runes();
            string q = EditService.Compact(query ?? "");
            return all.FindAll(r => (!learnedOnly || r.Learned)
                && (q.Length == 0 || EditService.Compact(r.Name).Contains(q) || r.Key.Contains(q)));
        }

        private static List<Recipe> Pills()
        {
            var result = new List<Recipe>();
            var learned = new HashSet<int>();
            var known = g.data.world.pillFormulas;
            for (int i = 0; i < known.Count; i++)
            {
                learned.Add(known[i].id);
            }
            var rows = g.conf.makePillFormula._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfMakePillFormulaItem f = rows[i];
                int[] need = { f.huo, f.shui, f.lei, f.feng, f.tu, f.mu, f.yin, f.yan };
                List<int> mains = Ints(f.herbalRequier), counts = Ints(f.herbalRequierCount);
                result.Add(new Recipe
                {
                    Kind = 0,
                    Key = f.id.ToString(),
                    BookId = f.id,
                    Grade = f.grade,
                    Name = NameOf(f.id, f.id.ToString()),
                    Learned = learned.Contains(f.id),
                    Set = CraftPlan.PillSet(need, mains, counts, Elements, Herbs()),
                    Detail = "요구 오행 " + Need(need),
                });
            }
            return result;
        }

        private static string Need(int[] need)
        {
            var parts = new List<string>();
            for (int i = 0; i < 8; i++)
            {
                if (need[i] > 0)
                {
                    parts.Add(CraftPlan.ElementNames[i] + " " + need[i]);
                }
            }
            return parts.Count == 0 ? "없음" : string.Join(", ", parts);
        }

        private static List<Recipe> Forges()
        {
            var result = new List<Recipe>();
            var mats = new List<CraftPlan.Candidate>();
            var mixed = g.conf.artifactShapeMixed._allConfList;
            for (int i = 0; i < mixed.Count; i++)
            {
                ItemCatalog.Entry e = ItemCatalog.Get(mixed[i].id);
                if (e != null && !Hidden(mixed[i].id))
                {
                    mats.Add(new CraftPlan.Candidate { Id = mixed[i].id, Class = e.SubType, Level = e.Level, Mixed = mixed[i].mixed });
                }
            }
            var fires = new List<CraftPlan.Candidate>();
            var fireRows = g.conf.artifactShapeFire._allConfList;
            for (int i = 0; i < fireRows.Count; i++)
            {
                ItemCatalog.Entry e = ItemCatalog.Get(fireRows[i].id);
                if (e != null && e.SubType == FireClass && !Hidden(fireRows[i].id))
                {
                    fires.Add(new CraftPlan.Candidate { Id = fireRows[i].id, Class = e.SubType, Level = e.Level });
                }
            }
            int skill = Skill(false);
            var rows = g.conf.artifactShapeMaterial._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfArtifactShapeMaterialItem m = rows[i];
                var types = new List<int>();
                for (int t = 0; m.materialType != null && t < m.materialType.Length; t++)
                {
                    types.Add(m.materialType[t]);
                }
                ItemCatalog.Entry book = ItemCatalog.Get(m.id);
                result.Add(new Recipe
                {
                    Kind = 1,
                    Key = m.id.ToString(),
                    BookId = m.id,
                    Grade = book == null ? 0 : book.Level,
                    Name = NameOf(m.id, m.id.ToString()),
                    Learned = g.data.world.allArtistrySkillID.Contains(m.id),
                    Set = CraftPlan.ForgeSet(m.coreMaterialID, m.materialNum, types, mats, fires),
                    Warning = skill >= 0 && skill < m.basEqpRequire ? "연기 자질 " + m.basEqpRequire + " 이상이 필요합니다(지금 " + skill + "). 능력치 탭에서 바꿀 수 있습니다." : null,
                    Detail = "보조 재료 정확히 " + m.materialNum + "개(불순물이 작은 재료), 연기 자질 " + m.basEqpRequire + " 이상",
                });
            }
            return result;
        }

        private static List<Recipe> Runes()
        {
            var result = new List<Recipe>();
            var rows = g.conf.runeFormula._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfRuneFormulaItem r = rows[i];
                result.Add(new Recipe
                {
                    Kind = 2,
                    Key = r.formulaId + "_" + r.grade,
                    BookId = r.formulaId,
                    Grade = r.grade,
                    Name = NameOf(r.formulaId, r.formulaId.ToString()) + " " + r.grade + "단계",
                    Learned = g.data.world.allArtistrySkillID.Contains(r.formulaId),
                    Set = CraftPlan.RuneSet(r.requier1, r.requier1Count),
                    Detail = "게임의 부적 화면이 가방의 재료를 자동으로 채운다",
                });
            }
            return result;
        }

        // 화면에 보이는 연단(true)·연기(false) 자질. 읽지 못하면 -1.
        private static int Skill(bool elixir)
        {
            try
            {
                WorldUnitBase me = Me();
                return elixir ? me.data.dynUnitData.refineElixir.value : me.data.dynUnitData.refineWeapon.value;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static string Summary(Recipe r)
        {
            var parts = new List<string>();
            foreach (CraftPlan.Line l in r.Set)
            {
                parts.Add(l.Id == 0 ? l.Note : (EditService.ItemName(l.Id) ?? l.Id.ToString()) + " x" + l.Count);
            }
            if (r.Kind == 0)
            {
                parts.Add("단로 1(쓸 단로가 없을 때)");
            }
            return string.Join(", ", parts);
        }

        private static List<CraftPlan.Candidate> Furnaces()
        {
            var list = new List<CraftPlan.Candidate>();
            var rows = g.conf.makePillFurnace._allConfList;
            for (int i = 0; i < rows.Count; i++)
            {
                if (Hidden(rows[i].id))
                {
                    continue;
                }
                list.Add(new CraftPlan.Candidate { Id = rows[i].id, Mixed = rows[i].basRequire });
            }
            return list;
        }

        public static string Give(Recipe r, int k)
        {
            WorldUnitBase me = Me();
            if (me == null)
            {
                return "기예 재료: 세계에 들어간 뒤에 쓸 수 있습니다.";
            }
            var data = me.data.unitData.propData;
            var bag = new HashSet<int>();
            for (int i = 0; i < data.allProps.Count; i++)
            {
                bag.Add(data.allProps[i].propsID);
            }
            List<CraftPlan.Line> lines = CraftPlan.Times(r.Set, k);
            string furnaceNote = null;
            if (r.Kind == 0)
            {
                int skill = Skill(true);
                bool usable = false;
                for (int i = 0; i < data.allProps.Count && !usable; i++)
                {
                    ConfMakePillFurnaceItem f = g.conf.makePillFurnace.GetItem(data.allProps[i].propsID);
                    usable = f != null && f.basRequire <= skill;
                }
                if (!usable)
                {
                    int furnace = CraftPlan.PickFurnace(Furnaces(), skill);
                    if (furnace != 0)
                    {
                        lines.Add(new CraftPlan.Line { Id = furnace, Count = 1 });
                    }
                    else
                    {
                        furnaceNote = "연단 자질이 모자라 쓸 단로가 없습니다(지금 " + skill + ").";
                    }
                }
            }
            int need = CraftPlan.NewSlots(lines, id => bag.Contains(id));
            int free = data.GetGridMaxNum() - data.GetGridCurNum();
            string head = KindNames[r.Kind] + " " + r.Name + ": ";
            if (need > free)
            {
                return head + "가방 칸 " + (need - free) + "개가 더 있어야 합니다(자원 탭의 가방 칸 단추).";
            }
            int kinds = 0, items = 0;
            var failed = new List<string>();
            foreach (CraftPlan.Line l in lines)
            {
                if (l.Id == 0)
                {
                    continue;
                }
                string text;
                if (EditService.TryGiveItem(me, l.Id, l.Count, out text))
                {
                    kinds++;
                    items += l.Count;
                }
                else
                {
                    failed.Add((EditService.ItemName(l.Id) ?? l.Id.ToString()) + " " + l.Count);
                }
            }
            ChangeLog.Add(EditService.NameOf(me), "기예 재료", r.Name, kinds + "종 " + items + "개 × " + k, failed.Count == 0 && kinds > 0);
            var notes = new List<string>();
            foreach (CraftPlan.Line l in lines)
            {
                if (l.Id == 0)
                {
                    notes.Add(l.Note);
                }
            }
            return head + "재료 " + kinds + "종 " + items + "개를 넣었습니다(묶음 " + k + ")."
                + (failed.Count > 0 ? " 넣지 못함: " + string.Join(", ", failed) + "." : "")
                + (notes.Count > 0 ? " " + string.Join(" ", notes) : "")
                + (furnaceNote != null ? " " + furnaceNote : "")
                + (r.Warning != null ? " " + r.Warning : "")
                + (r.Learned ? "" : " 이 처방은 아직 배우지 않았습니다(기예 탭에서 배울 수 있습니다).");
        }
    }
}
