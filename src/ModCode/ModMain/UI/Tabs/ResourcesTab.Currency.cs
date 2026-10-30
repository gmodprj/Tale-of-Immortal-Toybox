using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 자원 탭의 화폐 그룹 쪽(13 문서 S1로 나눔): 명성 줄, 화폐 줄, 화폐 더하기.
    internal sealed partial class ResourcesTab
    {
        // 화폐 그룹의 명성 줄. 명성은 아이템이 아니라 인물의 속성(저장값 reputation)이다. 낭야각 같은 상점의 "명성 N 도달 필요"에 쓰이는 값과 같아
        // (실측: 화면의 80 = 저장값 80) 재화와 함께 올릴 수 있게 여기 둔다. 쓰기는 능력치 탭과 같은 경로다.
        private float DrawFameLine(float y)
        {
            AttributeDef fame = AttributeCatalog.All.Find(d => d.Label == "명성");
            if (fame == null)
            {
                return y;
            }
            int amount = AmountBar.Get("item");
            int have = fame.Get(Player.data.unitData.propertyData);
            Ui.Label(content, "속성 명성", RightX, y, 110, 38, 18);
            Ui.Label(content, "보유", RightX + 110, y, 44, 38, 16).color = Ui.Dim;
            Ui.Label(content, have.ToString(), RightX + 154, y, 100, 38, 18).gameObject.name = "CurrencyOwnedFame";
            Ui.Button("CurrencyAddFame", content, "+ " + amount, RightX + 260, y + 2, 120, 34, new Action(() =>
                Say(EditService.SetAttribute(Player, fame, fame.Get(Player.data.unitData.propertyData) + amount))));
            // 안내는 두 줄로 줄바꿈한다(한 줄에는 들어가지 않는다. 회차 ws-1017f의 캡처).
            Ui.Wrapped(content, "인물의 속성입니다(능력치 탭 > 개인 속성의 명성과 같은 값). 상점의 \"명성 N 도달 필요\" 조건에 쓰입니다. 아래 재화는 가방 칸을 차지하지 않습니다.",
                RightX + 392, y, RightW - 392, 40, 14, TextAnchor.UpperLeft).color = Ui.Dim;
            return y + 44;
        }

        // 화폐 줄: ☆ 없음(즐겨찾기는 가방 아이템의 것), 종류 "화폐", 품질 없음, 보유는 영석이면 EditService.Money. 지급은 예전 화폐 탭의 Add와 같다.
        private void DrawCurrencyRow(RectTransform row, ItemCatalog.Entry entry, float h)
        {
            int id = entry.Id;
            WrapLabel(Ui.Button("ItemName" + id, row, entry.Name, ColName, 2, NameW, h - 4, new Action(() => detail.Show(Detail(entry)))));
            Ui.Wrapped(row, "화폐", ColKind, 0, KindW, h, 16);
            Ui.Label(row, "-", ColLevel, 0, 44, h, 16).color = Ui.Dim;
            int have = id == PropsIDType.Money ? EditService.Money(Player) : Player.data.unitData.propData.GetPropsNum(id);
            Ui.Label(row, have.ToString(), ColOwned, 0, 60, h, 16).gameObject.name = "CurrencyOwned" + id;
            int amount = AmountBar.Get("item");
            Ui.Button("CurrencyAdd" + id, row, "+ " + amount, ColGive, 2, GiveW, RowH - 4, new Action(() => Say(AddCurrency(entry, amount))));
        }

        private static string AddCurrency(ItemCatalog.Entry entry, int amount)
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
