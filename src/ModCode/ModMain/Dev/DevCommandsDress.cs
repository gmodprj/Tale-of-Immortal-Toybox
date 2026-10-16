using System;
using System.Text;
using UnityEngine;

namespace MOD_sWP6UL
{
    // 개발 전용. 입화 몸 그림의 조사(05 문서 51절). 의상 표(RoleDress)와 몸 그림 프리팹을 읽기만 한다.
    //   rdress <id>                             RoleDress 한 줄(id·dressID·sex·type·className·name·active·weight·npcGrade·beauty)
    //   rdress list <sex> <type> [from] [to]    GetDressItem(sex, type)의 줄들. sex 1 남 2 여, type yifu 등. dressID가 from~to인 것만
    //   rbody <from> [to] [Woman|Man] [dyn]     Game/Portrait/<성별>/Body/<n>(dyn이면 PortraitDynamic)을 g.res로 읽어 프리팹과 그 안 그림의 상태
    //   rbodyof <이름 일부>                      이름이 맞는 유닛들의 입화 몸 번호(PortraitModelData.body)와 성별
    //   rprops <itemId>                         ConfItemPropsItem의 정수·실수·글자 속성 전부(의복 아이템과 몸 번호의 대응을 찾는 용도)
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
                case "rbodyof":
                    return BodyOf(p.Length > 1 && p[1] != "*" ? p[1] : "", p.Length > 2 ? int.Parse(p[2]) : int.MinValue,
                        p.Length > 3 ? int.Parse(p[3]) : int.MaxValue);
                case "rprops":
                    return Props(int.Parse(p[1]));
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
                    .Append(":w").Append(row.weight).Append(":g").Append(row.npcGrade).Append(';');
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

        private static string Describe(string kind, Sprite sprite)
        {
            if (sprite == null)
            {
                return kind + ":null";
            }
            Texture2D texture = sprite.texture;
            string size = texture == null ? "notex" : texture.width + "x" + texture.height;
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
