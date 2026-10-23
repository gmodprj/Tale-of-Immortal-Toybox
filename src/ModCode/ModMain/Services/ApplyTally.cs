using System;
using System.Collections.Generic;
using System.Text;

namespace MOD_sWP6UL
{
    // 여러 줄을 넣는 동작(프리셋 적용, 캐릭터 가져오기)의 한 구역 집계. 두 곳이 같은 코드를 따로 갖던 것을 모았다(10 문서 R1, 묶음 5).
    internal enum ApplyOutcome { Done, Skipped, Failed }

    internal delegate ApplyOutcome ApplyPut(out string text);

    // "공법: 장착 3, 건너뜀 1, 못 넣음 1(이름: 이유)." 같은 문장이 된다.
    internal sealed class ApplyTally
    {
        public int Done, Skipped;
        public readonly List<string> Failed = new List<string>();

        public bool Any
        {
            get { return Done + Skipped + Failed.Count > 0; }
        }

        // 줄 하나를 넣고 결과를 센다. 실패하면 줄이 돌려준 문장(이름: 이유)을, 없으면 label을 남긴다. 예외는 실패로 센다.
        public void Count(string label, ApplyPut put)
        {
            ApplyOutcome outcome;
            string text;
            try
            {
                outcome = put(out text);
            }
            catch (Exception e)
            {
                outcome = ApplyOutcome.Failed;
                text = label + ": 게임이 받지 않았습니다(" + e.GetType().Name + ")";
            }
            if (outcome == ApplyOutcome.Done)
            {
                Done++;
            }
            else if (outcome == ApplyOutcome.Skipped)
            {
                Skipped++;
            }
            else
            {
                Failed.Add(string.IsNullOrEmpty(text) ? label : text.TrimEnd('.'));
            }
        }

        public string Sentence(string title, string verb)
        {
            var sb = new StringBuilder(title + ": " + verb + " " + Done + ", 건너뜀 " + Skipped);
            if (Failed.Count > 0)
            {
                sb.Append(", 못 넣음 ").Append(Failed.Count).Append("(");
                for (int i = 0; i < Failed.Count && i < 5; i++)
                {
                    sb.Append(i == 0 ? "" : " / ").Append(Failed[i]);
                }
                sb.Append(Failed.Count > 5 ? " 외 " + (Failed.Count - 5) + "개)" : ")");
            }
            return sb.Append(".").ToString();
        }
    }
}
