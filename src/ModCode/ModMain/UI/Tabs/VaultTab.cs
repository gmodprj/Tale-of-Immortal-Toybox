using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 보관함 탭: 가방(이 파일)과 캐릭터(VaultTab.Character.cs)를 파일로 내보내고, 내보낸 것을 골라 가져온다.
    internal sealed partial class VaultTab : EditorTab
    {
        private sealed class Line
        {
            public bool IsMoney;
            public int Index;       // 묶음의 Items 또는 Currencies 안의 순번
            public string Search;
        }

        private readonly ListView<BagExportService.Pack> list = new ListView<BagExportService.Pack>("VaultList", 40);
        private readonly ListView<Line> detailList = new ListView<Line>("VaultItems", 34);
        private List<BagExportService.Pack> packs;   // null이면 다시 읽는다
        private string exportName = "";
        private string query = "";
        private BagExportService.Pack opened;
        private string detailQuery = "";
        private const float ModeH = 44;
        private bool character;      // 캐릭터 쪽을 보고 있는지

        public override string Title
        {
            get { return "보관함"; }
        }

        public VaultTab()
        {
            list.EmptyText = "내보낸 것이 없습니다. 위에서 이름을 적고 내보내세요.";
            detailList.EmptyText = "조건에 맞는 줄이 없습니다.";
        }

        // 탭으로 들어올 때마다 폴더를 다시 읽는다(파일을 밖에서 넣거나 지웠을 수 있다).
        public override void OnShow()
        {
            packs = null;
            charPacks = null;
        }

        public override void OnOpen()
        {
            packs = null;
            charPacks = null;
        }

        private List<BagExportService.Pack> Found()
        {
            if (query.Length == 0)
            {
                return packs;
            }
            return packs.FindAll(p => Matches(p.Name, query) || Matches(System.IO.Path.GetFileName(p.File), query));
        }

        // 검색은 대소문자를 가리지 않는다.
        private static bool Matches(string text, string needle)
        {
            return text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        protected override void Draw()
        {
            Ui.Button("VaultModeBag", content, "가방", 0, 0, 130, 36, new Action(() => { character = false; Redraw(); }), true, !character);
            Ui.Button("VaultModeChar", content, "캐릭터", 136, 0, 130, 36, new Action(() => { character = true; Redraw(); }), true, character);
            // 아래 화면은 전환 줄만큼 내려 그린다. 가방과 캐릭터의 그리기 코드는 자기 영역의 왼쪽 위를 원점으로 쓴다.
            RectTransform whole = content;
            content = Ui.Rect("VaultArea", whole, 0, ModeH, ContentW, ContentH - ModeH);
            if (character)
            {
                DrawCharacters();
            }
            else
            {
                DrawBag();
            }
            content = whole;
        }

        private void DrawBag()
        {
            if (packs == null)
            {
                packs = BagExportService.List();
            }
            Ui.Label(content, "이름", 0, 0, 60, 38, 18).color = Ui.Dim;
            Ui.Input("VaultName", content, exportName, "내보낼 이름", 60, 0, 330, 38, new Action<string>(v => exportName = v));
            Ui.Button("VaultExport", content, "현재 가방 내보내기", 400, 0, 230, 38, new Action(() =>
            {
                string result = BagExportService.Export(exportName);
                packs = null;
                list.Reset();
                Say(result);
            }));
            Text note = Ui.Label(content, "가방의 아이템(장착 중인 것 포함), 영석과 화폐를 담습니다. 이 세이브에 묶인 아이템(종문 추천서 등)은 담지 않습니다.", 646, 0, 650, 38, 14);
            note.color = Ui.Dim;
            note.horizontalOverflow = HorizontalWrapMode.Wrap;

            Ui.Label(content, "찾기", 0, 48, 60, 38, 18).color = Ui.Dim;
            SearchBox.Build(content, "VaultQuery", 60, 48, 330, query, "이름의 일부", new Action<string>(v =>
            {
                query = v;
                list.SetItems(Found());
            }));
            Text where = Ui.Label(content, "지금 가방 " + BagExportService.Slots() + "칸. 가져오기는 항상 지금 가방에 더합니다. 파일은 " + BagExportService.Dir + " 에 있습니다.", 400, 48, ContentW - 400, 38, 14);
            where.gameObject.name = "VaultBagSlots";
            where.color = Ui.Dim;
            where.horizontalOverflow = HorizontalWrapMode.Wrap;

            Ui.Panel("VaultHead", content, 0, 96, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 96, 290, 30, 16).color = Ui.Dim;
            Ui.Label(content, "날짜", 300, 96, 190, 30, 16).color = Ui.Dim;
            Ui.Label(content, "인물", 495, 96, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템", 610, 96, 55, 30, 16).color = Ui.Dim;
            Ui.Label(content, "화폐", 668, 96, 45, 30, 16).color = Ui.Dim;
            list.HeightOf = pack => pack.Error != null
                ? RowFor(40, Ui.TextHeight(Unreadable(pack), ContentW - Ui.ScrollBarWidth - 158, 16))
                : RowFor(40, Ui.TextHeight(pack.Name, 290, 20), Ui.TextHeight(pack.Character, 110, 16));
            list.Build(content, 0, 128, ContentW, ContentH - ModeH - 128, Found(), DrawRow);
        }

        private static string Unreadable(BagExportService.Pack pack)
        {
            return "(읽을 수 없음) " + System.IO.Path.GetFileName(pack.File) + " — " + pack.Error;
        }

        private void DrawRow(RectTransform row, BagExportService.Pack pack, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            if (pack.Error != null)
            {
                Text bad = Ui.Wrapped(row, Unreadable(pack), 8, 0, right - 158, h, 16);
                bad.color = Ui.Warn;
            }
            else
            {
                Ui.Wrapped(row, pack.Name, 8, 0, 290, h);
                Ui.Label(row, pack.Date, 300, 0, 190, h, 16);
                Ui.Wrapped(row, pack.Character, 495, 0, 110, h, 16);
                Ui.Label(row, pack.Items.Count.ToString(), 610, 0, 55, h, 16);
                Ui.Label(row, pack.Currencies.Count.ToString(), 668, 0, 45, h, 16);
                Ui.Button("VaultOpen" + index, row, "세부", right - 566, 3, 90, 34, new Action(() =>
                {
                    opened = pack;
                    detailQuery = "";
                    detailList.Reset();
                    Push(() => opened.Name + " — " + opened.Character + ", " + opened.Date, DrawDetail);
                }));
                string key = "vault-all:" + pack.File;
                if (Confirm.IsPending(key))
                {
                    // 칸이 모자라 물은 상태. 가방을 늘릴지는 사용자가 고른다(추가 칸은 이 세이브에 계속 남는 변경이다).
                    Ui.Button("VaultExpand" + index, row, "가방을 늘리고 모두", right - 470, 3, 190, 34, new Action(() =>
                    {
                        Confirm.Clear();
                        Say(BagExportService.ImportAll(pack, true));
                    }), true, true);
                    Ui.Button("VaultFit" + index, row, "들어가는 만큼만", right - 274, 3, 160, 34, new Action(() =>
                    {
                        Confirm.Clear();
                        Say(BagExportService.ImportAll(pack, false));
                    }), true, true);
                }
                else
                {
                    Ui.Button("VaultAll" + index, row, "모두 가져오기", right - 470, 3, 160, 34, new Action(() =>
                    {
                        int need = BagExportService.NeededSlots(pack);
                        int free = BagExportService.FreeSlots();
                        // 파일이 밖에서 지워졌으면 칸을 묻지 않고 바로 그 사실을 알린다(ImportAll이 알린다).
                        if (need > free && System.IO.File.Exists(pack.File))
                        {
                            Confirm.Ask(key, pack.Name + ": 필요 " + need + "칸, 빈 칸 " + free + "칸. 가방을 늘리고 모두 가져올지, 들어가는 만큼만 가져올지 고르세요.");
                            return;
                        }
                        Say(BagExportService.ImportAll(pack, false));
                    }));
                }
            }
            Confirm.Button("VaultDelete" + index, row, "삭제", "정말 삭제", right - 108, 3, 100, 34, "vault-del:" + pack.File,
                System.IO.Path.GetFileName(pack.File) + ": 파일을 지웁니다. 되돌릴 수 없습니다. \"정말 삭제\"를 한 번 더 누르세요.", new Action(() =>
                {
                    string result = BagExportService.Delete(pack);
                    packs = null;
                    Say(result);
                }));
        }

        private List<Line> DetailLines()
        {
            var lines = new List<Line>();
            for (int i = 0; i < opened.Items.Count; i++)
            {
                BagExportService.Item item = opened.Items[i];
                lines.Add(new Line { IsMoney = false, Index = i, Search = item.Name + " " + item.Id });
            }
            for (int i = 0; i < opened.Currencies.Count; i++)
            {
                BagExportService.Money money = opened.Currencies[i];
                lines.Add(new Line { IsMoney = true, Index = i, Search = money.Name + " " + money.Id });
            }
            return detailQuery.Length == 0 ? lines : lines.FindAll(l => Matches(l.Search, detailQuery));
        }

        // 하위 화면: 한 묶음의 아이템과 화폐. 줄마다 하나씩 가져온다.
        private void DrawDetail()
        {
            float y = SubTop;
            SearchBox.Build(content, "VaultItemQuery", 0, y, 330, detailQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                detailQuery = v;
                detailList.SetItems(DetailLines());
            }));
            Ui.Label(content, "아이템 " + opened.Items.Count + "줄 · 화폐 " + opened.Currencies.Count + "종"
                + (opened.Skipped > 0 ? " · 내보낼 때 뺀 것 " + opened.Skipped + "개" : "") + "    가져온 장비는 가방에 들어가고 자동으로 장착하지 않습니다.",
                350, y, 950, 38, 15).color = Ui.Dim;
            Ui.Panel("VaultItemHead", content, 0, y + 46, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", 8, y + 46, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "이름", 130, y + 46, 400, 30, 16).color = Ui.Dim;
            Ui.Label(content, "수량", 540, y + 46, 120, 30, 16).color = Ui.Dim;
            Ui.Label(content, "종류", 670, y + 46, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "장착", 780, y + 46, 80, 30, 16).color = Ui.Dim;
            float top = y + 78;
            detailList.HeightOf = line => RowFor(34, Ui.TextHeight(line.IsMoney ? opened.Currencies[line.Index].Name : opened.Items[line.Index].Name, 400, 17));
            detailList.Build(content, 0, top, ContentW, ContentH - top, DetailLines(), DrawDetailRow);
        }

        private void DrawDetailRow(RectTransform row, Line line, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            BagExportService.Pack pack = opened;
            if (line.IsMoney)
            {
                BagExportService.Money money = pack.Currencies[line.Index];
                Ui.Label(row, money.Id.ToString(), 8, 0, 110, h, 16);
                Ui.Wrapped(row, money.Name, 130, 0, 400, h, 17);
                Ui.Label(row, money.Count.ToString(), 540, 0, 120, h, 16);
                Ui.Label(row, "화폐", 670, 0, 100, h, 16).color = Ui.Warn;
                int at = line.Index;
                string whyNot = BagExportService.Why(money);
                if (whyNot != null)
                {
                    Ui.Wrapped(row, whyNot, 780, 0, right - 128 - 790, h, 14).color = Ui.Warn;
                }
                Ui.Button("VaultGetC" + at, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportCurrency(pack, at))), whyNot == null);
                return;
            }
            BagExportService.Item item = pack.Items[line.Index];
            Ui.Label(row, item.Martial ? "-" : item.Id.ToString(), 8, 0, 110, h, 16);
            Ui.Wrapped(row, item.Name, 130, 0, 400, h, 17);
            Ui.Label(row, item.Count.ToString(), 540, 0, 120, h, 16);
            Ui.Label(row, item.Martial ? "비급" : "아이템", 670, 0, 100, h, 16);
            int itemAt = line.Index;
            // 쓸 수 없는 줄(게임에 없는 번호, 수량 0 등)은 이유를 보이고 버튼을 끈다.
            string why = BagExportService.Why(item);
            Text state = Ui.Wrapped(row, why ?? (item.Equipped ? "장착 중" : ""), 780, 0, right - 128 - 790, h, why == null ? 16 : 14);
            state.color = Ui.Warn;
            state.gameObject.name = "VaultWhy" + itemAt;
            Ui.Button("VaultGet" + itemAt, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportItem(pack, itemAt))), why == null);
        }
    }
}
