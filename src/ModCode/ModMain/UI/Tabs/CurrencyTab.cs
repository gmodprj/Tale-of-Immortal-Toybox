using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 화폐 탭: 영석과, 영석 말고도 화폐로 쓰이는 재화(종문 공헌, 성주령, 전공 등)를 추가한다.
    // 게임의 아이템 표에서 재화로 숨겨진 항목 가운데 일반 재화다(ItemCatalog.Currencies). 아이템과 같은 경로로 지급되고 가방 칸을 차지하지 않는다(실측).
    internal sealed class CurrencyTab : EditorTab
    {
        public override string Title
        {
            get { return "화폐"; }
        }

        protected override void Draw()
        {
            int amount = AmountBar.Get("money");
            Ui.Label(content, "추가량", 0, 0, 70, 38, 18).color = Ui.Dim;
            AmountBar.Build(content, "money", 70, 0);
            Ui.Label(content, "가방 칸을 차지하지 않습니다. 대상은 플레이어입니다.", 570, 0, 730, 38, 15).color = Ui.Dim;
            Ui.Panel("CurrencyHead", content, 0, 48, ContentW, 30, Ui.Section);
            Ui.Label(content, "번호", 8, 48, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "화폐", 120, 48, 200, 30, 16).color = Ui.Dim;
            Ui.Label(content, "보유", 330, 48, 120, 30, 16).color = Ui.Dim;
            Ui.Label(content, "설명", 460, 48, 600, 30, 16).color = Ui.Dim;
            var props = Player.data.unitData.propData;
            List<ItemCatalog.Entry> currencies = ItemCatalog.Currencies;
            float y = 82;
            for (int i = 0; i < currencies.Count; i++)
            {
                ItemCatalog.Entry entry = currencies[i];
                // 설명은 줄바꿈해 전부 보인다. 긴 설명의 줄은 그만큼 높아진다.
                float h = RowFor(44, Ui.TextHeight(entry.Desc, 680, 15));
                RectTransform row = Ui.Rect("CurrencyRow" + entry.Id, content, 0, y, ContentW, h);
                Ui.Label(row, entry.Id.ToString(), 8, 0, 100, h, 16);
                Ui.Label(row, entry.Name, 120, 0, 200, h);
                Ui.Label(row, props.GetPropsNum(entry.Id).ToString(), 330, 0, 120, h);
                Text desc = Ui.Wrapped(row, entry.Desc, 460, 0, 680, h, 15);
                desc.color = Ui.Dim;
                desc.gameObject.name = "CurrencyDesc" + entry.Id;
                Ui.Button("CurrencyAdd" + entry.Id, row, "+ " + amount, ContentW - 150, 4, 150, 36, new Action(() => Say(Add(entry, amount))));
                y += h;
            }

            // 명성은 아이템이 아니라 인물의 속성(저장값 reputation)이다. 낭야각 같은 상점의 "명성 N 도달 필요"에 쓰이는 값과 같아(실측: 화면의 80 = 저장값 80)
            // 재화와 함께 올릴 수 있게 여기에도 둔다. 쓰기는 능력치 탭과 같은 경로다.
            AttributeDef fame = AttributeCatalog.All.Find(d => d.Label == "명성");
            if (fame != null)
            {
                y += 8;
                Ui.Panel("CurrencyFameLine", content, 0, y, ContentW, 2, Ui.Section);
                y += 8;
                const string note = "인물의 속성입니다(능력치 탭 > 개인 속성의 명성과 같은 값). 상점의 \"명성 N 도달 필요\" 조건에 쓰입니다.";
                float h = RowFor(44, Ui.TextHeight(note, 680, 15));
                RectTransform row = Ui.Rect("CurrencyRowFame", content, 0, y, ContentW, h);
                int have = fame.Get(Player.data.unitData.propertyData);
                Ui.Label(row, "속성", 8, 0, 100, h, 16).color = Ui.Dim;
                Ui.Label(row, "명성", 120, 0, 200, h);
                Ui.Label(row, have.ToString(), 330, 0, 120, h).gameObject.name = "CurrencyOwnedFame";
                Ui.Wrapped(row, note, 460, 0, 680, h, 15).color = Ui.Dim;
                Ui.Button("CurrencyAddFame", row, "+ " + amount, ContentW - 150, 4, 150, 36, new Action(() =>
                    Say(EditService.SetAttribute(Player, fame, fame.Get(Player.data.unitData.propertyData) + amount))));
            }
        }

        private static string Add(ItemCatalog.Entry entry, int amount)
        {
            if (entry.Id == PropsIDType.Money)
            {
                return EditService.AddMoney(Player, amount);
            }
            string result = EditService.GiveItem(Player, entry.Id, amount);
            // 이름의 번역이 없는 재화는 결과 문장에 번역 열쇠가 나온다. 목록에 보인 이름으로 바꾼다.
            string raw = EditService.ItemName(entry.Id);
            return string.IsNullOrEmpty(raw) || raw == entry.Name ? result : result.Replace(raw, entry.Name);
        }
    }
}
