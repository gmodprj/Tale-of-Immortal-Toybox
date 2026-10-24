using System;
using System.Collections.Generic;
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

        // 하위 화면에서 내용이 시작하는 y. 그 위는 제목과 돌아가기다.
        protected const float SubTop = 44;

        private sealed class SubView
        {
            public Func<string> Title;
            public Action Draw;
        }

        private readonly List<SubView> views = new List<SubView>();

        public void Render(RectTransform area)
        {
            content = area;
            if (views.Count == 0)
            {
                Draw();
                return;
            }
            SubView view = views[views.Count - 1];
            Ui.Label(content, view.Title(), 0, 0, ContentW - 150, 36, 21).gameObject.name = "SubTitle";
            Ui.Button("Back", content, "돌아가기", ContentW - 130, 0, 130, 36, new Action(Pop));
            view.Draw();
        }

        // 하위 화면을 쌓는다(목록 → 고르기 같은 흐름). draw는 SubTop 아래에 그린다.
        protected void Push(Func<string> title, Action draw)
        {
            views.Add(new SubView { Title = title, Draw = draw });
            Redraw();
        }

        // 한 단계 돌아간다.
        protected void Pop()
        {
            if (views.Count > 0)
            {
                views.RemoveAt(views.Count - 1);
            }
            Redraw();
        }

        protected void CloseViews()
        {
            views.Clear();
        }

        // 틀이 부른다: 이 탭을 떠날 때. 하위 화면을 닫는다.
        public void Leave()
        {
            CloseViews();
            OnHide();
        }

        // 틀이 부른다: 편집 대상이 바뀌었을 때. 하위 화면은 앞의 대상을 보고 있으므로 닫는다.
        public void TargetChanged()
        {
            CloseViews();
            OnTargetChanged();
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

        // 줄의 높이: 가장 높은 글에 위아래 여백을 더한 값. 글이 한 줄이면 min 그대로다.
        // 목록의 글은 자르지 않고 줄바꿈해 전부 보인다(사용자 지시, 2026-10-13).
        protected static float RowFor(float min, params float[] textHeights)
        {
            float tallest = 0f;
            foreach (float each in textHeights)
            {
                tallest = Math.Max(tallest, each);
            }
            return Math.Max(min, (float)Math.Ceiling(tallest) + 10f);
        }

        // 위에 이름 한 줄(26)이 있고 그 아래에 설명이 오는 줄의 높이.
        protected const float SecondLineY = 26;

        protected static float RowWithSecondLine(float min, float textHeight)
        {
            return Math.Max(min, SecondLineY + (float)Math.Ceiling(textHeight) + 6f);
        }

        // 버튼의 글을 버튼 너비 안에서 줄바꿈한다.
        protected static UnityEngine.UI.Button WrapLabel(UnityEngine.UI.Button button, int size = 0)
        {
            UnityEngine.UI.Text text = button.GetComponentInChildren<UnityEngine.UI.Text>();
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            if (size > 0)
            {
                text.fontSize = size;
            }
            return button;
        }
    }
}
