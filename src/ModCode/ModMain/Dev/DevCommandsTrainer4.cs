using System;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 트레이너 4차 전투 묶음(스펙 2026-10-28-trainer4-battle-design.md, 05 문서 87절). 검사 trainer4-check.ps1이 쓴다.
    //   cdall                   전투 플레이어의 쿨타임: 단위 비율(skill/step/ult/item), 선법(immortalSkill)마다 cd·cdTime·energy·useCount,
    //                           영역 cd·cdTime·dp, 전투 아이템 칸마다 형식·cd·cdTime, 명우 기술 cd·cdTime
    //   icast <i>               선법 i를 IsCreate(false) 뒤 Create
    //   fcast                   영역 기술을 IsCreate(false) 뒤 Create
    //   iequip <카드> <칸>      선법 카드를 익히고 ImmortalCard.EquipCard(카드, 주동, 칸)로 장착(검사 준비. 데이터 장착은 게임 선법 창을 깨뜨린 기록이 있어 회차 안에서만)
    //   fieldset <main1> <dp>   unitInfo.fieldSkillData.mainType1과 dp·dpMax를 쓴다(영역 넣기, 검사 준비)
    //   bhp                     일격으로 마지막에 맞은 단위의 hp·die, 적 단위의 산 수·죽은 수
    //   artdur                  플레이어 법보마다 sole·내구, 내구 접두가 불린 수·막은 수
    internal static class DevCommandsTrainer4
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "cdall":
                    return CdAll();
                case "icast":
                    return ImmortalCast(int.Parse(p[1]));
                case "fcast":
                    return FieldCast();
                case "iequip":
                    {
                        WorldUnitBase me = g.world.playerUnit;
                        int card = int.Parse(p[1]);
                        ImmortalService.Entry entry = ImmortalService.All().Find(e => e.CardId == card);
                        string learn = entry == null ? "no card" : ImmortalService.Learn(me, entry);
                        bool ok = me.data.unitData.immortalCard.EquipCard(card, false, int.Parse(p[2]));
                        return learn + " | EquipCard=" + ok + " grade=" + me.data.unitData.propertyData.gradeID;
                    }
                case "fieldset":
                    {
                        var fd = g.world.playerUnit.data.unitData.fieldSkillData;
                        if (fd == null)
                        {
                            return "fieldSkillData null";
                        }
                        fd.mainType1 = int.Parse(p[1]);
                        fd.dpMax = Math.Max(fd.dpMax, int.Parse(p[2]));
                        fd.dp = int.Parse(p[2]);
                        return "main1=" + fd.mainType1 + " dp=" + fd.dp + "/" + fd.dpMax + " IsFieldSkill=" + fd.IsFieldSkill();
                    }
                case "bhp":
                    return Hp();
                case "artdur":
                    return Durable();
                default:
                    return null;
            }
        }

        private static UnitCtrlPlayer Ctrl
        {
            get { return RateService.InBattle ? SceneType.battle.battleMap.playerUnitCtrl : null; }
        }

        private static string Dyn(DynInt d)
        {
            return d == null ? "-" : d.baseValue + "/" + d.value;
        }

        private static string F(float v)
        {
            return v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
        }

        private static string CdAll()
        {
            UnitCtrlPlayer u = Ctrl;
            if (u == null)
            {
                return "not in battle";
            }
            var d = u.data;
            var sb = new StringBuilder();
            sb.Append("time=").Append(F(Time.time)).Append(" rates skill=").Append(Dyn(d.skillCDRate)).Append(" step=").Append(Dyn(d.stepCDRate))
              .Append(" ult=").Append(Dyn(d.ultimateCDRate)).Append(" item=").Append(Dyn(d.itemCDRate)).Append('\n');
            for (int i = 0; u.immortalSkill != null && i < u.immortalSkill.Count; i++)
            {
                ImmortalSkillDataBase sd = u.immortalSkill[i] == null ? null : u.immortalSkill[i].data;
                if (sd != null)
                {
                    sb.Append("imm").Append(i).Append(" id=").Append(sd.immortalSkillItem == null ? 0 : sd.immortalSkillItem.id).Append(" cd=").Append(Dyn(sd.cd))
                      .Append(" cdTime=").Append(F(sd.cdTime)).Append(" energy=").Append(F(sd.energy)).Append(" useCount=").Append(sd.useCount).Append('\n');
                }
            }
            if (u.fieldSkill != null && u.fieldSkill.data != null)
            {
                FieldSkillDataBase f = u.fieldSkill.data;
                sb.Append("field cd=").Append(Dyn(f.cd)).Append(" cdTime=").Append(F(f.cdTime)).Append(" last=").Append(F(f.lastShotTime)).Append(" dp=").Append(d.dp).Append('\n');
            }
            for (int i = 0; u.props != null && i < u.props.Count; i++)
            {
                PropItemBase pi = u.props[i];
                PropItemDataBase pd = pi == null ? null : pi.data;
                if (pd != null)
                {
                    sb.Append("prop").Append(i).Append(' ').Append(pi.GetIl2CppType().Name).Append(" id=").Append(pd.propsData == null ? 0 : pd.propsData.propsID)
                      .Append(" cd=").Append(Dyn(pd.cd)).Append(" cdTime=").Append(F(pd.cdTime)).Append('\n');
                }
            }
            WingManBase w = u.wingman;
            for (int i = 0; w != null && w.wingManList != null && i < w.wingManList.Count; i++)
            {
                SkillAttack sk = w.wingManList[i].GetSkillData(u);
                SkillDataAttack sd = sk == null || sk.data == null ? null : sk.data.TryCast<SkillDataAttack>();
                if (sd != null)
                {
                    sb.Append("wing").Append(i).Append(" skill=").Append(sd.skillID).Append(" cd=").Append(Dyn(sd.cd)).Append(" cdTime=").Append(F(sd.cdTime)).Append('\n');
                }
            }
            return sb.ToString().TrimEnd('\n');
        }

        private static string ImmortalCast(int i)
        {
            UnitCtrlPlayer u = Ctrl;
            if (u == null || u.immortalSkill == null || i >= u.immortalSkill.Count)
            {
                return "no immortal " + i;
            }
            ImmortalSkillBase s = u.immortalSkill[i];
            bool can = s.IsCreate(false);
            if (can)
            {
                s.Create(u.posiCenter.position, Vector2.right, null);
            }
            return "IsCreate=" + can + " cdTime=" + F(s.data.cdTime) + " energy=" + F(s.data.energy);
        }

        private static string FieldCast()
        {
            UnitCtrlPlayer u = Ctrl;
            if (u == null || u.fieldSkill == null)
            {
                return "no field";
            }
            bool can = u.fieldSkill.IsCreate(false);
            if (can)
            {
                u.fieldSkill.Create(u.posiCenter.position, Vector2.right);
            }
            return "IsCreate=" + can + " cdTime=" + F(u.fieldSkill.data.cdTime) + " dp=" + u.data.dp;
        }

        private static string Hp()
        {
            if (!RateService.InBattle)
            {
                return "not in battle";
            }
            var sb = new StringBuilder();
            UnitCtrlBase t = BattleService.DevKillTarget;
            sb.Append("target=").Append(t == null ? "none" : "hp=" + (t.data == null ? -1 : t.data.hp) + " die=" + t.isDie);
            int alive = 0, dead = 0;
            var all = SceneType.battle.unit.allUnitIncludeDie;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] != null && all[i].data != null && all[i].data.unitType != UnitType.Player)
                {
                    if (all[i].isDie)
                    {
                        dead++;
                    }
                    else
                    {
                        alive++;
                    }
                }
            }
            UnitCtrlPlayer me = Ctrl;
            sb.Append(" alive=").Append(alive).Append(" dead=").Append(dead).Append(" playerHp=").Append(me == null ? -1 : me.data.hp);
            return sb.ToString();
        }

        private static string Durable()
        {
            WorldUnitBase me = g.world.playerUnit;
            var sb = new StringBuilder();
            var all = me.data.unitData.propData.allProps;
            for (int i = 0; i < all.Count; i++)
            {
                DataProps.PropsData pd = all[i];
                if (pd == null || !SpriteService.IsArtifact(pd.propsID))
                {
                    continue;
                }
                DataProps.PropsArtifact art = pd.To<DataProps.PropsArtifact>();
                sb.Append(pd.soleID).Append(":dur=").Append(art == null ? -1 : art.durable).Append(' ');
            }
            sb.Append("calls=").Append(RateGuards.DurableCalls).Append(" blocked=").Append(RateGuards.DurableBlockedCount);
            return sb.ToString();
        }
    }
}
