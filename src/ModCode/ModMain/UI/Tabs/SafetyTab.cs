using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 안전 탭: 세이브 백업, 변경 이력(지난 실행의 기록 포함).
    internal sealed class SafetyTab : EditorTab
    {
        private const int MaxLines = 2000;
        private const float RowH = 30;
        private const float DetailH = 70;

        private readonly ListView<string> list = new ListView<string>("History", RowH);
        private readonly DetailPane detail = new DetailPane("HistoryDetail", "줄을 누르면 여기에 전체가 나옵니다.");
        private string query = "";
        private List<string> lines;   // 최신이 앞. null이면 다시 읽는다
        private string readError;

        public override string Title
        {
            get { return "안전"; }
        }

        public SafetyTab()
        {
            list.EmptyText = "조건에 맞는 기록이 없습니다.";
            list.OnSelect = line => detail.Show(line);
        }

        // 탭으로 들어올 때마다 파일을 다시 읽는다(다른 탭에서 한 변경이 들어와야 한다).
        public override void OnShow()
        {
            lines = null;
            list.Reset();
            detail.Clear();
        }

        public override void OnOpen()
        {
            lines = null;
        }

        private void Load()
        {
            lines = ChangeLog.ReadHistory(MaxLines, out readError);
            if (readError == null)
            {
                return;
            }
            // 파일을 읽지 못하면 이번 실행의 기록(메모리)을 보인다.
            IList<ChangeLog.Entry> entries = ChangeLog.Entries;
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                lines.Add(entries[i].ToString());
            }
        }

        private List<string> Found()
        {
            return query.Length == 0 ? lines : lines.FindAll(line => line.Contains(query));
        }

        protected override void Draw()
        {
            if (lines == null)
            {
                Load();
            }
            Ui.Panel("BackupBox", content, 0, 0, ContentW, 104, Ui.Section);
            Ui.Label(content, "세이브 백업: " + (SaveBackup.LastBackupDir == null ? "이번 실행에서 아직 백업하지 않았습니다." : "완료"), 16, 8, 880, 34);
            if (SaveBackup.LastBackupDir != null)
            {
                Ui.Label(content, SaveBackup.LastBackupDir, 16, 40, 880, 28, 15).color = Ui.Dim;
            }
            Ui.Button("BackupNow", content, "지금 백업", 16, 62, 160, 36, new Action(() =>
            {
                string error = SaveBackup.BackupNow();
                Say(error ?? "세이브를 백업했습니다.");
            }));
            Ui.Label(content, "값을 처음 바꾸기 직전에 자동으로 한 번 백업합니다. 백업은 디스크에 저장된 세이브의 사본입니다.", 190, 64, 720, 32, 16).color = Ui.Dim;

            Ui.Label(content, "변경 이력", 0, 114, 110, 38, 20);
            SearchBox.Build(content, "HistoryQuery", 110, 114, 330, query, "대상이나 항목의 일부", new Action<string>(v =>
            {
                query = v;
                detail.Clear();
                list.SetItems(Found());
            }));
            Text note = Ui.Label(content, readError == null ? "지난 실행의 기록을 포함해 최근 " + MaxLines + "줄까지, 최신이 위입니다."
                : "기록 파일을 읽지 못해 이번 실행의 기록만 보입니다(" + readError + ").", 460, 114, 840, 38, 15);
            note.color = readError == null ? Ui.Dim : Ui.Warn;

            list.Build(content, 0, 158, ContentW, ContentH - 158 - DetailH - 8, Found(), DrawRow);
            detail.Build(content, 0, ContentH - DetailH, ContentW, DetailH);
        }

        private void DrawRow(RectTransform row, string line, int index)
        {
            Text text = Ui.Label(row, Shorten(line, 92), 8, 0, ContentW - 40, RowH, 16);
            // 쓴 뒤 다시 읽은 값이 달랐던 기록
            if (line.Contains("(재조회 불일치)"))
            {
                text.color = Ui.Warn;
            }
        }
    }
}
