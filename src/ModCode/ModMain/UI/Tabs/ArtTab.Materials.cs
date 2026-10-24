using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 기예 탭의 하위 화면 "제작 재료"(docs/superpowers/specs/2026-10-18-craft-materials-design.md 5절, 05 문서 62절).
    // 연단·연기·부적 처방을 고르면 한 벌(× 묶음 수)의 재료를 가방에 넣는다. 지급과 계산은 CraftMaterialService.
    internal sealed partial class ArtTab
    {
        private static readonly int[] MatCounts = { 1, 5, 10 };
        private readonly ListView<CraftMaterialService.Recipe> matList = new ListView<CraftMaterialService.Recipe>("MatList", 64);
        private readonly DetailPane matDetail = new DetailPane("MatDetail", "줄을 누르면 재료 전체와 조건이 여기에 나옵니다.");
        private int matKind;
        private bool matLearnedOnly = true;
        private string matQuery = "";
        private int matCount = 1;

        private void OpenMaterials()
        {
            matList.Reset();
            matDetail.Clear();
            Push(() => "제작 재료", DrawMaterials);
        }

        private List<CraftMaterialService.Recipe> MatItems()
        {
            return CraftMaterialService.Recipes(matKind, matLearnedOnly, matQuery);
        }

        private void DrawMaterials()
        {
            float y = SubTop;
            for (int i = 0; i < CraftMaterialService.KindNames.Length; i++)
            {
                int index = i;
                Ui.Button("MatKind" + i, content, CraftMaterialService.KindNames[i], i * 126, y, 120, 38,
                    new Action(() => { matKind = index; matList.Reset(); matDetail.Clear(); Redraw(); }), true, matKind == i);
            }
            Ui.Button("MatLearnedOnly", content, "배운 것만", 390, y, 120, 38,
                new Action(() => { matLearnedOnly = !matLearnedOnly; matList.Reset(); Redraw(); }), true, matLearnedOnly);
            SearchBox.Build(content, "MatQuery", 520, y, 300, matQuery, "처방 이름 또는 번호", new Action<string>(v =>
            {
                matQuery = v;
                matDetail.Clear();
                matList.SetItems(MatItems());
            }));
            Ui.Label(content, "묶음", 840, y, 60, 38, 16).color = Ui.Dim;
            for (int i = 0; i < MatCounts.Length; i++)
            {
                int count = MatCounts[i];
                Ui.Button("MatCount" + count, content, count.ToString(), 900 + i * 64, y, 58, 38,
                    new Action(() => { matCount = count; Redraw(); }), true, matCount == count);
            }
            y += 46;
            Ui.Label(content, "처방 한 벌의 재료를 가방에 넣습니다(연단은 쓸 단로가 없을 때 단로 하나도). 만들기는 게임의 제작 화면에서 합니다.", 0, y, ContentW, 28, 15).color = Ui.Dim;
            y += 32;
            matList.EmptyText = matLearnedOnly ? "배운 처방이 없습니다. \"배운 것만\"을 끄면 모두 보입니다." : "조건에 맞는 처방이 없습니다.";
            matList.OnSelect = r => matDetail.Show(r.Name + "  [" + r.Key + "]" + (r.Learned ? "" : "  (배우지 않음)") + "\n" + CraftMaterialService.Summary(r)
                + "\n" + r.Detail + (r.Warning != null ? "\n" + r.Warning : ""));
            matList.HeightOf = r => Math.Max(64, (float)Math.Ceiling(Ui.TextHeight(SummaryText(r), 900, 15)) + 34);
            float detailH = 90;
            matList.Build(content, 0, y, ContentW, ContentH - y - detailH - 8, MatItems(), DrawMatRow);
            matDetail.Build(content, 0, ContentH - detailH, ContentW, detailH);
        }

        private static string SummaryText(CraftMaterialService.Recipe r)
        {
            return CraftMaterialService.Summary(r) + (r.Warning != null ? "  — " + r.Warning : "");
        }

        private void DrawMatRow(RectTransform row, CraftMaterialService.Recipe r, int index)
        {
            float h = row.sizeDelta.y;
            Ui.Label(row, r.Name + (r.Learned ? "" : "  (배우지 않음)"), 8, 2, 700, 28, 18).color = r.Learned ? Color.white : Ui.Dim;
            Ui.Label(row, (r.Kind == 2 ? r.Grade + "단계" : "품질 " + r.Grade), 720, 2, 120, 28, 16).color = Ui.Dim;
            Text summary = Ui.Wrapped(row, SummaryText(r), 24, 30, 900, h - 32, 15, TextAnchor.UpperLeft);
            summary.color = r.Warning != null ? Ui.Warn : Ui.Dim;
            summary.gameObject.name = "MatSummary" + r.Key;
            CraftMaterialService.Recipe recipe = r;
            Ui.Button("MatGive" + r.Key, row, "재료 지급", ContentW - Ui.ScrollBarWidth - 178, (h - 36) / 2, 170, 36,
                new Action(() => Say(CraftMaterialService.Give(recipe, matCount))));
        }
    }
}
