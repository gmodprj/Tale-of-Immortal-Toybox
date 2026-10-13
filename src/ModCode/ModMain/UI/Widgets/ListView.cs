using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 세로로 스크롤되는 목록. 전체 높이만큼의 영역에 화면에 걸친 줄만 만든다(아이템 3천여 개를 줄마다 만들면 너무 느리다).
    // 스크롤해서 보이는 줄이 바뀌면 그때 줄을 다시 만든다. 목록 위치는 이 객체가 기억하므로 탭이 내용을 다시 그려도 유지된다.
    // 줄의 높이는 기본이 같은 값이고, HeightOf를 주면 항목마다 다르다(긴 글을 줄바꿈해 전부 보이는 줄).
    // 아래에 이동 줄이 붙는다: 처음·이전·다음·끝과 "전체 N개 중 a~b번째". 전체가 한 화면에 들어오면 버튼을 숨긴다.
    internal sealed class ListView<T>
    {
        private const float NavH = 38;
        private const float Gap = 6;
        private static readonly Color Stripe = new Color(1f, 1f, 1f, 0.04f);
        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        private readonly string name;
        private readonly float rowHeight;
        private IList<T> items = new List<T>();
        private float[] tops = new float[1];    // tops[i]는 i번째 줄의 위쪽, tops[Count]는 전체 높이
        private Action<RectTransform, T, int> drawRow;
        private RectTransform inner;
        private RectTransform nav;
        private float viewH;
        private float innerW;
        private float scrollY;
        private int first = -1;
        private int drawnEnd;                   // 만들어 둔 줄의 끝(이 번호 앞까지)

        // 줄을 눌렀을 때 부를 것. 있으면 줄 전체가 눌린다(줄 위의 버튼은 따로 눌린다).
        public Action<T> OnSelect { get; set; }

        // 항목마다의 줄 높이. 없으면 모든 줄이 생성자에 준 높이다.
        public Func<T, float> HeightOf { get; set; }

        public string EmptyText = "조건에 맞는 것이 없습니다.";

        public ListView(string name, float rowHeight)
        {
            this.name = name;
            this.rowHeight = rowHeight;
        }

        public int Count
        {
            get { return items.Count; }
        }

        private float Total
        {
            get { return tops[items.Count]; }
        }

        // 다음에 그릴 때 맨 위에서 시작한다(분류를 바꿨을 때).
        public void Reset()
        {
            scrollY = 0;
        }

        private void Measure()
        {
            tops = new float[items.Count + 1];
            for (int i = 0; i < items.Count; i++)
            {
                tops[i + 1] = tops[i] + (HeightOf != null ? HeightOf(items[i]) : rowHeight);
            }
        }

        // y가 걸친 줄의 번호.
        private int RowAt(float y)
        {
            int low = 0, high = items.Count - 1;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                if (tops[mid] <= y)
                {
                    low = mid;
                }
                else
                {
                    high = mid - 1;
                }
            }
            return Mathf.Max(0, low);
        }

        // 위쪽이 y보다 위에 있지 않은 첫 줄(잘리지 않고 보이는 첫 줄).
        private int FirstWholeFrom(float y)
        {
            int row = RowAt(y);
            return row < items.Count && tops[row] < y - 0.5f ? row + 1 : row;
        }

        // 아래쪽이 y보다 아래에 있지 않은 마지막 줄(잘리지 않고 보이는 마지막 줄). 없으면 from.
        private int LastWholeUntil(float y, int from)
        {
            int row = from;
            while (row + 1 < items.Count && tops[row + 2] <= y + 0.5f)
            {
                row++;
            }
            return row;
        }

        private float Clamp(float y)
        {
            return Mathf.Clamp(y, 0f, Mathf.Max(0f, Total - viewH));
        }

        // (x, y, w, h) 안에 목록과 이동 줄을 그린다. h에는 이동 줄의 높이가 들어 있다.
        public void Build(Transform parent, float x, float y, float w, float h, IList<T> list, Action<RectTransform, T, int> draw)
        {
            items = list;
            drawRow = draw;
            viewH = h - NavH - Gap;
            innerW = w - Ui.ScrollBarWidth;
            Measure();
            scrollY = Clamp(scrollY);
            inner = Ui.Scroll(name, parent, x, y, w, viewH, Total, scrollY, new Action<float>(OnScroll));
            nav = Ui.Rect(name + "Nav", parent, x, y + viewH + Gap, w, NavH);
            DrawRows();
        }

        // 걸러진 결과로 바꾸고 맨 위에서 시작한다. 줄만 다시 그리므로 검색 칸이 그대로 남는다.
        public void SetItems(IList<T> list)
        {
            items = list;
            scrollY = 0;
            Measure();
            if (inner == null)
            {
                return;
            }
            inner.sizeDelta = new Vector2(innerW, Mathf.Max(viewH, Total));
            inner.anchoredPosition = Vector2.zero;
            DrawRows();
        }

        // 보이는 줄을 다시 그린다(줄의 내용이 바뀌었을 때).
        public void Refresh()
        {
            if (inner != null)
            {
                DrawRows();
            }
        }

        private void OnScroll(float y)
        {
            scrollY = y;
            // 첫 줄이 바뀌었거나, 만들어 둔 줄보다 아래가 보이게 됐으면 줄을 다시 만든다.
            if (RowAt(y) != first || (drawnEnd < items.Count && tops[drawnEnd] < y + viewH))
            {
                DrawRows();
                return;
            }
            // 줄이 그대로여도 맨 위·맨 아래를 벗어났는지는 달라졌을 수 있다. 이동 줄만 다시 그린다.
            DrawNav();
        }

        private void ScrollTo(float y)
        {
            scrollY = Clamp(y);
            inner.anchoredPosition = new Vector2(0f, scrollY);
            DrawRows();
        }

        private void DrawRows()
        {
            Ui.Clear(inner);
            first = RowAt(scrollY);
            if (items.Count == 0)
            {
                Ui.Label(inner, EmptyText, 16, 8, innerW - 32, 34, 18).color = Ui.Warn;
            }
            // 화면에 걸친 줄과 그 아래 한 줄까지 만든다.
            int i = first;
            for (; i < items.Count; i++)
            {
                float height = tops[i + 1] - tops[i];
                RectTransform row = Ui.Rect("Row" + i, inner, 0, tops[i], innerW, height);
                if (OnSelect != null || i % 2 == 1)
                {
                    var image = row.gameObject.AddComponent<Image>();
                    image.color = i % 2 == 1 ? Stripe : Clear;
                    if (OnSelect != null)
                    {
                        T item = items[i];
                        var button = row.gameObject.AddComponent<Button>();
                        button.targetGraphic = image;
                        button.transition = Selectable.Transition.None;
                        button.onClick.AddListener(new Action(() => OnSelect(item)));
                    }
                    else
                    {
                        image.raycastTarget = false;
                    }
                }
                drawRow(row, items[i], i);
                CenterButtons(row, height);
                if (tops[i] >= scrollY + viewH)
                {
                    i++;
                    break;
                }
            }
            drawnEnd = i;
            DrawNav();
        }

        // 줄에 바로 붙은 버튼을 줄의 세로 가운데로 옮긴다. 긴 글 때문에 줄이 높아져도 버튼이 위에 붙어 있지 않게 한다.
        // 줄 높이를 거의 다 채우는 버튼(아이템 이름처럼 줄 전체를 쓰는 것)은 그대로 둔다.
        private static void CenterButtons(RectTransform row, float height)
        {
            for (int i = 0; i < row.childCount; i++)
            {
                var child = row.GetChild(i).GetComponent<RectTransform>();
                if (child == null || child.GetComponent<Button>() == null || child.sizeDelta.y >= height - 8f)
                {
                    continue;
                }
                child.anchoredPosition = new Vector2(child.anchoredPosition.x, -(height - child.sizeDelta.y) / 2f);
            }
        }

        private void DrawNav()
        {
            Ui.Clear(nav);
            bool fits = Total <= viewH;
            float max = Clamp(float.MaxValue);
            bool atEnd = !fits && scrollY >= max - 0.5f;
            // 범위는 다 보이는 줄로 센다. 다만 맨 아래가 아니면 위가 잘린 첫 줄부터 센다(조금만 내렸을 때 번호가 건너뛰지 않게).
            int from = items.Count == 0 ? 0 : atEnd ? FirstWholeFrom(scrollY) : first;
            int to = items.Count == 0 ? 0 : LastWholeUntil(scrollY + viewH, from);
            float labelX = 0;
            if (!fits)
            {
                // 이전: 한 화면 위에서, 잘리지 않는 첫 줄에 맞춘다. 다음: 지금 다 보이지 않는 첫 줄에서 시작한다.
                float prev = tops[Mathf.Min(items.Count - 1, FirstWholeFrom(Mathf.Max(0f, scrollY - viewH)))];
                float next = tops[Mathf.Min(items.Count - 1, to + 1)];
                Ui.Button(name + "First", nav, "처음", 0, 0, 80, NavH, new Action(() => ScrollTo(0f)), scrollY > 0f);
                Ui.Button(name + "Prev", nav, "이전", 86, 0, 80, NavH, new Action(() => ScrollTo(prev)), scrollY > 0f);
                Ui.Button(name + "Next", nav, "다음", 172, 0, 80, NavH, new Action(() => ScrollTo(next)), scrollY < max);
                Ui.Button(name + "Last", nav, "끝", 258, 0, 80, NavH, new Action(() => ScrollTo(max)), scrollY < max);
                labelX = 350;
            }
            string range = items.Count == 0 ? "0개"
                : "전체 " + items.Count + "개 중 " + (from + 1) + "~" + (to + 1) + "번째";
            RectTransform holder = Ui.Rect(name + "Range", nav, labelX, 0, 600, NavH);
            Ui.Label(holder, range, 0, 0, 600, NavH, 16).color = Ui.Dim;
        }
    }
}
