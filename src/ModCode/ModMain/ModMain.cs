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
            // 어느 코드가 설치됐는지 Latest.log만으로 알 수 있게 빌드 표지를 찍는다(개발 빌드는 .devremote 경로도). 05 문서 49절.
            MelonLogger.Msg("[TlToybox] Init build=" + BuildInfo.Commit + DevRemoteNote());
            var total = System.Diagnostics.Stopwatch.StartNew();
            long patchMs = -1;
            EditorHost.Ensure();
            try
            {
                TranslationService.Apply();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] translations failed: " + e.GetType().Name + ": " + e.Message);
            }
            try
            {
                // LocalText 밖 중국어 글(05 문서 53·54절): 표 칸 바꿔 끼우기 뒤 화면 글 치환을 건다.
                TextTranslationService.Apply();
                long patchStart = total.ElapsedMilliseconds;
                TextPatch.Ensure();
                patchMs = total.ElapsedMilliseconds - patchStart;
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] texts failed: " + e.GetType().Name + ": " + e.Message);
            }
            try
            {
                BattleService.OnInit();
            }
            catch (Exception e)
            {
                // 전투 사건 등록이 실패해도 뒤의 초기화는 돈다(13 문서 A3).
                MelonLogger.Msg("[TlToybox] battle events failed: " + e.GetType().Name + ": " + e.Message);
            }
            long dressStart = total.ElapsedMilliseconds;
            try
            {
                // 캐릭터 만들기 의복 목록의 빈 썸네일 채우기(05 문서 74절).
                DressThumbService.Ensure();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] dress thumbs failed: " + e.GetType().Name + ": " + e.Message);
            }
            long dressMs = total.ElapsedMilliseconds - dressStart;
            long gridStart = total.ElapsedMilliseconds;
            try
            {
                // 기술 학습 상한(05 문서 60절): 설정 파일의 값이 0이 아니면 패치를 건다.
                MartialGridService.Ensure();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] martial grid failed: " + e.GetType().Name + ": " + e.Message);
            }
            long gridMs = total.ElapsedMilliseconds - gridStart;
            try
            {
                // 극 대화 창의 빠진 배경 그림을 흰 판 대신 검은 바탕으로(05 문서 95절).
                DramaBackgroundService.Ensure();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] drama background failed: " + e.GetType().Name + ": " + e.Message);
            }
            // 단계별 시간(성능 1 스펙 4.5의 1). 번역 두 단계의 시간은 translations:·texts: 줄에 있다.
            MelonLogger.Msg("[TlToybox] init ms=" + total.ElapsedMilliseconds + " dress=" + dressMs + " grid=" + gridMs + " patch=" + patchMs);
#if DEVTOOLS
            StartDevPoll();
#endif
        }

        private static string DevRemoteNote()
        {
#if DEVTOOLS
            return " devremote=" + DevCommands.Dir + (DevCommands.DirFromEnv ? " (env)" : " (default)");
#else
            return "";
#endif
        }

        // 주 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
            MelonLogger.Msg("[TlToybox] Destroy");
            EditorWindow.Close();
            ResetState();
            try
            {
                BattleService.OnDestroy();
            }
            catch (Exception e)
            {
                // 사건 해제가 던져도 뒤의 정리(배율·입력·포인터 되돌리기)는 돈다(13 문서 A3).
                MelonLogger.Msg("[TlToybox] battle events failed: " + e.GetType().Name + ": " + e.Message);
            }
            DressThumbService.Stop();
            RateService.OnDestroy();
            ArtifactKeyService.OnDestroy();
            InputGuard.Release();
            PointerGuard.Release();
            TextPatch.ResetStats();
            AppellationService.ForgetNames();
            AppellationService.ForgetQuiet();
            DramaBackgroundService.Forget();
#if DEVTOOLS
            StopDevPoll();
#endif
        }

        // 세이브를 떠날 때 켜 둔 것(날짜 고정·배율·무적)을 끈다. 다음 세이브가 그 상태로 시작하지 않게(리뷰 C3, 05 문서 48절). 이력에 남는다.
        // 예외가 나도 뒤의 정리(OnDestroy)는 돈다.
        public static void ResetState()
        {
            try
            {
                if (TimeService.Frozen)
                {
                    TimeService.SetFrozen(false);
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 날짜 고정 되돌리기 오류: " + e.GetType().Name + ": " + e.Message);
            }
            try
            {
                RateService.Reset();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 배율 되돌리기 오류: " + e.GetType().Name + ": " + e.Message);
            }
            try
            {
                // 성공·드롭 탭(훔치기·관계 발전·드롭 배수·참오 최대)도 다음 세이브로 넘기지 않는다(13 문서 A1).
                SuccessService.Reset();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 성공·드롭 되돌리기 오류: " + e.GetType().Name + ": " + e.Message);
            }
            try
            {
                // 하위 화면(강화·기술 추가·대도까지·전승 등)과 결과 문구는 앞의 세이브를 보고 있으므로 닫는다(13 문서 B8).
                EditorWindow.ResetViews();
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 하위 화면 닫기 오류: " + e.GetType().Name + ": " + e.Message);
            }
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
                // 세계 밖(메인·캐릭터 만들기 화면)에서도 명령 파일을 받는다(05 문서 52절의 의복 목록 확인). 세계가 필요한 명령은 ERROR로 돌아온다.
                DevCommands.Poll();
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
