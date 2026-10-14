using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 신기 탭의 검증용 명령(tools/research/artifact-check.ps1).
    internal static class DevCommandsArtifact
    {
        public static string Execute(string[] p)
        {
            if (g.world == null || g.world.playerUnit == null || !ArtifactService.Ready)
            {
                return null;
            }
            switch (p[0])
            {
                case "aw":
                    return "weapons=" + ArtifactService.Listed() + " point=" + ArtifactService.Point;
                case "awset":
                    {
                        // 검사 준비: 목록을 그대로 바꾼다(서비스의 보호를 거치지 않는다).
                        var list = g.data.world.animaWeapons;
                        list.Clear();
                        foreach (string s in p[1].Split(','))
                        {
                            list.Add((GameAnimaWeapon)int.Parse(s));
                        }
                        return "weapons=" + ArtifactService.Listed();
                    }
                case "awbtn":
                    {
                        // 지도 화면의 신기 버튼이 켜져 있는지.
                        UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                        if (map == null || map.playerInfo == null)
                        {
                            return "map=null";
                        }
                        return "pisces=" + On(map.playerInfo.btnPiscesPendant) + " godEye=" + On(map.playerInfo.btnGodEye) + " devil=" + On(map.playerInfo.btnDevilDemon);
                    }
                case "aon": return ArtifactService.Enable(int.Parse(p[1]));
                case "aoff": return ArtifactService.Disable(int.Parse(p[1]));
                case "apoint": return ArtifactService.SetPoint(int.Parse(p[1]));
                case "gedmg":
                    {
                        var e = g.data.world.godEyeData;
                        e.isDamage = p[1] == "1";
                        e.repairMonth = 0;
                        return GeState();
                    }
                case "gestate": return GeState();
                case "gerepair": return ArtifactService.RepairGodEye();
                case "gecat":
                    {
                        var bosses = GodEyeService.Bosses();
                        return "bosses=" + bosses.Count + " skills=" + GodEyeService.SkillCount() + " first=" + (bosses.Count == 0 ? "" : bosses[0].Name.Replace(' ', '_'));
                    }
                case "geunlock": return GodEyeService.Unlock(GodEyeService.FindBoss(int.Parse(p[1])));
                case "geunlockall": return GodEyeService.UnlockAll(GodEyeService.Bosses());
                case "geboss":
                    {
                        GodEyeService.Boss b = GodEyeService.FindBoss(int.Parse(p[1]));
                        return b == null ? "no boss" : "grade=" + GodEyeService.Grade(b) + " learned=" + GodEyeService.LearnedCount(b) + "/" + b.Skills.Count;
                    }
                case "geequip": return GodEyeService.Equip(GodEyeService.FindSkill(int.Parse(p[1])));
                case "ddmg":
                    {
                        // 0 정상, 1 손상, 2 수리 중(지금 달 + 3에 끝남), 3 끝난 수리(지난 달). 관리자와 세계 자료 둘 다.
                        int mode = int.Parse(p[1]);
                        int month = g.world.run.roundMonth;
                        bool damage = mode == 1;
                        int repair = mode == 2 ? month + 3 : mode == 3 ? Math.Max(1, month - 1) : 0;
                        if (g.world.devilDemonMgr != null && g.world.devilDemonMgr.devilDemonData != null)
                        {
                            g.world.devilDemonMgr.devilDemonData.isDamage = damage;
                            g.world.devilDemonMgr.devilDemonData.repairMonth = repair;
                        }
                        g.data.world.devilDemonData.isDamage = damage;
                        g.data.world.devilDemonData.repairMonth = repair;
                        return DdState();
                    }
                case "ddstate": return DdState();
                case "ddrepair": return ArtifactService.RepairDevil();
                case "potlist":
                    {
                        List<PotmonService.Pet> pets = PotmonService.Owned();
                        var sb = new StringBuilder("count=" + pets.Count);
                        if (pets.Count > 0)
                        {
                            sb.Append(" first=").Append(pets[0].SoleId).Append(" grade=").Append(pets[0].Grade).Append(" exp=").Append(pets[0].Exp).Append(" next=").Append(pets[0].Next);
                        }
                        return sb.ToString();
                    }
                case "potadd": return PotmonService.Add(PotmonService.FindKind(int.Parse(p[1])), int.Parse(p[2]));
                case "potfill": return PotmonService.FillExp(p[1]);
                default: return null;
            }
        }

        private static string GeState()
        {
            var e = g.data.world.godEyeData;
            return "damaged=" + ArtifactService.GodEyeDamaged + " canUse=" + e.IsCanUse() + " repairMonth=" + e.repairMonth
                + " bosses=" + (e.bossGrade == null ? -1 : e.bossGrade.Count) + " skills=" + (e.skillsID == null ? -1 : e.skillsID.Count) + " equip=" + e.equipSkillID;
        }

        private static string DdState()
        {
            DevilDemonMgr mgr = g.world.devilDemonMgr;
            int repair = Math.Max(mgr == null || mgr.devilDemonData == null ? 0 : mgr.devilDemonData.repairMonth, g.data.world.devilDemonData.repairMonth);
            return "damaged=" + ArtifactService.DevilDamaged + " canUse=" + (mgr != null && mgr.IsCanUse()) + " repairMonth=" + repair + " month=" + g.world.run.roundMonth;
        }

        private static string On(UnityEngine.UI.Button b)
        {
            return b == null ? "null" : b.gameObject.activeSelf ? "on" : "off";
        }
    }
}
