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
            for (int i = 0; i < currencies.Count; i++)
            {
                ItemCatalog.Entry entry = currencies[i];
                float y = 82 + i * 44;
                Ui.Label(content, entry.Id.ToString(), 8, y, 100, 40, 16);
                Ui.Label(content, entry.Name, 120, y, 200, 40);
                Ui.Label(content, props.GetPropsNum(entry.Id).ToString(), 330, y, 120, 40);
                Ui.Label(content, Shorten(entry.Desc, 44), 460, y, 680, 40, 15).color = Ui.Dim;
                Ui.Button("CurrencyAdd" + entry.Id, content, "+ " + amount, ContentW - 150, y + 2, 150, 36, new Action(() => Say(Add(entry, amount))));
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
