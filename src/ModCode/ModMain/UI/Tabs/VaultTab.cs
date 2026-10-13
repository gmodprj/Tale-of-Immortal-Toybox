using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 보관함 탭: 가방을 파일로 내보내고, 내보낸 것을 골라 지금 가방에 더한다. 하위 화면: 한 묶음의 세부 목록.
    internal sealed class VaultTab : EditorTab
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
        }

        public override void OnOpen()
        {
            packs = null;
        }

        private List<BagExportService.Pack> Found()
        {
            if (query.Length == 0)
            {
                return packs;
            }
            return packs.FindAll(p => p.Name.Contains(query) || System.IO.Path.GetFileName(p.File).Contains(query));
        }

        protected override void Draw()
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
            Text where = Ui.Label(content, "가져오기는 항상 지금 가방에 더합니다. 파일은 " + BagExportService.Dir + " 에 있습니다.", 400, 48, ContentW - 400, 38, 14);
            where.color = Ui.Dim;
            where.horizontalOverflow = HorizontalWrapMode.Wrap;

            Ui.Panel("VaultHead", content, 0, 96, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 96, 290, 30, 16).color = Ui.Dim;
            Ui.Label(content, "날짜", 300, 96, 190, 30, 16).color = Ui.Dim;
            Ui.Label(content, "인물", 495, 96, 110, 30, 16).color = Ui.Dim;
            Ui.Label(content, "아이템", 610, 96, 55, 30, 16).color = Ui.Dim;
            Ui.Label(content, "화폐", 668, 96, 45, 30, 16).color = Ui.Dim;
            list.Build(content, 0, 128, ContentW, ContentH - 128, Found(), DrawRow);
        }

        private void DrawRow(RectTransform row, BagExportService.Pack pack, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            if (pack.Error != null)
            {
                Text bad = Ui.Label(row, "(읽을 수 없음) " + System.IO.Path.GetFileName(pack.File) + " — " + Shorten(pack.Error, 40), 8, 0, right - 150, 40, 16);
                bad.color = Ui.Warn;
            }
            else
            {
                Ui.Label(row, Shorten(pack.Name, 14), 8, 0, 290, 40);
                Ui.Label(row, pack.Date, 300, 0, 190, 40, 16);
                Ui.Label(row, Shorten(pack.Character, 6), 495, 0, 110, 40, 16);
                Ui.Label(row, pack.Items.Count.ToString(), 610, 0, 55, 40, 16);
                Ui.Label(row, pack.Currencies.Count.ToString(), 668, 0, 45, 40, 16);
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
                        if (need > free)
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
            return detailQuery.Length == 0 ? lines : lines.FindAll(l => l.Search.Contains(detailQuery));
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
            detailList.Build(content, 0, top, ContentW, ContentH - top, DetailLines(), DrawDetailRow);
        }

        private void DrawDetailRow(RectTransform row, Line line, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            BagExportService.Pack pack = opened;
            if (line.IsMoney)
            {
                BagExportService.Money money = pack.Currencies[line.Index];
                Ui.Label(row, money.Id.ToString(), 8, 0, 110, 34, 16);
                Ui.Label(row, Shorten(money.Name, 24), 130, 0, 400, 34, 17);
                Ui.Label(row, money.Count.ToString(), 540, 0, 120, 34, 16);
                Ui.Label(row, "화폐", 670, 0, 100, 34, 16).color = Ui.Warn;
                int at = line.Index;
                Ui.Button("VaultGetC" + at, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportCurrency(pack, at))));
                return;
            }
            BagExportService.Item item = pack.Items[line.Index];
            Ui.Label(row, item.Martial ? "-" : item.Id.ToString(), 8, 0, 110, 34, 16);
            Ui.Label(row, Shorten(item.Name, 24), 130, 0, 400, 34, 17);
            Ui.Label(row, item.Count.ToString(), 540, 0, 120, 34, 16);
            Ui.Label(row, item.Martial ? "비급" : "아이템", 670, 0, 100, 34, 16);
            Ui.Label(row, item.Equipped ? "장착 중" : "", 780, 0, 80, 34, 16).color = Ui.Warn;
            int itemAt = line.Index;
            Ui.Button("VaultGet" + itemAt, row, "가져오기", right - 128, 2, 120, 30, new Action(() => Say(BagExportService.ImportItem(pack, itemAt))));
        }
    }
}
