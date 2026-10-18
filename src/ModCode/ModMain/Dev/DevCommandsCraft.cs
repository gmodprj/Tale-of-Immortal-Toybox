using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 개발 전용. 기예 제작 재료 지급 설계(docs/superpowers/specs/2026-10-18-craft-materials-design.md 4절)의 설계 전 실험 E1~E5.
    //   cmlearn <처방 아이템 번호>       연단·연기·부적 처방을 배운다(ArtService.ForceLearn)
    //   cmgive <아이템> <수>             아이템 지급(EditService.GiveItem)
    //   cmbag [아이템]                   가방의 칸 수와(아이템을 주면) 그 아이템의 줄: sole:수
    //   cmherb <약재>                    약재의 오행 여덟 값(ItemHerbal → RoleEffect)
    //   cmpill <단방>                    단방 표, 주약, 가방의 약재로 RefineElixirBase를 만들어 오행 합을 본다(단로는 가방의 첫 단로)
    //   cmforge <연기보> [types]         가방의 재료로 UnitActionPropArtifactShapeMade를 만들어 IsCreate(false). types=1이면 첫 종류만
    //   cmrune <도보> <단계>             부적 그리기 화면을 열고 AutoFillRequireProps 뒤의 재료와 고른 처방
    internal static class DevCommandsCraft
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "cmlearn":
                    return Learn(int.Parse(p[1]));
                case "cmgive":
                    return EditService.GiveItem(Me(), int.Parse(p[1]), int.Parse(p[2]));
                case "cmbag":
                    return Bag(p.Length > 1 ? int.Parse(p[1]) : 0);
                case "cmherb":
                    return string.Join(",", Elements(int.Parse(p[1])));
                case "cmpill":
                    return Pill(int.Parse(p[1]));
                case "cmforge":
                    return Forge(int.Parse(p[1]), p.Length > 2 && p[2] == "1");
                case "cmrune":
                    return Rune(int.Parse(p[1]), int.Parse(p[2]));
                default:
                    return null;
            }
        }

        private static WorldUnitBase Me()
        {
            return g.world == null ? null : g.world.playerUnit;
        }

        private static string Learn(int id)
        {
            ItemCatalog.Entry e = ItemCatalog.Get(id);
            if (e == null)
            {
                return "no item " + id;
            }
            foreach (ArtService.Kind k in ArtService.Kinds)
            {
                if (k.ItemClass == e.SubType)
                {
                    return ArtService.ForceLearn(k, e);
                }
            }
            return "not an art book: class " + e.SubType;
        }

        private static List<DataProps.PropsData> Props(Func<DataProps.PropsData, bool> pick)
        {
            var result = new List<DataProps.PropsData>();
            var all = Me().data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                if (pick(all[i]))
                {
                    result.Add(all[i]);
                }
            }
            return result;
        }

        private static int ClassOf(int propsId)
        {
            ItemCatalog.Entry e = ItemCatalog.Get(propsId);
            return e == null ? -1 : e.SubType;
        }

        private static string Bag(int id)
        {
            var data = Me().data.unitData.propData;
            var sb = new StringBuilder();
            sb.Append("slots=").Append(data.allProps.Count);
            if (id != 0)
            {
                foreach (DataProps.PropsData pd in Props(x => x.propsID == id))
                {
                    sb.Append(' ').Append(pd.soleID).Append(':').Append(pd.propsCount);
                }
            }
            return sb.ToString();
        }

        // 약재의 오행(화·수·뇌·풍·토·목·음·양으로 가정). RoleEffect의 effectType 102 값 element_elementNames_a_b_c_d_e_f_g_h.
        public static int[] Elements(int herbId)
        {
            var v = new int[8];
            ConfItemHerbalItem herb = g.conf.itemHerbal.GetItem(herbId);
            if (herb == null)
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
                if (row == null || row.effectType != 102)
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

        private static string Join(Il2CppSystem.Collections.Generic.List<int> list)
        {
            var parts = new List<string>();
            for (int i = 0; list != null && i < list.Count; i++)
            {
                parts.Add(list[i].ToString());
            }
            return string.Join("|", parts);
        }

        private static string Pill(int formulaId)
        {
            WorldUnitBase me = Me();
            ConfMakePillFormulaItem f = g.conf.makePillFormula.GetItem(formulaId);
            if (f == null)
            {
                return "no formula " + formulaId;
            }
            var sb = new StringBuilder();
            sb.Append("need=").Append(f.huo).Append(',').Append(f.shui).Append(',').Append(f.lei).Append(',').Append(f.feng).Append(',')
              .Append(f.tu).Append(',').Append(f.mu).Append(',').Append(f.yin).Append(',').Append(f.yan)
              .Append(" main=").Append(f.herbalRequier).Append(" x").Append(f.herbalRequierCount);
            DataWorld.World.PillFormulaData formula = null;
            var list = g.data.world.pillFormulas;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].id == formulaId)
                {
                    formula = list[i];
                }
            }
            sb.Append(" learned=").Append(formula != null);
            List<DataProps.PropsData> furnaces = Props(x => g.conf.makePillFurnace.GetItem(x.propsID) != null);
            sb.Append(" furnaces=").Append(furnaces.Count);
            var herbs = Props(x => g.conf.itemHerbal.GetItem(x.propsID) != null);
            var sum = new int[8];
            foreach (DataProps.PropsData h in herbs)
            {
                int[] e = Elements(h.propsID);
                for (int i = 0; i < 8; i++)
                {
                    sum[i] += e[i] * h.propsCount;
                }
                sb.Append(" herb ").Append(h.propsID).Append('x').Append(h.propsCount).Append('=').Append(string.Join(",", e));
            }
            sb.Append(" bagSum=").Append(string.Join(",", sum));
            if (formula == null || furnaces.Count == 0)
            {
                return sb.ToString();
            }
            try
            {
                var rb = new RefineElixirBase();
                rb.Init(me, furnaces[0].To<DataProps.PropsPillFurnaceData>(), formula);
                sb.Append(" base=").Append(string.Join(",", rb.elementBaseValue)).Append(" cur=").Append(string.Join(",", rb.curElementValue))
                  .Append(" req=").Append(Join(rb.herbalRequier)).Append(" reqN=").Append(Join(rb.herbalRequierCount)).Append(" site=").Append(rb.GetSiteNum());
                foreach (DataProps.PropsData h in herbs)
                {
                    rb.AddMasterial(h.soleID, h.propsCount);
                }
                sb.Append(" afterAdd cur=").Append(string.Join(",", rb.curElementValue)).Append(" mats=").Append(Join(rb.materialPropsID));
                rb.Destroy();
            }
            catch (Exception ex)
            {
                sb.Append(" ERROR ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }
            return sb.ToString();
        }

        private static string Forge(int shapeMaterialId, bool firstTypeOnly)
        {
            ConfArtifactShapeMaterialItem m = g.conf.artifactShapeMaterial.GetItem(shapeMaterialId);
            if (m == null)
            {
                return "no shape material " + shapeMaterialId;
            }
            var types = new List<int>();
            for (int i = 0; i < m.materialType.Length; i++)
            {
                types.Add(m.materialType[i]);
            }
            if (firstTypeOnly)
            {
                types.RemoveRange(1, types.Count - 1);
            }
            List<DataProps.PropsData> main = Props(x => x.propsID == m.coreMaterialID);
            List<DataProps.PropsData> rare = Props(x => x.propsID == m.rareMaterial);
            List<DataProps.PropsData> fire = Props(x => ClassOf(x.propsID) == 522);
            List<DataProps.PropsData> mats = Props(x => types.Contains(ClassOf(x.propsID)));
            var sb = new StringBuilder();
            int mixed = 0, count = 0;
            var il = new Il2CppSystem.Collections.Generic.List<DataProps.PropsData>();
            foreach (DataProps.PropsData pd in mats)
            {
                ConfArtifactShapeMixedItem mix = g.conf.artifactShapeMixed.GetItem(pd.propsID);
                mixed += (mix == null ? 0 : mix.mixed) * pd.propsCount;
                count += pd.propsCount;
                il.Add(pd);
            }
            sb.Append("need n=").Append(m.materialNum).Append(" types=").Append(string.Join("|", types)).Append(" mixed>=").Append(m.mixedRequire).Append(" pure ").Append(string.Join("|", m.pureType)).Append(">=").Append(m.pureRequire)
              .Append(" bas>=").Append(m.basEqpRequire).Append(" | have main=").Append(main.Count).Append(" rare=").Append(rare.Count).Append(" fire=").Append(fire.Count)
              .Append(" mats=").Append(mats.Count).Append(" kinds/").Append(count).Append(" items mixed=").Append(mixed);
            try
            {
                var action = new UnitActionPropArtifactShapeMade(main.Count > 0 ? main[0] : null, rare.Count > 0 ? rare[0] : null, il,
                    fire.Count > 0 ? fire[0] : null, m, true);
                action.unit = Me();
                sb.Append(" isCreate=").Append(Me().IsCreateAction(action, false));
            }
            catch (Exception ex)
            {
                sb.Append(" ERROR ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }
            return sb.ToString();
        }

        private static string Rune(int formulaId, int grade)
        {
            ConfRuneFormulaItem row = null;
            var all = g.conf.runeFormula._allConfList;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].formulaId == formulaId && all[i].grade == grade)
                {
                    row = all[i];
                }
            }
            if (row == null)
            {
                return "no rune formula " + formulaId + " grade " + grade;
            }
            var sb = new StringBuilder();
            sb.Append("row=").Append(row.id).Append(" group=").Append(row.group).Append(" r1=").Append(row.requier1).Append('x').Append(row.requier1Count)
              .Append(" r2=").Append(row.requier2).Append('x').Append(row.requier2Count).Append(" learned=").Append(g.data.world.allArtistrySkillID.Contains(formulaId));
            try
            {
                UIRuneSelectDraw ui = g.ui.OpenUI<UIRuneSelectDraw>(UIType.RuneSelectDraw);
                ui.InitData(row.group);
                ui.AutoFillRequireProps();
                sb.Append(" current=").Append(ui.currentFormula == null ? "-" : ui.currentFormula.id.ToString())
                  .Append(" filled=").Append(ui.propsMaterial == null ? -1 : ui.propsMaterial.Count);
                for (int i = 0; ui.propsMaterial != null && i < ui.propsMaterial.Count; i++)
                {
                    DataProps.PropsData pd = ui.propsMaterial[i];
                    sb.Append(' ').Append(pd == null ? "null" : pd.propsID + "x" + pd.propsCount);
                }
            }
            catch (Exception ex)
            {
                sb.Append(" ERROR ").Append(ex.GetType().Name).Append(": ").Append(ex.Message);
            }
            return sb.ToString();
        }
    }
}
