using System;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;

namespace MOD_sWP6UL
{
    // 기술 학습 상한(기술 칸) 늘리기(05 문서 60절).
    // 게임은 종류(무예·영술 / 절기 / 신법 / 심법 / 신통)마다 경지에 따른 칸 수를 설정 표 RoleMartialGradeGrid(5~22칸, 심법은 10~44칸)에서 읽는다.
    // ConfRoleMartialGradeGrid.GetMaxGrid(경지, 종류)에 후위 패치를 걸어 고른 수만큼 더한다. 이 함수는 인물을 받지 않으므로 NPC에게도 같이 걸린다.
    // 고른 값은 데이터 폴더의 martial-grid.txt에 남아 다음 실행에도 쓰인다(세이브와 무관).
    internal static class MartialGridService
    {
        // 단추의 단계. 마지막이 "해제"(사실상 무제한).
        public static readonly int[] Steps = { 0, 10, 50, Unlimited };
        public const int Unlimited = 200;

        public static int Bonus { get; private set; }
        public static string Failure { get; private set; }
        private static HarmonyLib.Harmony harmony;
        private static bool loaded;

        private static string FilePath
        {
            get { return Path.Combine(ChangeLog.DataDir, "martial-grid.txt"); }
        }

        public static string StepName(int bonus)
        {
            return bonus == 0 ? "기본" : bonus >= Unlimited ? "해제" : "+" + bonus;
        }

        // Init마다 부른다. 파일을 한 번 읽고, 0이 아니면 패치를 건다.
        public static void Ensure()
        {
            if (!loaded)
            {
                loaded = true;
                Bonus = Read();
            }
            if (Bonus != 0)
            {
                Patch();
            }
        }

        public static string Set(int bonus)
        {
            if (Array.IndexOf(Steps, bonus) < 0)
            {
                return "학습 상한: 고를 수 없는 값입니다(" + bonus + ").";
            }
            int before = Bonus;
            if (bonus == before)
            {
                return "학습 상한: 이미 " + StepName(bonus) + "입니다.";
            }
            if (bonus != 0)
            {
                string failure = Patch();
                if (failure != null)
                {
                    return "학습 상한: " + failure;
                }
            }
            Bonus = bonus;
            string saveError = Write(bonus);
            // 세이브에 쓰지 않는 설정이라 백업은 하지 않는다(배율과 같다). 재조회는 게임의 함수로 한다.
            int now = SampleGrid();
            ChangeLog.Add("(설정)", "학습 상한", StepName(before), StepName(bonus) + (now >= 0 ? "(결정 무예 " + now + "칸)" : ""), now < 0 || now == BaseGrid() + bonus);
            return "학습 상한: " + StepName(bonus) + "(" + (bonus == 0 ? "게임 기본 칸 수" : "종류마다 칸 " + (bonus >= Unlimited ? "사실상 무제한" : bonus + "개 더")) + "). NPC에게도 적용됩니다."
                + (now >= 0 ? " 지금 경지의 무예 칸: " + now + "." : "")
                + (saveError != null ? " 설정 파일에 저장하지 못했습니다(" + saveError + "). 다음 실행에는 기본으로 돌아갑니다." : "");
        }

        // 후위 패치: 게임이 계산한 칸 수에 Bonus를 더한다. 예외는 게임으로 번지지 않게 막는다.
        public static void GridPostfix(ref int __result)
        {
            try
            {
                if (Bonus > 0)
                {
                    __result += Bonus;
                }
            }
            catch (Exception)
            {
            }
        }

        private static string Patch()
        {
            if (harmony != null)
            {
                return null;
            }
            var instance = new HarmonyLib.Harmony("TlToybox.martialgrid");
            try
            {
                MethodInfo target = typeof(ConfRoleMartialGradeGrid).GetMethod("GetMaxGrid", BindingFlags.Instance | BindingFlags.Public);
                MethodInfo postfix = typeof(MartialGridService).GetMethod("GridPostfix", BindingFlags.Static | BindingFlags.Public);
                instance.Patch(target, postfix: new HarmonyLib.HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                instance.UnpatchSelf();
                Failure = "게임에 연결하지 못했습니다(" + e.GetType().Name + ": " + e.Message + ")";
                MelonLogger.Msg("[TlToybox] 학습 상한 " + Failure);
                return Failure;
            }
            harmony = instance;
            Failure = null;
            return null;
        }

        // 확인용: 플레이어의 지금 경지에서 무예·영술(종류 1)의 칸 수. 세계 밖이면 -1.
        public static int SampleGrid()
        {
            try
            {
                if (g.world == null || g.world.playerUnit == null)
                {
                    return -1;
                }
                int grade = g.conf.roleGrade.GetItem(g.world.playerUnit.data.unitData.propertyData.gradeID).grade;
                return g.conf.roleMartialGradeGrid.GetMaxGrid(grade, MartialType.SkillLeft);
            }
            catch (Exception)
            {
                return -1;
            }
        }

        // 패치 없이 표에서 읽은 값(위와 같은 경지·종류).
        private static int BaseGrid()
        {
            try
            {
                int grade = g.conf.roleGrade.GetItem(g.world.playerUnit.data.unitData.propertyData.gradeID).grade;
                var rows = g.conf.roleMartialGradeGrid._allConfList;
                for (int i = 0; i < rows.Count; i++)
                {
                    ConfRoleMartialGradeGridItem row = rows[i];
                    if (row.type == (int)MartialType.SkillLeft && row.grade == grade)
                    {
                        return row.grid;
                    }
                }
            }
            catch (Exception)
            {
            }
            return -1000;
        }

        private static int Read()
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    int v;
                    if (int.TryParse(File.ReadAllText(FilePath, Encoding.UTF8).Trim(), out v) && Array.IndexOf(Steps, v) >= 0)
                    {
                        return v;
                    }
                }
            }
            catch (IOException e)
            {
                MelonLogger.Msg("[TlToybox] 학습 상한 설정을 읽지 못했습니다: " + e.Message);
            }
            catch (UnauthorizedAccessException e)
            {
                MelonLogger.Msg("[TlToybox] 학습 상한 설정을 읽지 못했습니다: " + e.Message);
            }
            return 0;
        }

        private static string Write(int bonus)
        {
            try
            {
                AtomicFile.WriteAllText(FilePath, bonus.ToString(), Encoding.UTF8);
                return null;
            }
            catch (IOException e)
            {
                return e.Message;
            }
            catch (UnauthorizedAccessException e)
            {
                return e.Message;
            }
        }
    }
}
