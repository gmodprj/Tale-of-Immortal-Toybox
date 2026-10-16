using System;
using System.IO;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 디스크의 세이브 폴더를 통째로 복사해 둔다. 편집기는 한 실행에서 첫 쓰기를 하기 전에 한 번 백업한다.
    internal static class SaveBackup
    {
        private static string lastBackupDir;

        public static string LastBackupDir
        {
            get { return lastBackupDir; }
        }

        private static string SaveDir
        {
            get { return Path.Combine(Path.Combine(Application.persistentDataPath, "Steam"), "CacheData"); }
        }

        // 이번 실행에서 아직 백업하지 않았으면 백업한다. 실패하면 사유를 돌려주고, 성공하면 null을 돌려준다.
        public static string EnsureOnce()
        {
            return lastBackupDir != null ? null : BackupNow();
        }

#if DEVTOOLS
        // 개발 전용: 한 실행 안에서 "첫 쓰기 전 백업"을 여러 번 보려고 백업 기록을 지운다(검사 backup).
        public static void DevReset()
        {
            lastBackupDir = null;
        }
#endif

        public static string BackupNow()
        {
            try
            {
                if (!Directory.Exists(SaveDir))
                {
                    return "세이브 폴더를 찾지 못했습니다: " + SaveDir;
                }
                string target = Path.Combine(Path.Combine(ChangeLog.DataDir, "backups"), DateTime.Now.ToString("yyyyMMdd-HHmmss"));
                CopyDirectory(SaveDir, Path.Combine(target, "CacheData"));
                lastBackupDir = target;
                return null;
            }
            catch (Exception e)
            {
                return "백업 실패: " + e.Message;
            }
        }

        private static void CopyDirectory(string from, string to)
        {
            Directory.CreateDirectory(to);
            foreach (string file in Directory.GetFiles(from))
            {
                File.Copy(file, Path.Combine(to, Path.GetFileName(file)), true);
            }
            foreach (string dir in Directory.GetDirectories(from))
            {
                CopyDirectory(dir, Path.Combine(to, Path.GetFileName(dir)));
            }
        }
    }
}
