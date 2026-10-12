using System;
using MelonLoader;

// 이 네임스페이스는 모드 ID(MOD_<ID>)이며 어셈블리 이름, 모드 에디터의 코드 네임스페이스와 같아야 한다.
// 다르면 게임이 <네임스페이스>.ModMain 타입을 찾지 못해 DLL 로드가 실패한다.
namespace MOD_sWP6UL
{
    public class ModMain
    {
        // 모드가 로드될 때 호출된다(실측: 세이브를 불러오기 전). 한 실행 중 여러 번 호출될 수 있다.
        public void Init()
        {
            MelonLogger.Msg("[TlToybox] Init");
            EditorHost.Ensure();
#if DEVTOOLS
            StartDevPoll();
#endif
        }

        // 주 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
            MelonLogger.Msg("[TlToybox] Destroy");
            EditorWindow.Close();
#if DEVTOOLS
            StopDevPoll();
#endif
        }

#if DEVTOOLS
        // 개발 전용: 세계에 들어가면 읽기 탐침을 한 번 실행하고, 이후 파일 기반 명령 채널을 감시한다.
        private const int PollFrames = 60;

        private TimerCoroutine pollTimer;
        private string lastState;
        private bool probed;

        private void StartDevPoll()
        {
            StopDevPoll();
            lastState = null;
            pollTimer = g.timer.Frame(new Action(OnDevPoll), PollFrames, true);
        }

        private void StopDevPoll()
        {
            if (pollTimer != null)
            {
                g.timer.Stop(pollTimer);
                pollTimer = null;
            }
        }

        private void OnDevPoll()
        {
            string state = EditService.WorldReady ? "ready" : "not ready";
            if (state != lastState)
            {
                MelonLogger.Msg("[TlToybox] state: " + state);
                lastState = state;
            }
            if (state != "ready")
            {
                probed = false;
                return;
            }
            if (!probed)
            {
                probed = true;
                ReadOnlyProbe.Run();
            }
            DevCommands.Poll();
        }
#endif
    }
}
