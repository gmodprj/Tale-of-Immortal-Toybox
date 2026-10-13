using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 능력치 탭: 게임 인물 화면의 다섯 분류로 저장값을 편집한다. 저장값 칸에 숫자를 쓰고 Enter를 누르면 그 값이 된다.
    internal sealed class AttributesTab : EditorTab
    {
        private int attributeCategory;

        public override string Title
        {
            get { return "능력치"; }
        }

        public override bool UsesTarget
        {
            get { return true; }
        }

        public override void OnTargetChanged()
        {
            attributeCategory = 0;
        }

        protected override void Draw()
        {
            WorldUnitBase target = Target;
            int step = AmountBar.Get("attr");
            var saved = target.data.unitData.propertyData;
            var dyn = target.data.dynUnitData;
            ConfRoleAttributeLimit limit = g.conf.roleAttributeLimit;
            List<AttributeDef> all = AttributeCatalog.All;

            // 게임의 인물 화면과 같은 분류
            for (int i = 0; i < AttributeCatalog.Categories.Length; i++)
            {
                int category = i;
                Ui.Button("AttrCat" + i, content, AttributeCatalog.Categories[i], i * 156, 0, 150, 40,
                    new Action(() => { attributeCategory = category; Redraw(); }), true, attributeCategory == i);
            }

            const float head = 52;
            Ui.Label(content, "항목", 0, head, 190, 30, 18).color = Ui.Dim;
            Ui.Label(content, "저장값", 206, head, 110, 30, 18).color = Ui.Dim;
            Ui.Label(content, "게임 화면 값(보정 포함)", 330, head, 220, 30, 18).color = Ui.Dim;
            Ui.Label(content, "허용 범위", 560, head, 220, 30, 18).color = Ui.Dim;

            int row = 0;
            for (int i = 0; i < all.Count; i++)
            {
                AttributeDef def = all[i];
                if (def.Category != attributeCategory)
                {
                    continue;
                }
                int index = i;
                float y = head + 34 + row * 40;
                row++;
                int value = def.Get(saved);
                int min = def.Min(limit);
                int max = def.Max(limit);
                Ui.Label(content, def.Label, 0, y, 200, 36);
                NumberField.Build(content, "AttrValue" + index, 206, y, 110, 34, value.ToString(), null,
                    new Action<int>(v => Say(EditService.SetAttribute(Target, def, v))));
                Ui.Label(content, def.DynValue(dyn).ToString(), 330, y, 220, 36);
                Ui.Label(content, min + " ~ " + max + (def.OwnLimit ? " (자체 상한)" : ""), 560, y, 260, 36, 18).color = Ui.Dim;
                Ui.Button("Minus" + index, content, "- " + step, 830, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) - step))));
                Ui.Button("Plus" + index, content, "+ " + step, 936, y, 100, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Get(Target.data.unitData.propertyData) + step))));
                Ui.Button("Max" + index, content, "최대", 1042, y, 84, 34, new Action(() => Say(EditService.SetAttribute(Target, def, def.Max(g.conf.roleAttributeLimit)))));
            }

            // 분류마다 줄 수가 달라도 아래쪽 줄의 위치는 고정한다(가장 긴 분류가 12줄).
            float footer = head + 34 + 12 * 40 + 12;
            Ui.Label(content, "증감 단위", 0, footer, 110, 38, 18).color = Ui.Dim;
            float right = AmountBar.Build(content, "attr", 110, footer);
            Ui.Button("Restore", content, "자원 회복", right + 30, footer, 140, 38, new Action(() => Say(EditService.RestoreResources(Target))));
            Ui.Label(content, "저장값 칸에 숫자를 쓰고 Enter를 누르면 그 값으로 바뀝니다.", 0, footer + 52, ContentW, 30, 18).color = Ui.Dim;
            Ui.Label(content, "저장값은 인물 고유의 값이고, 게임 화면에는 운명·장비 등의 보정이 더해진 값이 나옵니다. 편집기는 저장값을 바꿉니다.", 0, footer + 86, ContentW, 30, 15).color = Ui.Dim;
        }
    }
}
