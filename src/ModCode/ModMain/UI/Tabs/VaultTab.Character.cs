using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 보관함 탭의 캐릭터 쪽: 플레이어를 파일로 내보내고, 내보낸 것을 골라 지금 캐릭터로 가져온다.
    internal sealed partial class VaultTab
    {
        private readonly ListView<CharacterExportService.Pack> charList = new ListView<CharacterExportService.Pack>("CharList", 40);
        private List<CharacterExportService.Pack> charPacks;    // null이면 다시 읽는다
        private string charName = "";
        private string charQuery = "";

        private List<CharacterExportService.Pack> FoundCharacters()
        {
            if (charQuery.Length == 0)
            {
                return charPacks;
            }
            return charPacks.FindAll(p => p.Name.Contains(charQuery) || System.IO.Path.GetFileName(p.File).Contains(charQuery));
        }

        private static string UnreadableChar(CharacterExportService.Pack pack)
        {
            return "(읽을 수 없음) " + System.IO.Path.GetFileName(pack.File) + " — " + pack.Error;
        }

        private void DrawCharacters()
        {
            if (charPacks == null)
            {
                charPacks = CharacterExportService.List();
            }
            charList.EmptyText = "내보낸 것이 없습니다. 위에서 이름을 적고 내보내세요.";
            Ui.Label(content, "이름", 0, 0, 60, 38, 18).color = Ui.Dim;
            Ui.Input("CharName", content, charName, "내보낼 이름", 60, 0, 330, 38, new Action<string>(v => charName = v));
            Ui.Button("CharExport", content, "현재 캐릭터 내보내기", 400, 0, 240, 38, new Action(() =>
            {
                string result = CharacterExportService.Export(charName);
                charPacks = null;
                charList.Reset();
                Say(result);
            }));
            Ui.Wrapped(content, "플레이어의 능력치·경지·공법·기예·운명을 담습니다. 내보내기도 가져오기도 항상 플레이어에 합니다(편집 대상과 무관).", 656, 0, ContentW - 656, 38, 14).color = Ui.Dim;

            Ui.Label(content, "찾기", 0, 48, 60, 38, 18).color = Ui.Dim;
            SearchBox.Build(content, "CharQuery", 60, 48, 330, charQuery, "이름의 일부", new Action<string>(v =>
            {
                charQuery = v;
                charList.SetItems(FoundCharacters());
            }));
            Ui.Wrapped(content, "공법·기예·운명은 지금 것에 더하고, 능력치와 경지는 파일의 값으로 바뀝니다. 파일은 " + ExportFiles.Dir(CharacterExportService.Kind) + " 에 있습니다.", 400, 48, ContentW - 400, 38, 14).color = Ui.Dim;

            Ui.Panel("CharHead", content, 0, 96, ContentW, 30, Ui.Section);
            Ui.Label(content, "이름", 8, 96, 250, 30, 16).color = Ui.Dim;
            Ui.Label(content, "날짜", 265, 96, 180, 30, 16).color = Ui.Dim;
            Ui.Label(content, "인물", 450, 96, 100, 30, 16).color = Ui.Dim;
            Ui.Label(content, "경지", 555, 96, 150, 30, 16).color = Ui.Dim;
            Ui.Label(content, "공법", 710, 96, 45, 30, 16).color = Ui.Dim;
            Ui.Label(content, "기예", 758, 96, 45, 30, 16).color = Ui.Dim;
            Ui.Label(content, "운명", 806, 96, 45, 30, 16).color = Ui.Dim;
            charList.HeightOf = pack => pack.Error != null
                ? RowFor(40, Ui.TextHeight(UnreadableChar(pack), ContentW - Ui.ScrollBarWidth - 158, 16))
                : RowFor(40, Ui.TextHeight(pack.Name, 250, 20), Ui.TextHeight(pack.Character, 100, 16), Ui.TextHeight(pack.GradeName, 150, 16));
            charList.Build(content, 0, 128, ContentW, ContentH - ModeH - 128, FoundCharacters(), DrawCharacterRow);
        }

        private void DrawCharacterRow(RectTransform row, CharacterExportService.Pack pack, int index)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            float h = row.sizeDelta.y;
            if (pack.Error != null)
            {
                Ui.Wrapped(row, UnreadableChar(pack), 8, 0, right - 158, h, 16).color = Ui.Warn;
            }
            else
            {
                Ui.Wrapped(row, pack.Name, 8, 0, 250, h);
                Ui.Label(row, pack.Date, 265, 0, 180, h, 16);
                Ui.Wrapped(row, pack.Character, 450, 0, 100, h, 16);
                Ui.Wrapped(row, pack.HasGrade ? pack.GradeName : "-", 555, 0, 150, h, 16);
                Ui.Label(row, pack.Martials.Count.ToString(), 710, 0, 45, h, 16);
                Ui.Label(row, pack.Arts.Count.ToString(), 758, 0, 45, h, 16);
                Ui.Label(row, (pack.BornLucks.Count + pack.AddLucks.Count).ToString(), 806, 0, 45, h, 16);
                DrawCharacterActions(row, pack, index, right);
            }
            Confirm.Button("CharDelete" + index, row, "삭제", "정말 삭제", right - 108, 3, 100, 34, "char-del:" + pack.File,
                System.IO.Path.GetFileName(pack.File) + ": 파일을 지웁니다. 되돌릴 수 없습니다. \"정말 삭제\"를 한 번 더 누르세요.", new Action(() =>
                {
                    string result = CharacterExportService.Delete(pack);
                    charPacks = null;
                    Say(result);
                }));
        }

        // 읽힌 묶음의 줄에 붙는 버튼(세부, 모두 가져오기). 가져오기가 생기기 전에는 비어 있다.
        private void DrawCharacterActions(RectTransform row, CharacterExportService.Pack pack, int index, float right)
        {
        }
    }
}
