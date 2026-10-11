using System;
using MelonLoader;

// 이 네임스페이스는 모드 ID(MOD_<ID>)이며 어셈블리 이름, 모드 에디터의 코드 네임스페이스와 같아야 한다.
// 다르면 게임이 <네임스페이스>.ModMain 타입을 찾지 못해 DLL 로드가 실패한다.
namespace MOD_TlTbx0
{
    public class ModMain
    {
        private const int PollFrames = 30;
        private const int MaxPolls = 600;

        private TimerCoroutine pollTimer;
        private int pollCount;

        // 세이브에 진입할 때 호출된다. 한 실행 중 여러 번 호출될 수 있다.
        public void Init()
        {
            MelonLogger.Msg("[TlToybox] Init");
            StopPoll();
            pollCount = 0;
            pollTimer = g.timer.Frame(new Action(OnPoll), PollFrames, true);
        }

        // 주 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
            MelonLogger.Msg("[TlToybox] Destroy");
            StopPoll();
        }

        // Init 시점에 세계가 준비되어 있는지 알 수 없으므로, 준비될 때까지 기다렸다가 탐침을 한 번 실행한다.
        private void OnPoll()
        {
            pollCount++;
            bool ready = g.world != null && g.world.isIntoWorld && g.world.playerUnit != null;
            if (!ready && pollCount < MaxPolls)
            {
                return;
            }
            StopPoll();
            if (!ready)
            {
                MelonLogger.Msg("[TlToybox] world not ready after " + pollCount + " polls");
                return;
            }
            MelonLogger.Msg("[TlToybox] world ready after " + pollCount + " polls");
            ReadOnlyProbe.Run();
        }

        private void StopPoll()
        {
            if (pollTimer != null)
            {
                g.timer.Stop(pollTimer);
                pollTimer = null;
            }
        }
    }
}
