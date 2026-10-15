using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 도호 탭: 플레이어의 도호(칭호) 보기·주기·빼기·장착. 설계: docs/superpowers/specs/2026-10-15-appellation-design.md 3.1절.
    // 판단은 AppellationService가 하고 여기서는 그린다. 편집 대상을 따르지 않는다(플레이어 전용).
    internal sealed class AppellationTab : EditorTab
    {
        private const float RowH = 34;
        private const float TopH = 44;
        private const float DetailH = 150;
        private const float ChipH = 34, ChipGap = 6;
        private const float ColState = 4, ColName = 52, ColClass = 362, ColLoss = 458, ColHolder = 664;
        private const float StateW = 44, NameW = 300, ClassW = 90, LossW = 200, HolderW = 200;
        private const float ActW = 240;
        private const float ColAct = ContentW - Ui.ScrollBarWidth - ActW - 4;

        // 검색어가 바뀌면 탭을 다시 그리지 않고(검색 칸의 포커스를 지킨다) 칩의 수만 고친다.
        private sealed class CountLabel
        {
            public Text Text;
            public string Name;
            public string Key;
        }

        private delegate bool TitleAction(WorldUnitBase unit, int id, out string text);

        private readonly ListView<AppellationService.Entry> list = new ListView<AppellationService.Entry>("AplTable", RowH);
        private readonly DetailPane detail = new DetailPane("AplDetail", "줄을 누르면 여기에 효과와 얻는 법이 나옵니다.");
        private readonly List<CountLabel> labels = new List<CountLabel>();
        private string classKey = AppellationService.AllKey;
        private string query = "";
        private bool ownedOnly;
        private HashSet<int> owned = new HashSet<int>();   // 그릴 때마다 읽는다
        private int equipped;
        private int grade = 1;
        private int selectedId;     // 아래 칸에 보이는 도호. 0이면 없다

        public override string Title
        {
            get { return "도호"; }
        }

        public AppellationTab()
        {
            list.EmptyText = "조건에 맞는 도호가 없습니다.";
            list.OnSelect = Select;
        }

        // 줄이나 이름을 눌렀을 때. 고른 번호를 기억해 다시 그릴 때마다 아래 칸을 새로 만든다(주기·빼기 뒤에도 보유자가 맞게).
        private void Select(AppellationService.Entry e)
        {
            selectedId = e.Id;
            detail.Show(DetailOf(e));
        }

        protected override void Draw()
        {
            WorldUnitBase player = Player;
            owned = AppellationService.Owned(player);
            equipped = AppellationService.Equipped(player);
            grade = AppellationService.CurrentGrade(player);
            labels.Clear();
            DrawTop();
            float y = TopH + 8;
            y = DrawChips(y);
            y = DrawFilters(y);
            DrawHead(y);
            y += 32;
            list.HeightOf = e => RowFor(RowH, Ui.TextHeight(e.Name, NameW, 18));
            list.Build(content, 0, y, ContentW, ContentH - DetailH - 8 - y, Rows(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
            AppellationService.Entry selected = selectedId == 0 ? null : AppellationService.Get(selectedId);
            if (selected != null)
            {
                detail.Show(DetailOf(selected));
            }
        }

        private void DrawTop()
        {
            Ui.Panel("AplTop", content, 0, 0, ContentW, TopH, Ui.Section);
            Ui.Label(content, "장착 중", 12, 2, 80, TopH - 4, 17).color = Ui.Dim;
            AppellationService.Entry e = equipped == 0 ? null : AppellationService.Get(equipped);
            string text = equipped == 0 ? "장착한 도호가 없습니다."
                : e == null ? "도호 " + equipped
                : e.Name + " — " + (e.Effect.Length > 0 ? e.Effect : "효과 글 없음");
            Ui.Label(content, text, 92, 2, ContentW - 92 - 140, TopH - 4, 17).gameObject.name = "AplEquipped";
            Ui.Button("AplUnequip", content, "장착 해제", ContentW - 130, 4, 120, TopH - 8, new Action(() =>
            {
                string result;
                AppellationService.Unequip(Player, out result);
                Say(result);
            }), equipped != 0);
        }

        private bool Passes(AppellationService.Entry e)
        {
            if (ownedOnly && !owned.Contains(e.Id))
            {
                return false;
            }
            string needle = EditService.Compact(query ?? "");
            int id;
            bool isId = int.TryParse(needle, out id);
            return needle.Length == 0 || e.Search.Contains(needle) || (isId && e.Id == id);
        }

        private bool InClass(AppellationService.Entry e, string key)
        {
            if (key == AppellationService.AllKey)
            {
                return true;
            }
            if (key == AppellationService.OwnedKey)
            {
                return owned.Contains(e.Id);
            }
            return e.ClassKey == key;
        }

        private int Count(string key)
        {
            int n = 0;
            foreach (AppellationService.Entry e in AppellationService.All())
            {
                if (Passes(e) && InClass(e, key))
                {
                    n++;
                }
            }
            return n;
        }

        private List<AppellationService.Entry> Rows()
        {
            var rows = new List<AppellationService.Entry>();
            foreach (AppellationService.Entry e in AppellationService.All())
            {
                if (Passes(e) && InClass(e, classKey))
                {
                    rows.Add(e);
                }
            }
            return rows;
        }

        // 분류 칩: 전체, 가진 것, 묶음들(기타는 표에 있을 때만). 줄바꿈해 놓는다. 다음 줄의 y를 돌려준다.
        private float DrawChips(float y)
        {
            var keys = new List<string> { AppellationService.AllKey, AppellationService.OwnedKey };
            var names = new List<string> { "전체", "가진 것" };
            foreach (AppellationService.ClassGroup group in AppellationService.Groups)
            {
                if (group.Key == AppellationService.OtherKey && !AppellationService.HasOther)
                {
                    continue;
                }
                keys.Add(group.Key);
                names.Add(group.Name);
            }
            float x = 0, cy = y;
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                float w = Math.Max(70f, Ui.TextWidth(names[i] + " 000", 18) + 28f);
                if (x > 0 && x + w > ContentW)
                {
                    x = 0;
                    cy += ChipH + ChipGap;
                }
                Button chip = Ui.Button("AplClass" + key, content, names[i] + " " + Count(key), x, cy, w, ChipH,
                    new Action(() => { classKey = key; Changed(); }), true, classKey == key);
                labels.Add(new CountLabel { Text = chip.GetComponentInChildren<Text>(), Name = names[i], Key = key });
                x += w + ChipGap;
            }
            return cy + ChipH + 8;
        }

        private float DrawFilters(float y)
        {
            Ui.Label(content, "도호명", 0, y, 76, 38, 18);
            SearchBox.Build(content, "AplQuery", 76, y, 320, query, "이름의 일부 또는 번호", new Action<string>(OnQuery));
            Ui.Button("AplOwnedOnly", content, "가진 것만", 406, y, 120, 38, new Action(() => { ownedOnly = !ownedOnly; Changed(); }), true, ownedOnly);
            Ui.Button("AplClear", content, "초기화", 532, y, 100, 38, new Action(() =>
            {
                query = "";
                ownedOnly = false;
                classKey = AppellationService.AllKey;
                Changed();
            }));
            return y + 44;
        }

        private void DrawHead(float y)
        {
            Ui.Panel("AplHead", content, 0, y, ContentW, 30, Ui.Section);
            Ui.Label(content, "상태", ColState, y, StateW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
            Ui.Label(content, "도호명", ColName, y, NameW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "분류", ColClass, y, ClassW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "잃는 경지", ColLoss, y, LossW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "다른 보유자", ColHolder, y, HolderW, 30, 16).color = Ui.Dim;
            Ui.Label(content, "버튼", ColAct, y, ActW, 30, 16, TextAnchor.MiddleCenter).color = Ui.Dim;
        }

        // 분류·가진 것만·초기화: 목록을 맨 위로, 아래 칸을 비우고 다시 그린다.
        private void Changed()
        {
            list.Reset();
            selectedId = 0;
            detail.Clear();
            Redraw();
        }

        // 검색어가 바뀌었을 때: 목록과 칩의 수만 고친다.
        private void OnQuery(string value)
        {
            query = value;
            selectedId = 0;
            detail.Clear();
            list.SetItems(Rows());
            foreach (CountLabel label in labels)
            {
                label.Text.text = label.Name + " " + Count(label.Key);
            }
        }

        private void DrawRow(RectTransform row, AppellationService.Entry e, int index)
        {
            float h = row.sizeDelta.y;
            int id = e.Id;
            bool has = owned.Contains(id);
            bool on = equipped == id;
            Ui.Label(row, on ? "★" : has ? "●" : "-", ColState, 0, StateW, h, 18, TextAnchor.MiddleCenter).gameObject.name = "AplState" + id;
            WrapLabel(Ui.Button("AplName" + id, row, e.Name, ColName, 2, NameW, h - 4, new Action(() => Select(e))));
            Ui.Label(row, AppellationService.GroupName(e.ClassKey), ColClass, 0, ClassW, h, 16);
            Text loss = Ui.Label(row, AppellationService.LossText(e, grade), ColLoss, 0, LossW, h, 16);
            loss.gameObject.name = "AplLoss" + id;
            if (AppellationService.DropsSoon(e, grade))
            {
                loss.color = Ui.Warn;
            }
            List<WorldUnitBase> holders = AppellationService.OtherHolders(Player, id);
            Ui.Label(row, AppellationService.HolderText(e, holders), ColHolder, 0, HolderW, h, 16).gameObject.name = "AplHolder" + id;
            if (has)
            {
                Confirm.Button("AplRemove" + id, row, "빼기", "정말 빼기", ColAct, 2, 110, RowH - 4, "apl-remove-" + id,
                    e.Name + "을(를) 뺍니다. 한 번 더 누르면 실행합니다.", new Action(() => Run(AppellationService.Remove, id)));
                Ui.Button("AplEquip" + id, row, on ? "장착 중" : "장착", ColAct + 120, 2, ActW - 120, RowH - 4,
                    new Action(() => Run(AppellationService.Equip, id)), !on, on);
            }
            else if (e.Unique && holders.Count > 0)
            {
                string who = EditService.NameOf(holders[0]) + (holders.Count > 1 ? " 외 " + (holders.Count - 1) + "명" : "");
                Confirm.Button("AplGive" + id, row, who + "에게서 가져오기", "정말 가져오기", ColAct, 2, ActW, RowH - 4, "apl-take-" + id,
                    who + "에게서 " + e.Name + "을(를) 가져옵니다. 한 번 더 누르면 실행합니다.", new Action(() => Run(AppellationService.Give, id)));
            }
            else
            {
                Ui.Button("AplGive" + id, row, "주기", ColAct, 2, ActW, RowH - 4, new Action(() => Run(AppellationService.Give, id)));
            }
        }

        private static void Run(TitleAction action, int id)
        {
            string text;
            action(Player, id, out text);
            Say(text);
        }

        private string DetailOf(AppellationService.Entry e)
        {
            List<WorldUnitBase> holders = AppellationService.OtherHolders(Player, e.Id);
            var names = new List<string>();
            if (owned.Contains(e.Id))
            {
                names.Add("나");
            }
            for (int i = 0; i < holders.Count && i < 10; i++)
            {
                names.Add(EditService.NameOf(holders[i]));
            }
            if (holders.Count > 10)
            {
                names.Add("외 " + (holders.Count - 10) + "명");
            }
            string text = "[" + e.Id + "] " + e.Name + " · " + AppellationService.GroupName(e.ClassKey)
                + "\n효과: " + (e.Effect.Length > 0 ? e.Effect : "-")
                + "\n얻는 법: " + (e.Gain.Length > 0 ? e.Gain : "-")
                + "\n설명: " + (e.Desc.Length > 0 ? e.Desc : "-")
                + "\n보유자: " + (names.Count > 0 ? string.Join(", ", names.ToArray()) : "없음");
            if (AppellationService.DropsSoon(e, grade))
            {
                text += "\n" + AppellationService.DropNotice(e) + ".";
            }
            if (e.Story)
            {
                text += "\n" + AppellationService.StoryNotice + ".";
            }
            return text;
        }
    }
}
