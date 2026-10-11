using MelonLoader;

// 이 네임스페이스는 모드 ID(MOD_<ID>)이며 어셈블리 이름, 모드 에디터의 코드 네임스페이스와 같아야 한다.
// 다르면 게임이 <네임스페이스>.ModMain 타입을 찾지 못해 DLL 로드가 실패한다.
namespace MOD_TlTbx0
{
    public class ModMain
    {
        // 세이브에 진입할 때 호출된다. 한 실행 중 여러 번 호출될 수 있다.
        public void Init()
        {
            MelonLogger.Msg("[TlToybox] Init");
        }

        // 주 화면으로 돌아갈 때 호출되며, 이후 Init이 다시 호출된다.
        public void Destroy()
        {
        }
    }
}
