using System.Collections.Generic;

namespace MOD_sWP6UL
{
    // 기예(연단·연기·부적·풍수·약재·광재). 게임에서는 서적 아이템을 배우면 목록에 들어간다(docs/05 22절).
    // 기록이 세계 데이터에 있어 플레이어에게만 해당한다.
    internal static class ArtService
    {
        public class Kind
        {
            public string Tab;      // 게임 기예 화면의 탭 이름
            public string Book;     // 배우는 서적의 종류
            public int ItemClass;   // ItemProps.className
        }

        private const int PillFormulaClass = 605;

        public static readonly Kind[] Kinds =
        {
            new Kind { Tab = "연단", Book = "단방", ItemClass = PillFormulaClass },
            new Kind { Tab = "연기", Book = "연기보", ItemClass = 618 },
            new Kind { Tab = "부적", Book = "부적 도보", ItemClass = 658 },
            new Kind { Tab = "풍수", Book = "풍수서", ItemClass = 614 },
            new Kind { Tab = "약재", Book = "약보", ItemClass = 615 },
            new Kind { Tab = "광재", Book = "감식서", ItemClass = 617 },
        };

        public static List<ItemCatalog.Entry> Books(Kind kind, string query)
        {
            return ItemCatalog.Find(query, -1, kind.ItemClass);
        }

        public static bool Learned(Kind kind, int id)
        {
            var world = g.data.world;
            if (kind.ItemClass == PillFormulaClass)
            {
                for (int i = 0; i < world.pillFormulas.Count; i++)
                {
                    if (world.pillFormulas[i].id == id)
                    {
                        return true;
                    }
                }
                return false;
            }
            return world.allArtistrySkillID.Contains(id);
        }

        public static int LearnedCount(Kind kind)
        {
            int count = 0;
            foreach (ItemCatalog.Entry entry in Books(kind, ""))
            {
                if (Learned(kind, entry.Id))
                {
                    count++;
                }
            }
            return count;
        }

        private static bool LearnNow(Kind kind, int id)
        {
            WorldUnitBase player = g.world.playerUnit;
            DataProps.PropsData props = DataProps.PropsData.NewProps(id, 1);
            if (kind.ItemClass == PillFormulaClass)
            {
                player.CreateAction(new UnitActionPropPillFormulaStudy(props), false);
            }
            else
            {
                player.CreateAction(new UnitUnitArtistrySkillStudy(props), false);
            }
            return Learned(kind, id);
        }

        public static string Learn(Kind kind, ItemCatalog.Entry book)
        {
            if (Learned(kind, book.Id))
            {
                return book.Name + ": 이미 배웠습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            bool ok = LearnNow(kind, book.Id);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), kind.Tab + " " + book.Name + "(" + book.Id + ")", "안 배움", ok ? "배움" : "안 배움", ok);
            return ok ? kind.Tab + " 배움: " + book.Name : book.Name + ": 게임이 배우게 하지 않았습니다. 강제 추가로 넣을 수 있습니다.";
        }

        // 지금 게임의 방식으로 배울 수 없는 이유. 배울 수 있으면 null.
        public static string BlockReason(Kind kind, int id)
        {
            WorldUnitBase player = g.world.playerUnit;
            DataProps.PropsData props = DataProps.PropsData.NewProps(id, 1);
            if (kind.ItemClass == PillFormulaClass)
            {
                int code = player.IsCreateAction(new UnitActionPropPillFormulaStudy(props), false);
                return code == 0 ? null : "게임의 학습 조건을 채우지 못했습니다(코드 " + code + ")";
            }
            var errors = UnitUnitArtistrySkillStudy.GetStudyErrorTip(player, props);
            if (errors == null || errors.Count == 0)
            {
                return null;
            }
            return MartialService.CleanDesc(errors[0]).Replace('\n', ' ');
        }

        // 게임의 조건을 건너뛰고 배운 기록에 직접 넣는다.
        public static string ForceLearn(Kind kind, ItemCatalog.Entry book)
        {
            if (Learned(kind, book.Id))
            {
                return book.Name + ": 이미 배웠습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var world = g.data.world;
            if (kind.ItemClass == PillFormulaClass)
            {
                var formula = new DataWorld.World.PillFormulaData();
                formula.id = book.Id;
                formula.exp = 0;
                formula.maxExp = 100; // 게임이 새로 배운 단방에 넣는 값(실측)
                world.pillFormulas.Add(formula);
            }
            else
            {
                world.allArtistrySkillID.Add(book.Id);
            }
            bool ok = Learned(kind, book.Id);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), kind.Tab + " " + book.Name + "(" + book.Id + ") 강제", "안 배움", ok ? "배움" : "안 배움", ok);
            return ok ? kind.Tab + " 강제 추가: " + book.Name : book.Name + ": 넣지 못했습니다.";
        }

        public static string LearnAll(Kind kind)
        {
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            // 풍수서처럼 앞 단계를 배워야 다음 단계를 배울 수 있는 서적이 있어, 더 배워지는 것이 없을 때까지 되풀이한다.
            List<ItemCatalog.Entry> books = Books(kind, "");
            var done = new HashSet<int>();
            int learned = 0;
            for (int pass = 0; pass < 8; pass++)
            {
                int before = learned;
                foreach (ItemCatalog.Entry entry in books)
                {
                    if (done.Contains(entry.Id))
                    {
                        continue;
                    }
                    if (Learned(kind, entry.Id))
                    {
                        done.Add(entry.Id);
                        continue;
                    }
                    if (LearnNow(kind, entry.Id))
                    {
                        done.Add(entry.Id);
                        learned++;
                    }
                }
                if (learned == before)
                {
                    break;
                }
            }
            int failed = books.Count - done.Count;
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), kind.Tab + " " + kind.Book + " 모두 배우기", "", learned + "개 배움", learned > 0 || failed == 0);
            return kind.Tab + ": " + learned + "개를 배웠습니다." + (failed > 0 ? " " + failed + "개는 게임이 배우게 하지 않았습니다." : "");
        }

        // 배운 기록을 지운다. 게임에는 잊는 기능이 없어 기록 목록에서 직접 뺀다.
        public static string Forget(Kind kind, ItemCatalog.Entry book)
        {
            if (!Learned(kind, book.Id))
            {
                return book.Name + ": 배우지 않았습니다.";
            }
            string backupError = SaveBackup.EnsureOnce();
            if (backupError != null)
            {
                return backupError + " 변경을 중단했습니다.";
            }
            var world = g.data.world;
            if (kind.ItemClass == PillFormulaClass)
            {
                for (int i = world.pillFormulas.Count - 1; i >= 0; i--)
                {
                    if (world.pillFormulas[i].id == book.Id)
                    {
                        world.pillFormulas.RemoveAt(i);
                    }
                }
                if (world.lastPillFormulaId == book.Id)
                {
                    world.lastPillFormulaId = 0;
                }
            }
            else
            {
                world.allArtistrySkillID.Remove(book.Id);
            }
            bool ok = !Learned(kind, book.Id);
            ChangeLog.Add(EditService.NameOf(g.world.playerUnit), kind.Tab + " " + book.Name + "(" + book.Id + ")", "배움", ok ? "안 배움" : "배움", ok);
            return ok ? kind.Tab + " 잊음: " + book.Name : book.Name + ": 지우지 못했습니다.";
        }
    }
}
