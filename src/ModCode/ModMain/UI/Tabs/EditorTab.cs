using UnityEngine;

namespace MOD_sWP6UL
{
    // 편집기 탭의 기반. 탭은 실행 중 한 번 만들어지고 자기 상태(선택, 검색어, 쪽)를 필드로 갖는다.
    // 버튼 동작 뒤에는 Say나 Refresh를 불러 내용을 다시 그린다(틀이 내용 영역을 지우고 Draw를 부른다).
    internal abstract class EditorTab
    {
        protected const float ContentW = EditorWindow.ContentW;
        protected const float ContentH = EditorWindow.ContentH;

        // 내용을 그릴 영역. 좌표는 이 영역의 왼쪽 위가 원점이다.
        protected RectTransform content;

        public abstract string Title { get; }

        // 편집 대상(플레이어 또는 NPC)에 적용되는 탭인지. 탭 줄에 표시가 붙는다.
        public virtual bool UsesTarget
        {
            get { return false; }
        }

        public void Render(RectTransform area)
        {
            content = area;
            Draw();
        }

        protected abstract void Draw();

        // 편집기를 열 때마다(모든 탭)
        public virtual void OnOpen()
        {
        }

        // 이 탭으로 들어올 때, 이 탭을 떠날 때
        public virtual void OnShow()
        {
        }

        public virtual void OnHide()
        {
        }

        // 편집 대상이 바뀌었을 때(모든 탭). 대상에 딸린 선택을 지운다.
        public virtual void OnTargetChanged()
        {
        }

        protected static WorldUnitBase Player
        {
            get { return EditorWindow.Player; }
        }

        protected static WorldUnitBase Target
        {
            get { return EditorWindow.Target; }
        }

        // 결과 문구를 보이고 다시 그린다.
        protected static void Say(string text)
        {
            EditorWindow.Say(text);
        }

        // 결과 문구는 그대로 두고 다시 그린다.
        protected static void Refresh()
        {
            EditorWindow.Refresh();
        }

        // 결과 문구를 지우고 다시 그린다.
        protected static void Redraw()
        {
            EditorWindow.ClearMessage();
            EditorWindow.Refresh();
        }

        protected static string Shorten(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }
    }
}
