using System;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 입화 몸 그림의 조사(05 문서 52절). 의상 표(RoleDress)와 몸 그림 프리팹을 읽기만 한다.
    //   rdress <id>                             RoleDress 한 줄(id·dressID·sex·type·className·name·active·weight·npcGrade·beauty)
    //   rdress list <sex> <type> [from] [to]    GetDressItem(sex, type)의 줄들. sex 1 남 2 여, type yifu 등. dressID가 from~to인 것만
    //   rbody <from> [to] [Woman|Man] [dyn]     Game/Portrait/<성별>/Body/<n>(dyn이면 PortraitDynamic)을 g.res로 읽어 프리팹과 그 안 그림의 상태
    //   rbodyof <이름 일부>                      이름이 맞는 유닛들의 입화 몸 번호(PortraitModelData.body)와 성별
    //   rprops <itemId>                         ConfItemPropsItem의 정수·실수·글자 속성 전부(의복 아이템과 몸 번호의 대응을 찾는 용도)
    //   rthumbs                                 의복 목록 빈 썸네일 채우기(DressThumbService)의 요청·적재·채움 수(74절)
    internal static class DevCommandsDress
    {
        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "rdress":
                    return p.Length > 1 && p[1] == "list" ? DressList(p) : Dress(int.Parse(p[1]));
                case "rbody":
                    return Bodies(p);
                case "rbodyq":
                    return QueueBodies(p);
                case "rbodyr":
                    return AsyncResults();
                case "rbodyof":
                    return BodyOf(p.Length > 1 && p[1] != "*" ? p[1] : "", p.Length > 2 ? int.Parse(p[2]) : int.MinValue,
                        p.Length > 3 ? int.Parse(p[3]) : int.MaxValue);
                case "rprops":
                    return Props(int.Parse(p[1]));
                case "rsetbody":
                    return SetPlayerBody(int.Parse(p[1]));
                case "rthumbs":
                    return p.Length > 1 ? DressThumbService.Describe(p[1]) : DressThumbService.Stats();
                case "ruigrid":
                    return UiGrid(p.Length > 1 ? p[1] : "srItems", p.Length > 2 && p[2] == "all");
                case "rnpcinfo":
                    return NpcInfo(p.Length > 1 ? p[1] : "", p.Length > 2 ? p[2] : "");
                case "ropenui":
                    return UiCall("OpenUI", p[1], false);
                case "rcloseui":
                    return UiCall("CloseUI", p[1], true);
                default:
                    return null;
            }
        }

        private static string Dress(int id)
        {
            ConfRoleDressItem row = g.conf.roleDress.GetItem(id);
            if (row == null)
            {
                return "no dress " + id;
            }
            return Row(row);
        }

        private static string Row(ConfRoleDressItem row)
        {
            return "id=" + row.id + " dressID=" + row.dressID + " sex=" + row.sex + " type=" + row.type + " className=" + row.className
                + " name=" + row.name + " active=" + row.active + " weight=" + row.weight + " npcGrade=" + row.npcGrade + " beauty=" + row.beauty;
        }

        private static string DressList(string[] p)
        {
            int sex = int.Parse(p[2]);
            string type = p[3];
            int from = p.Length > 4 ? int.Parse(p[4]) : int.MinValue;
            int to = p.Length > 5 ? int.Parse(p[5]) : int.MaxValue;
            var rows = g.conf.roleDress.GetDressItem(sex, type);
            if (rows == null)
            {
                return "no rows sex=" + sex + " type=" + type;
            }
            var sb = new StringBuilder();
            int shown = 0;
            for (int i = 0; i < rows.Count; i++)
            {
                ConfRoleDressItem row = rows[i];
                if (row == null || row.dressID < from || row.dressID > to)
                {
                    continue;
                }
                sb.Append(row.id).Append(':').Append(row.dressID).Append(':').Append(row.name).Append(":a").Append(row.active)
                    .Append(":w").Append(row.weight).Append(":g").Append(row.npcGrade)
                    .Append(":f").Append(row.flag).Append(":gf").Append(row.gamerFlag).Append(":c").Append(row.className).Append(":cg").Append(row.colorGroup).Append(';');
                shown++;
            }
            return "total=" + rows.Count + " shown=" + shown + " " + sb;
        }

        // 몸 그림 프리팹을 게임의 자원 적재로 읽는다(Fatury가 바꿔치기한 결과가 그대로 온다). 프리팹이 없으면 null,
        // 있으면 안의 SpriteRenderer·Image마다 스프라이트 이름과 텍스처 크기를 적는다. 8x8 텍스처는 깨진 그림이다.
        private static string Bodies(string[] p)
        {
            int from = int.Parse(p[1]);
            int to = p.Length > 2 ? int.Parse(p[2]) : from;
            string sex = p.Length > 3 ? p[3] : "Woman";
            bool dyn = p.Length > 4 && p[4] == "dyn";
            var sb = new StringBuilder();
            for (int n = from; n <= to; n++)
            {
                string path = (dyn ? "Game/PortraitDynamic/" : "Game/Portrait/") + sex + "/Body/" + n;
                sb.Append(n).Append('=');
                try
                {
                    GameObject go = g.res.Load<GameObject>(path);
                    if (go == null)
                    {
                        sb.Append("null");
                    }
                    else
                    {
                        sb.Append("go(").Append(go.transform.childCount).Append(')');
                        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
                        for (int i = 0; i < renderers.Length; i++)
                        {
                            sb.Append(' ').Append(Describe("sr", renderers[i].sprite));
                        }
                        var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                        for (int i = 0; i < images.Length; i++)
                        {
                            sb.Append(' ').Append(Describe("img", images[i].sprite));
                        }
                    }
                }
                catch (Exception e)
                {
                    sb.Append("ERROR ").Append(e.GetType().Name).Append(' ').Append(e.Message.Replace('\n', ' '));
                }
                sb.Append(';');
            }
            return sb.ToString();
        }

        // 게임이 입화 조각을 읽는 길(g.res.LoadAsync. Fatury가 Harmony로 패치하는 메서드)로 몸 그림을 읽어 둔다. 결과는 콜백이 오는 대로 쌓이고
        // rbodyr로 읽는다. 동기 Load(rbody)는 패치를 거치지 않아 원작 자산만 보이므로, Fatury가 바꿔치기한 결과는 이 길로만 보인다.
        private static readonly System.Collections.Generic.SortedDictionary<string, string> asyncResults = new System.Collections.Generic.SortedDictionary<string, string>();
        private static readonly System.Collections.Generic.List<Il2CppSystem.Action<UnityEngine.Object>> asyncHandlers = new System.Collections.Generic.List<Il2CppSystem.Action<UnityEngine.Object>>();

        private static string QueueBodies(string[] p)
        {
            int from = int.Parse(p[1]);
            int to = p.Length > 2 ? int.Parse(p[2]) : from;
            string sex = p.Length > 3 ? p[3] : "Woman";
            bool dyn = p.Length > 4 && p[4] == "dyn";
            asyncResults.Clear();
            asyncHandlers.Clear();
            int queued = 0;
            for (int n = from; n <= to; n++)
            {
                string path = (dyn ? "Game/PortraitDynamic/" : "Game/Portrait/") + sex + "/Body/" + n;
                string key = n.ToString("D6");
                asyncResults[key] = "pending";
                var handler = UnhollowerRuntimeLib.DelegateSupport.ConvertDelegate<Il2CppSystem.Action<UnityEngine.Object>>(
                    new Action<UnityEngine.Object>(o => asyncResults[key] = DescribeObject(o)));
                asyncHandlers.Add(handler);
                try
                {
                    g.res.LoadAsync(path, handler);
                    queued++;
                }
                catch (Exception e)
                {
                    asyncResults[key] = "ERROR " + e.GetType().Name + " " + e.Message.Replace('\n', ' ');
                }
            }
            return "queued=" + queued;
        }

        private static string AsyncResults()
        {
            var sb = new StringBuilder();
            int pending = 0, bad = 0;
            foreach (var pair in asyncResults)
            {
                if (pair.Value == "pending")
                {
                    pending++;
                }
                else if (pair.Value == "null" || pair.Value.Contains("8x8") || pair.Value.Contains(":null") || pair.Value.StartsWith("ERROR"))
                {
                    bad++;
                }
                sb.Append(int.Parse(pair.Key)).Append('=').Append(pair.Value).Append(';');
            }
            return "total=" + asyncResults.Count + " pending=" + pending + " bad=" + bad + " " + sb;
        }

        private static string DescribeObject(UnityEngine.Object o)
        {
            if (o == null)
            {
                return "null";
            }
            GameObject go = o.TryCast<GameObject>();
            if (go == null)
            {
                return o.GetIl2CppType().Name + ":" + o.name;
            }
            var sb = new StringBuilder();
            sb.Append("go(").Append(go.transform.childCount).Append(')');
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                sb.Append(' ').Append(Describe("sr", renderers[i].sprite));
            }
            var images = go.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                sb.Append(' ').Append(Describe("img", images[i].sprite));
            }
            return sb.ToString();
        }

        private static string Describe(string kind, Sprite sprite)
        {
            if (sprite == null)
            {
                return kind + ":null";
            }
            Texture2D texture = sprite.texture;
            string size = texture == null ? "notex" : texture.width + "x" + texture.height + "#" + texture.GetInstanceID();
            return kind + ":" + sprite.name + "/" + size + "/" + (int)sprite.rect.width + "x" + (int)sprite.rect.height;
        }

        // 이름이 맞는(또는 *이면 모든) 유닛 가운데 입화 몸 번호가 from~to인 것. 속성 이름은 실행 시 찾는다(modelData.body가 없으면 그 사실을 적는다).
        private static string BodyOf(string part, int from, int to)
        {
            var units = g.world.unit.GetUnits(false);
            var sb = new StringBuilder();
            int shown = 0, matched = 0;
            for (int i = 0; i < units.Count; i++)
            {
                var saved = units[i].data.unitData.propertyData;
                string name = saved.GetName();
                if (part.Length > 0 && (name == null || name.IndexOf(part, StringComparison.Ordinal) < 0))
                {
                    continue;
                }
                string body = ReadPath(saved, "modelData.body");
                int number;
                if (int.TryParse(body, out number) && (number < from || number > to))
                {
                    continue;
                }
                matched++;
                if (shown < 30)
                {
                    sb.Append(name).Append(':').Append(ReadPath(saved, "sex")).Append(":body=").Append(body).Append(';');
                    shown++;
                }
            }
            return "units=" + units.Count + " matched=" + matched + " shown=" + shown + " " + sb;
        }

        private static string ReadPath(object root, string path)
        {
            object current = root;
            foreach (string step in path.Split('.'))
            {
                if (current == null)
                {
                    return "null";
                }
                System.Reflection.PropertyInfo property = current.GetType().GetProperty(step);
                if (property == null)
                {
                    return "no " + step;
                }
                current = property.GetValue(current, null);
            }
            return current == null ? "null" : current.ToString();
        }

        // 화면에 떠 있는 스크롤 목록(이름에 scrollName이 든 ScrollRect의 Viewport/Content)의 칸마다 그림(Image)의 스프라이트 상태를 적는다.
        // 캐릭터 만들기의 의복 목록(G:srItems) 확인용: 칸 이름(의상 줄 번호)과 그림의 이름·텍스처 크기. 기본은 그림이 없는 칸만, all이면 전부.
        private static string UiGrid(string scrollName, bool all)
        {
            Transform content = null;
            foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
            {
                if (t.name == "Content" && t.parent != null && t.parent.name == "Viewport" && t.parent.parent != null
                    && t.parent.parent.name.Contains(scrollName) && t.gameObject.scene.IsValid())
                {
                    content = t;
                    break;
                }
            }
            if (content == null)
            {
                return "no scroll content for " + scrollName;
            }
            var sb = new StringBuilder();
            int total = 0, blank = 0;
            for (int i = 0; i < content.childCount; i++)
            {
                Transform cell = content.GetChild(i);
                total++;
                var images = cell.GetComponentsInChildren<UnityEngine.UI.Image>(true);
                string best = "noimage";
                bool hasPicture = false;
                for (int k = 0; k < images.Length; k++)
                {
                    Sprite sprite = images[k].sprite;
                    // 칸의 틀(Image·G:goFaceBg·Image_Selected)이 아닌 그림(의복 목록은 G:imgPart)에 8×8보다 큰 스프라이트가 있으면 그림이 있는 칸으로 본다.
                    if (images[k].name != "Image_Selected" && images[k].name != "Image" && images[k].name != "G:goFaceBg"
                        && sprite != null && sprite.texture != null && sprite.texture.width > 8)
                    {
                        hasPicture = true;
                    }
                    best += "|" + images[k].name + ":" + Describe("", sprite);
                }
                if (!hasPicture)
                {
                    blank++;
                }
                if (all || !hasPicture)
                {
                    sb.Append(cell.name).Append('=').Append(best.Length > 160 ? best.Substring(0, 160) : best).Append(';');
                }
            }
            return "cells=" + total + " blank=" + blank + " " + sb;
        }

        // NPC 정보 창(UINPCInfo)을 열어 이름이 맞는 유닛을 보여 준다. 창의 멤버 이름을 소스에서 모르므로, WorldUnitBase를 받는 속성·메서드를
        // 실행 시 찾아 적는다(이름 없이 부르면 멤버 목록만). second에 메서드 이름을 주면 그 메서드를 유닛과 함께 부른다.
        private static string NpcInfo(string part, string method)
        {
            part = part.Replace('_', ' ');   // 이름의 공백은 _로 적는다(명령은 공백으로 나뉜다)
            UINPCInfo ui = g.ui.GetUI<UINPCInfo>(UIType.NPCInfo);
            string opened = ui != null ? "already open " : "";
            if (ui == null)
            {
                try
                {
                    ui = g.ui.OpenUI<UINPCInfo>(UIType.NPCInfo);
                    opened = "opened ";
                }
                catch (Exception e)
                {
                    return "OpenUI ERROR " + e.GetType().Name + ": " + e.Message.Replace('\n', ' ');
                }
            }
            if (ui == null)
            {
                return "OpenUI returned null";
            }
            var sb = new StringBuilder();
            Type type = ui.GetType();
            foreach (System.Reflection.PropertyInfo property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (typeof(WorldUnitBase).IsAssignableFrom(property.PropertyType))
                {
                    sb.Append("prop:").Append(property.Name).Append(property.CanWrite ? "(rw) " : "(r) ");
                }
            }
            foreach (System.Reflection.MethodInfo candidate in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                System.Reflection.ParameterInfo[] parameters = candidate.GetParameters();
                if (parameters.Length >= 1 && parameters.Length <= 3 && typeof(WorldUnitBase).IsAssignableFrom(parameters[0].ParameterType))
                {
                    sb.Append("method:").Append(candidate.Name).Append('/').Append(parameters.Length).Append(' ');
                }
            }
            if (part.Length == 0)
            {
                return sb.ToString();
            }
            WorldUnitBase target = null;
            var units = g.world.unit.GetUnits(false);
            for (int i = 0; i < units.Count; i++)
            {
                string name = units[i].data.unitData.propertyData.GetName();
                if (name != null && name.IndexOf(part, StringComparison.Ordinal) >= 0)
                {
                    target = units[i];
                    break;
                }
            }
            if (target == null)
            {
                return "no unit " + part + " | " + sb;
            }
            string applied = opened;
            try
            {
                foreach (System.Reflection.PropertyInfo property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    if (property.CanWrite && typeof(WorldUnitBase).IsAssignableFrom(property.PropertyType))
                    {
                        property.SetValue(ui, target, null);
                        applied += "set " + property.Name + " ";
                    }
                }
            }
            catch (Exception e)
            {
                applied += "set ERROR " + e.GetType().Name + " ";
            }
            if (method.Length > 0)
            {
                // 이름이 맞는 메서드를 유닛과 함께 부른다. 둘째 인수부터는 형의 기본값(값 형은 0·false, 참조 형은 null)을 준다.
                foreach (System.Reflection.MethodInfo candidate in type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
                {
                    System.Reflection.ParameterInfo[] parameters = candidate.GetParameters();
                    if (candidate.Name == method && parameters.Length >= 1 && typeof(WorldUnitBase).IsAssignableFrom(parameters[0].ParameterType))
                    {
                        object[] args = new object[parameters.Length];
                        args[0] = target;
                        for (int i = 1; i < parameters.Length; i++)
                        {
                            Type parameterType = parameters[i].ParameterType;
                            args[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
                            applied += "arg" + i + "=" + parameterType.Name + " ";
                        }
                        try
                        {
                            candidate.Invoke(ui, args);
                            applied += "called " + method + "/" + parameters.Length + " ";
                        }
                        catch (Exception e)
                        {
                            Exception inner = e.InnerException ?? e;
                            applied += "call ERROR " + inner.GetType().Name + ": " + inner.Message.Replace('\n', ' ') + " ";
                        }
                        break;
                    }
                }
            }
            else
            {
                System.Reflection.MethodInfo update = type.GetMethod("UpdateUI", Type.EmptyTypes);
                if (update != null)
                {
                    update.Invoke(ui, null);
                    applied += "called UpdateUI ";
                }
            }
            return "unit=" + target.data.unitData.propertyData.GetName() + " body=" + ReadPath(target.data.unitData.propertyData, "modelData.body")
                + " " + applied + "| " + sb;
        }

        // UIType의 정적 필드(UIType.PlayerInfo 등)를 이름으로 찾아 g.ui.OpenUI(형)·CloseUI(형, true)를 부른다. UIType은 정적 클래스라
        // 열거형처럼 파싱할 수 없고 필드의 형 이름도 소스에서 모르므로, 필드 값에 맞는 비제네릭 overload를 실행 시 고른다.
        private static string UiCall(string method, string name, bool close)
        {
            // Unhollower는 IL2CPP의 정적 필드를 정적 속성으로 내놓으므로 속성부터 찾고, 없으면 필드를 본다.
            System.Reflection.PropertyInfo property = typeof(UIType).GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            System.Reflection.FieldInfo field = property == null ? typeof(UIType).GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static) : null;
            if (property == null && field == null)
            {
                return "no UIType " + name;
            }
            object value = property != null ? property.GetValue(null, null) : field.GetValue(null);
            string typeName = property != null ? property.PropertyType.Name : field.FieldType.Name;
            foreach (System.Reflection.MethodInfo candidate in g.ui.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                if (candidate.Name != method || candidate.IsGenericMethod)
                {
                    continue;
                }
                System.Reflection.ParameterInfo[] parameters = candidate.GetParameters();
                bool fits = close
                    ? parameters.Length == 2 && parameters[0].ParameterType.IsInstanceOfType(value) && parameters[1].ParameterType == typeof(bool)
                    : parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(value);
                if (fits)
                {
                    candidate.Invoke(g.ui, close ? new object[] { value, true } : new object[] { value });
                    return method + " " + name + " (" + typeName + ")";
                }
            }
            return "no " + method + " overload for " + typeName;
        }

        // 플레이어의 입화 몸(RoleDress 줄 번호. 여성 원작은 58001~58114)을 바꾼다. 고정본 회차에서 화면 확인용. 속성 이름은 실행 시 찾는다.
        private static string SetPlayerBody(int rowId)
        {
            var saved = g.world.playerUnit.data.unitData.propertyData;
            System.Reflection.PropertyInfo modelProperty = saved.GetType().GetProperty("modelData");
            object model = modelProperty == null ? null : modelProperty.GetValue(saved, null);
            if (model == null)
            {
                return "no modelData";
            }
            System.Reflection.PropertyInfo bodyProperty = model.GetType().GetProperty("body");
            if (bodyProperty == null)
            {
                return "no body";
            }
            object before = bodyProperty.GetValue(model, null);
            bodyProperty.SetValue(model, rowId, null);
            // 게임은 lastUpdateBody와 body가 같으면 입화를 다시 만들지 않는 것으로 보여(실측: 바꿔도 화면이 그대로) 함께 지운다.
            System.Reflection.PropertyInfo lastProperty = model.GetType().GetProperty("lastUpdateBody");
            string last = "없음";
            if (lastProperty != null)
            {
                last = lastProperty.GetValue(model, null) + "->0";
                lastProperty.SetValue(model, 0, null);
            }
            return "body " + before + " -> " + bodyProperty.GetValue(model, null) + " lastUpdateBody " + last;
        }

        private static string Props(int itemId)
        {
            ConfItemPropsItem conf = g.conf.itemProps.GetItem(itemId);
            if (conf == null)
            {
                return "no item " + itemId;
            }
            var sb = new StringBuilder();
            foreach (System.Reflection.PropertyInfo property in conf.GetType().GetProperties(System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance))
            {
                Type type = property.PropertyType;
                if (type != typeof(int) && type != typeof(float) && type != typeof(string))
                {
                    continue;
                }
                object value = property.GetValue(conf, null);
                sb.Append(property.Name).Append('=').Append(value == null ? "" : value.ToString()).Append(' ');
            }
            return sb.ToString();
        }
    }
}
