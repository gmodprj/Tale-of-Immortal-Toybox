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

        private sealed class CharLine
        {
            public CharacterExportService.Section Section;
            public int Index;       // 그 구역의 목록 안의 순번
            public bool Born;       // 운명 구역: 선천인지
            public string Search = "";
        }

        private readonly ListView<CharLine> charDetail = new ListView<CharLine>("CharItems", 38);
        private CharacterExportService.Pack charOpened;
        private CharacterExportService.Section charSection = CharacterExportService.Section.Grade;
        private string charItemQuery = "";

        private List<CharacterExportService.Pack> FoundCharacters()
        {
            if (charQuery.Length == 0)
            {
                return charPacks;
            }
            return charPacks.FindAll(p => Matches(p.Name, charQuery) || Matches(System.IO.Path.GetFileName(p.File), charQuery));
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

        // 읽힌 묶음의 줄에 붙는 버튼.
        private void DrawCharacterActions(RectTransform row, CharacterExportService.Pack pack, int index, float right)
        {
            Ui.Button("CharOpen" + index, row, "세부", right - 380, 3, 90, 34, new Action(() =>
            {
                charOpened = pack;
                charSection = CharacterExportService.Section.Grade;
                charItemQuery = "";
                charDetail.Reset();
                Push(() => charOpened.Name + " — " + charOpened.Character + ", " + charOpened.Date, DrawCharacterDetail);
            }));
            Confirm.Button("CharAll" + index, row, "모두 가져오기", "정말 가져오기", right - 284, 3, 170, 34, "char-all:" + pack.File,
                pack.Name + ": 경지와 능력치가 파일의 값으로 바뀌고, 공법·기예·운명은 없는 것이 더해집니다. \"정말 가져오기\"를 한 번 더 누르세요.",
                new Action(() => Say(CharacterExportService.ImportAll(pack))));
        }

        private static string SectionLabel(CharacterExportService.Section section, CharacterExportService.Pack pack)
        {
            switch (section)
            {
                case CharacterExportService.Section.Grade: return "경지";
                case CharacterExportService.Section.Attributes: return "능력치 " + pack.Attributes.Count;
                case CharacterExportService.Section.Martials: return "공법 " + pack.Martials.Count;
                case CharacterExportService.Section.Arts: return "기예 " + pack.Arts.Count;
                case CharacterExportService.Section.Lucks: return "운명 " + (pack.BornLucks.Count + pack.AddLucks.Count);
                default: return "역천개명 " + pack.Fates.Count;
            }
        }

        private static string LuckTips(int id)
        {
            ConfRoleCreateFeatureItem conf = g.conf.roleCreateFeature.GetItem(id);
            return conf == null ? "" : MartialService.CleanDesc(GameTool.LS(conf.tips)).Replace('\n', ' ');
        }

        private List<CharLine> CharLines()
        {
            CharacterExportService.Pack pack = charOpened;
            var lines = new List<CharLine>();
            switch (charSection)
            {
                case CharacterExportService.Section.Grade:
                    lines.Add(new CharLine { Section = charSection, Search = pack.GradeName + " " + pack.GradeId });
                    break;
                case CharacterExportService.Section.Attributes:
                    for (int i = 0; i < pack.Attributes.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Attributes[i].Name + " " + pack.Attributes[i].Key });
                    }
                    break;
                case CharacterExportService.Section.Martials:
                    for (int i = 0; i < pack.Martials.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Martials[i].Name + " " + pack.Martials[i].BaseId });
                    }
                    break;
                case CharacterExportService.Section.Arts:
                    for (int i = 0; i < pack.Arts.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Arts[i].Name + " " + pack.Arts[i].Id + " " + pack.Arts[i].KindName });
                    }
                    break;
                case CharacterExportService.Section.Lucks:
                    for (int i = 0; i < pack.BornLucks.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Born = true, Search = pack.BornLucks[i].Name + " " + pack.BornLucks[i].Id });
                    }
                    for (int i = 0; i < pack.AddLucks.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.AddLucks[i].Name + " " + pack.AddLucks[i].Id });
                    }
                    break;
                default:
                    for (int i = 0; i < pack.Fates.Count; i++)
                    {
                        lines.Add(new CharLine { Section = charSection, Index = i, Search = pack.Fates[i].Name + " " + pack.Fates[i].Id });
                    }
                    break;
            }
            return charItemQuery.Length == 0 ? lines : lines.FindAll(l => Matches(l.Search, charItemQuery));
        }

        // 줄에서 가장 긴 글(이름이나 효과)의 높이로 줄 높이를 정한다.
        private float CharLineHeight(CharLine line)
        {
            CharacterExportService.Pack pack = charOpened;
            switch (line.Section)
            {
                case CharacterExportService.Section.Martials:
                    return RowFor(38, Ui.TextHeight(pack.Martials[line.Index].Name, 330, 17));
                case CharacterExportService.Section.Arts:
                    return RowFor(38, Ui.TextHeight(pack.Arts[line.Index].Name, 400, 17));
                case CharacterExportService.Section.Lucks:
                    {
                        CharacterExportService.LuckLine luck = line.Born ? pack.BornLucks[line.Index] : pack.AddLucks[line.Index];
                        return RowFor(38, Ui.TextHeight(luck.Name, 240, 17), Ui.TextHeight(LuckTips(luck.Id), 560, 15));
                    }
                case CharacterExportService.Section.Fates:
                    return RowFor(38, Ui.TextHeight(pack.Fates[line.Index].Name, 240, 17), Ui.TextHeight(LuckTips(pack.Fates[line.Index].Id), 480, 15));
                default:
                    return 38;
            }
        }

        // 하위 화면: 한 묶음의 구역들. 구역을 고르면 그 줄들이 나오고, 줄마다 또는 구역 전체를 가져온다.
        private void DrawCharacterDetail()
        {
            CharacterExportService.Pack pack = charOpened;
            float y = SubTop;
            var sections = (CharacterExportService.Section[])Enum.GetValues(typeof(CharacterExportService.Section));
            for (int i = 0; i < sections.Length; i++)
            {
                CharacterExportService.Section section = sections[i];
                Ui.Button("CharSec" + section, content, SectionLabel(section, pack), i * 156, y, 150, 38, new Action(() =>
                {
                    charSection = section;
                    charItemQuery = "";
                    charDetail.Reset();
                    Redraw();
                }), true, charSection == section);
            }
            SearchBox.Build(content, "CharItemQuery", 0, y + 46, 330, charItemQuery, "이름의 일부 또는 번호", new Action<string>(v =>
            {
                charItemQuery = v;
                charDetail.SetItems(CharLines());
            }));
            bool rewrites = charSection == CharacterExportService.Section.Grade || charSection == CharacterExportService.Section.Attributes;
            CharacterExportService.Section current = charSection;
            Action importSection = new Action(() => Say(CharacterExportService.ImportSection(pack, current)));
            if (rewrites)
            {
                // 값이 파일의 값으로 바뀌는 구역은 한 번 더 확인한다.
                Confirm.Button("CharImportSection", content, "이 항목 모두 가져오기", "정말 가져오기", 350, y + 46, 240, 38, "char-sec:" + current + pack.File,
                    current == CharacterExportService.Section.Grade
                        ? "경지를 파일의 값으로 바꿉니다. 능력치는 따라오지 않습니다(능력치도 가져와야 원래 인물과 같아집니다). \"정말 가져오기\"를 한 번 더 누르세요."
                        : "능력치를 모두 파일의 값으로 바꿉니다. \"정말 가져오기\"를 한 번 더 누르세요.", importSection);
            }
            else
            {
                Ui.Button("CharImportSection", content, "이 항목 모두 가져오기", 350, y + 46, 240, 38, importSection);
            }
            Ui.Wrapped(content, rewrites ? "값은 파일의 값으로 바뀝니다. 대상은 플레이어입니다." : "지금 것에 더합니다(이미 있는 것은 건너뜁니다). 가져온 공법은 자동으로 장착하지 않습니다.",
                606, y + 46, ContentW - 606, 38, 14).color = Ui.Dim;
            float top = y + 94;
            charDetail.EmptyText = "이 항목에는 줄이 없습니다.";
            charDetail.HeightOf = CharLineHeight;
            charDetail.Build(content, 0, top, ContentW, ContentH - top, CharLines(), DrawCharLine);
        }

        // 상태 글과 가져오기 버튼. 줄을 쓸 수 없으면 이유를 보이고 버튼을 끈다.
        private Text LineEnd(RectTransform row, string name, string state, string problem, string label, bool enabled, Action action, bool confirm = false, string key = null, string message = null)
        {
            float right = ContentW - Ui.ScrollBarWidth;
            Text text = Ui.Wrapped(row, problem ?? state, right - 330, 0, 190, row.sizeDelta.y, 15);
            text.color = problem != null ? Ui.Warn : Ui.Dim;
            if (confirm)
            {
                Confirm.Button(name, row, label, "정말 가져오기", right - 138, 3, 130, 32, key, message, action, enabled && problem == null);
            }
            else
            {
                Ui.Button(name, row, label, right - 138, 3, 130, 32, action, enabled && problem == null);
            }
            return text;
        }

        private void DrawCharLine(RectTransform row, CharLine line, int index)
        {
            CharacterExportService.Pack pack = charOpened;
            float h = row.sizeDelta.y;
            int at = line.Index;
            switch (line.Section)
            {
                case CharacterExportService.Section.Grade:
                    {
                        var saved = Player.data.unitData.propertyData;
                        Ui.Label(row, "파일", 8, 0, 60, h, 16).color = Ui.Dim;
                        Ui.Label(row, pack.HasGrade ? EditService.GradeName(pack.GradeId) + " (번호 " + pack.GradeId + ", 수련치 " + pack.GradeExp + ")" : "경지 없음", 70, 0, 420, h, 17);
                        Ui.Label(row, "지금", 500, 0, 60, h, 16).color = Ui.Dim;
                        Ui.Label(row, EditService.GradeName(saved.gradeID) + " (번호 " + saved.gradeID + ", 수련치 " + saved.exp + ")", 562, 0, 380, h, 17);
                        bool same = pack.HasGrade && saved.gradeID == pack.GradeId;
                        LineEnd(row, "CharGetGrade", same ? "같은 경지" : "", pack.HasGrade ? null : "파일에 경지가 없습니다", "가져오기", true,
                            new Action(() => Say(CharacterExportService.ImportGrade(pack))), true, "char-grade:" + pack.File,
                            "경지를 " + EditService.GradeName(pack.GradeId) + "(으)로 바꿉니다. 능력치는 따라오지 않습니다(능력치도 가져와야 원래 인물과 같아집니다). \"정말 가져오기\"를 한 번 더 누르세요.");
                        break;
                    }
                case CharacterExportService.Section.Attributes:
                    {
                        CharacterExportService.Attr attr = pack.Attributes[at];
                        AttributeDef def = AttributeCatalog.All.Find(d => d.Key == attr.Key);
                        int now;
                        bool known = CharacterExportService.AttrNow(attr, out now);
                        Ui.Label(row, def == null ? "-" : AttributeCatalog.Categories[def.Category], 8, 0, 130, h, 16).color = Ui.Dim;
                        Ui.Label(row, def == null ? (attr.Name.Length > 0 ? attr.Name : attr.Key) : def.Label, 145, 0, 240, h, 17);
                        Ui.Label(row, "파일 " + attr.Value, 400, 0, 180, h, 17);
                        Ui.Label(row, known ? "지금 " + now : "", 590, 0, 180, h, 17);
                        bool same = known && now == attr.Value;
                        LineEnd(row, "CharGetAttr_" + attr.Key, same ? "같음" : "", attr.Problem ?? (known ? null : "모르는 능력치입니다"), same ? "같음" : "가져오기", !same,
                            new Action(() => Say(CharacterExportService.ImportAttribute(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Martials:
                    {
                        CharacterExportService.MartialLine martial = pack.Martials[at];
                        bool known = CharacterExportService.HasMartial(martial.BaseId);
                        Ui.Label(row, MartialService.TypeLabel((MartialType)martial.Type), 8, 0, 110, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, martial.Name, 125, 0, 330, h, 17);
                        Ui.Label(row, "경지 " + martial.Grade, 465, 0, 70, h, 16);
                        Ui.Label(row, "숙련 " + martial.Exp, 540, 0, 110, h, 16);
                        Ui.Label(row, "옵션 " + CharacterExportService.OptionCount(martial), 655, 0, 80, h, 16);
                        Ui.Label(row, martial.Equipped ? "장착했던 것" : "", 740, 0, 110, h, 15).color = Ui.Warn;
                        LineEnd(row, "CharGetMartial" + at, known ? "있음" : "없음", martial.Problem, known ? "하나 더" : "가져오기", true,
                            new Action(() => Say(CharacterExportService.ImportMartial(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Arts:
                    {
                        CharacterExportService.ArtLine art = pack.Arts[at];
                        bool learned = CharacterExportService.ArtLearned(art);
                        Ui.Label(row, art.KindName, 8, 0, 110, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, art.Name, 125, 0, 400, h, 17);
                        Ui.Label(row, art.Id.ToString(), 540, 0, 120, h, 16).color = Ui.Dim;
                        LineEnd(row, "CharGetArt" + at, learned ? "배움" : "-", CharacterExportService.Why(art), learned ? "배움" : "가져오기", !learned,
                            new Action(() => Say(CharacterExportService.ImportArt(pack, at))));
                        break;
                    }
                case CharacterExportService.Section.Lucks:
                    {
                        CharacterExportService.LuckLine luck = line.Born ? pack.BornLucks[at] : pack.AddLucks[at];
                        bool has = LuckService.Has(Player, luck.Id);
                        Ui.Label(row, line.Born ? "선천" : "후천", 8, 0, 70, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, luck.Name, 85, 0, 240, h, 17);
                        Ui.Wrapped(row, LuckTips(luck.Id), 335, 0, 560, h, 15).color = Ui.Dim;
                        bool born = line.Born;
                        LineEnd(row, (born ? "CharGetBorn" : "CharGetAdd") + at, has ? "보유" : "-", has ? null : CharacterExportService.Why(luck), has ? "보유" : "가져오기", !has,
                            new Action(() => Say(born ? CharacterExportService.ImportBornLuck(pack, at) : CharacterExportService.ImportAddLuck(pack, at))));
                        break;
                    }
                default:
                    {
                        CharacterExportService.FateLine fate = pack.Fates[at];
                        string state = CharacterExportService.FateState(fate);
                        Ui.Label(row, "대경지 " + fate.Grade, 8, 0, 150, h, 16).color = Ui.Dim;
                        Ui.Wrapped(row, fate.Name, 165, 0, 240, h, 17);
                        Ui.Wrapped(row, LuckTips(fate.Id), 415, 0, 480, h, 15).color = Ui.Dim;
                        // 상태 글에는 이름을 붙인다(검사가 "경지 미달"을 읽는다).
                        LineEnd(row, "CharGetFate" + at, state, CharacterExportService.Why(fate), state == "같음" ? "같음" : "가져오기", state == "다름",
                            new Action(() => Say(CharacterExportService.ImportFate(pack, at)))).gameObject.name = "CharFateState" + at;
                        break;
                    }
            }
        }
    }
}
