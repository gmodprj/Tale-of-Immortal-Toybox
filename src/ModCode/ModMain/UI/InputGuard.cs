using System;
using System.Collections.Generic;
using System.Reflection;
using MelonLoader;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MOD_sWP6UL
{
    // 편집기의 입력 칸에 글자를 치는 동안 게임의 단축키(B 가방, X 공법 등)가 같이 반응하지 않게 한다(사용자 보고, 2026-10-14).
    // 게임은 키 설정(g.data.dataGloble.data.key, DataGloble.Key의 KeyCode 33개)을 그대로 읽어 키를 확인한다(신기 키를 메모리에서 바꾸면 바로 반영됐다).
    // 그래서 편집기의 입력 칸이 입력 상태인 동안 그 값들을 None으로 바꿔 두고, 벗어나면 되돌린다.
    // 게임 입력 끄기(InputBase.InputDisable)와 InputSDK.GetKeyDown 등의 패치로는 지도 단축키가 막히지 않았다(회차 typing-1014c·d).
    // 게임은 키 설정 창의 확인에서 이 값을 저장하는데, 그 창은 편집기의 입력 칸과 동시에 쓸 수 없으므로 None이 저장되지 않는다.
    internal static class InputGuard
    {
        private const string EditorName = "TlToyboxEditor";
        private static readonly List<KeyValuePair<PropertyInfo, KeyCode>> saved = new List<KeyValuePair<PropertyInfo, KeyCode>>();
        private static DataGloble.Key savedFrom;
        private static bool warned;

        public static bool Blocking
        {
            get { return savedFrom != null; }
        }

        public static void Tick()
        {
            try
            {
                bool typing = EditorTyping();
                if (typing && savedFrom == null)
                {
                    Block();
                }
                else if (!typing && savedFrom != null)
                {
                    Release();
                }
            }
            catch (Exception e)
            {
                if (!warned)
                {
                    warned = true;
                    MelonLogger.Msg("[TlToybox] 입력 칸의 게임 단축키 막기 오류: " + e.Message);
                }
            }
        }

        private static void Block()
        {
            DataGloble.Key key = g.data == null || g.data.dataGloble == null || g.data.dataGloble.data == null ? null : g.data.dataGloble.data.key;
            if (key == null)
            {
                return;
            }
            saved.Clear();
            foreach (PropertyInfo prop in typeof(DataGloble.Key).GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.PropertyType != typeof(KeyCode) || !prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }
                KeyCode value = (KeyCode)prop.GetValue(key, null);
                if (value == KeyCode.None)
                {
                    continue;
                }
                saved.Add(new KeyValuePair<PropertyInfo, KeyCode>(prop, value));
                prop.SetValue(key, KeyCode.None, null);
            }
            savedFrom = key;
        }

        // 막아 둔 키를 되돌린다. 편집기를 닫거나 모드가 내려갈 때도 불린다.
        public static void Release()
        {
            if (savedFrom == null)
            {
                return;
            }
            try
            {
                foreach (var pair in saved)
                {
                    // 막는 동안 다른 곳(신기 키 끄기 등)이 값을 바꿨으면 그 값을 존중한다.
                    if ((KeyCode)pair.Key.GetValue(savedFrom, null) == KeyCode.None)
                    {
                        pair.Key.SetValue(savedFrom, pair.Value, null);
                    }
                }
            }
            catch (Exception e)
            {
                MelonLogger.Msg("[TlToybox] 게임 단축키를 되돌리지 못했습니다: " + e.Message);
            }
            saved.Clear();
            savedFrom = null;
        }

        // 편집기 안의 입력 칸이 입력 상태(커서가 있음)인가.
        private static bool EditorTyping()
        {
            EventSystem es = EventSystem.current;
            GameObject selected = es == null ? null : es.currentSelectedGameObject;
            if (selected == null || !selected.activeInHierarchy)
            {
                return false;
            }
            InputField input = selected.GetComponent<InputField>();
            if (input == null || !input.isFocused)
            {
                return false;
            }
            for (Transform t = selected.transform; t != null; t = t.parent)
            {
                if (t.name == EditorName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
