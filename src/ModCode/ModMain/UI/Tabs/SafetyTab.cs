using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 안전 탭: 세이브 백업, 변경 이력.
    internal sealed class SafetyTab : EditorTab
    {
        private const int HistoryRows = 14;

        public override string Title
        {
            get { return "안전"; }
        }

        protected override void Draw()
        {
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

            IList<ChangeLog.Entry> entries = ChangeLog.Entries;
            Ui.Label(content, "변경 이력 (이번 실행, 최근 " + HistoryRows + "건 / 전체 " + entries.Count + "건)", 0, 116, 880, 32, 20);
            int shown = Math.Min(HistoryRows, entries.Count);
            for (int i = 0; i < shown; i++)
            {
                ChangeLog.Entry entry = entries[entries.Count - 1 - i];
                Ui.Label(content, entry.ToString(), 0, 152 + i * 30, 900, 28, 17).color = entry.Verified ? Color.white : Ui.Warn;
            }
            if (entries.Count == 0)
            {
                Ui.Label(content, "아직 변경한 것이 없습니다.", 0, 152, 880, 30, 18).color = Ui.Dim;
            }
        }
    }
}
