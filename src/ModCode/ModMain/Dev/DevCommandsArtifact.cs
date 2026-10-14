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
                case "bwpos":
                    {
                        // 전투 화면 신기 칸의 자리(부모 기준 x,y). 꺼진 칸은 off.
                        UIBattleInfo ui = g.ui.GetUI<UIBattleInfo>(UIType.BattleInfo);
                        if (ui == null || ui.info == null)
                        {
                            return "battle=null";
                        }
                        return "pisces=" + Slot(ui.info.goPiscesPendant) + " godEye=" + Slot(ui.info.goGodEye) + " devil=" + Slot(ui.info.goDevilDemon);
                    }
                case "pigive":
                    {
                        // 검사 준비: 쌍어패 반쪽을 가장 가까운 NPC 목록의 첫 사람에게 준 것으로 기록한다(전투의 쌍어패 칸이 켜진다).
                        var units = g.world.unit.GetUnits(false);
                        string me = g.world.playerUnit.data.unitData.unitID;
                        for (int i = 0; i < units.Count; i++)
                        {
                            if (units[i].data.unitData.unitID != me)
                            {
                                g.data.world.piscesData.GivePisces(units[i]);
                                return "given=" + units[i].data.unitData.unitID + " IsGive=" + g.data.world.piscesData.IsGive;
                            }
                        }
                        return "no npc";
                    }
                case "awreset":
                    {
                        // 검사용: 게임이 자리를 되돌린 것처럼 지도 화면 신기 버튼 셋을 게임의 자리(-240,-93)로 옮긴다.
                        UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                        if (map == null || map.playerInfo == null)
                        {
                            return "map=null";
                        }
                        foreach (UnityEngine.UI.Button b in new[] { map.playerInfo.btnPiscesPendant, map.playerInfo.btnGodEye, map.playerInfo.btnDevilDemon })
                        {
                            b.GetComponent<UnityEngine.RectTransform>().anchoredPosition = new UnityEngine.Vector2(-240f, -93f);
                        }
                        return "reset";
                    }
                case "awpos":
                    {
                        // 지도 화면 신기 버튼의 자리(부모 기준 x,y)와 게임 좌표(1920x1080, 왼쪽 위 기준)의 가운데.
                        UIMapMain map = g.ui.GetUI<UIMapMain>(UIType.MapMain);
                        if (map == null || map.playerInfo == null)
                        {
                            return "map=null";
                        }
                        return "pisces=" + Pos(map.playerInfo.btnPiscesPendant) + " godEye=" + Pos(map.playerInfo.btnGodEye) + " devil=" + Pos(map.playerInfo.btnDevilDemon);
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

        private static string Pos(UnityEngine.UI.Button b)
        {
            if (b == null || !b.gameObject.activeInHierarchy)
            {
                return "off";
            }
            var rect = b.GetComponent<UnityEngine.RectTransform>();
            UnityEngine.Canvas canvas = b.GetComponentInParent<UnityEngine.Canvas>();
            UnityEngine.Camera cam = canvas == null || canvas.renderMode == UnityEngine.RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            UnityEngine.Rect r = rect.rect;
            UnityEngine.Vector2 screen = UnityEngine.RectTransformUtility.WorldToScreenPoint(cam, rect.TransformPoint(new UnityEngine.Vector3(r.center.x, r.center.y, 0f)));
            return UnityEngine.Mathf.RoundToInt(rect.anchoredPosition.x) + "," + UnityEngine.Mathf.RoundToInt(rect.anchoredPosition.y)
                + "@" + UnityEngine.Mathf.RoundToInt(screen.x) + "," + UnityEngine.Mathf.RoundToInt(UnityEngine.Screen.height - screen.y);
        }

        private static string Slot(UnityEngine.GameObject go)
        {
            if (go == null || !go.activeInHierarchy)
            {
                return "off";
            }
            var rect = go.GetComponent<UnityEngine.RectTransform>();
            return UnityEngine.Mathf.RoundToInt(rect.anchoredPosition.x) + "," + UnityEngine.Mathf.RoundToInt(rect.anchoredPosition.y);
        }

        private static string On(UnityEngine.UI.Button b)
        {
            return b == null ? "null" : b.gameObject.activeSelf ? "on" : "off";
        }
    }
}
