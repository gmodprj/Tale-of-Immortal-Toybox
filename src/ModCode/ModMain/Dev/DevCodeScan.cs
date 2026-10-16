using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using Iced.Intel;
using UnhollowerBaseLib;

namespace MOD_sWP6UL
{
    // 개발 전용. IL2CPP로 컴파일된 게임 메서드의 네이티브 코드를 읽어 그 메서드가 어떤 메서드를 부르는지 본다(05 문서 51절).
    // MelonLoader의 XrefScanner는 이 구성에서 캐시 DB 초기화에 실패하고(UsedBy는 게임이 종료됨), XrefScannerLowLevel은 1000바이트·첫 ret까지만 읽어
    // 큰 메서드에는 모자라므로, Iced로 메서드 시작부터 "다음 메서드의 시작" 직전까지 통째로 해독한다.
    //   주소 → 이름 표는 언홀로어가 만든 형식들의 정적 필드 NativeMethodInfoPtr_<메서드>_<서명>(Il2CppMethodInfo*)에서 methodPointer(첫 필드)를 읽어 만든다.
    //   xrefmap                      : 표를 만든다(메서드 수와 걸린 시간)
    //   xrefl <형식> <메서드> [깊이] : 그 메서드가 부르는 메서드들. 깊이 2 이상이면 게임 어셈블리의 대상으로 내려간다
    //   xrefl @<16진 주소> [깊이]    : 그 주소의 코드
    //   xrefd <형식> <메서드> <이름 조각> [앞 수] [뒤 수] : 그 메서드 안에서 이름에 그 조각이 든 메서드를 처음 부르는 자리 앞뒤의 명령을 역어셈블해 보인다
    internal static class DevCodeScan
    {
        private const int MaxBytes = 65536;
        private const int MaxLines = 400;

        private static readonly Dictionary<long, string> names = new Dictionary<long, string>();
        private static readonly HashSet<long> gameCode = new HashSet<long>();
        private static long[] starts;

        public static string Execute(string[] p)
        {
            switch (p[0])
            {
                case "xrefmap":
                    return Build();
                case "xrefd":
                    return Disasm(p[1], p[2], p[3], p.Length > 4 ? int.Parse(p[4]) : 40, p.Length > 5 ? int.Parse(p[5]) : 40);
                case "xrefl":
                    {
                        int depth = p.Length > 3 ? int.Parse(p[3]) : p.Length == 3 && p[1].StartsWith("@") ? int.Parse(p[2]) : 1;
                        if (p[1].StartsWith("@"))
                        {
                            long address = Convert.ToInt64(p[1].Substring(1).Replace("0x", ""), 16);
                            var sb = new StringBuilder();
                            var seen = new HashSet<long>();
                            Walk((IntPtr)address, depth, sb, seen);
                            return sb.ToString();
                        }
                        return Xrefl(p[1], p[2], depth);
                    }
                default:
                    return null;
            }
        }

        private static Type FindType(string name)
        {
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType(name, false);
                if (t != null)
                {
                    return t;
                }
            }
            return null;
        }

        // Il2CppMethodInfo*에서 네이티브 코드의 시작. methodPointer는 구조체의 첫 필드다(Unity 2020.3).
        private static IntPtr CodeOf(MethodBase method)
        {
            FieldInfo field = UnhollowerUtils.GetIl2CppMethodInfoPointerFieldForGeneratedMethod(method);
            if (field == null)
            {
                return IntPtr.Zero;
            }
            IntPtr info = (IntPtr)field.GetValue(null);
            return info == IntPtr.Zero ? IntPtr.Zero : Marshal.ReadIntPtr(info);
        }

        private static string Build()
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            names.Clear();
            gameCode.Clear();
            Assembly game = typeof(g).Assembly;
            var assemblies = new List<Assembly> { game, typeof(UnityEngine.GameObject).Assembly, typeof(UnityEngine.Input).Assembly,
                typeof(UnityEngine.Canvas).Assembly, typeof(UnityEngine.UI.Button).Assembly, typeof(Il2CppSystem.Object).Assembly };
            int types = 0, failedTypes = 0;
            foreach (Assembly asm in assemblies)
            {
                Type[] all;
                try
                {
                    all = asm.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    all = e.Types;
                }
                foreach (Type type in all)
                {
                    if (type == null)
                    {
                        continue;
                    }
                    types++;
                    try
                    {
                        foreach (FieldInfo field in type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                        {
                            if (!field.Name.StartsWith("NativeMethodInfoPtr_") || field.FieldType != typeof(IntPtr))
                            {
                                continue;
                            }
                            IntPtr info = (IntPtr)field.GetValue(null);
                            if (info == IntPtr.Zero)
                            {
                                continue;
                            }
                            long code = (long)Marshal.ReadIntPtr(info);
                            if (code == 0 || names.ContainsKey(code))
                            {
                                continue;
                            }
                            names[code] = type.FullName + "." + field.Name.Substring("NativeMethodInfoPtr_".Length);
                            if (asm == game)
                            {
                                gameCode.Add(code);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        failedTypes++;
                    }
                }
            }
            starts = new long[names.Count];
            names.Keys.CopyTo(starts, 0);
            Array.Sort(starts);
            return "methods=" + names.Count + " types=" + types + " failedTypes=" + failedTypes + " ms=" + watch.ElapsedMilliseconds;
        }

        private static string NameOf(long code)
        {
            string name;
            return names.TryGetValue(code, out name) ? name : "0x" + code.ToString("X");
        }

        // 그 메서드의 코드 끝: 알려진 메서드 가운데 다음으로 시작하는 것의 직전.
        private static long EndOf(long start)
        {
            if (starts == null || starts.Length == 0)
            {
                return start + MaxBytes;
            }
            int index = Array.BinarySearch(starts, start);
            index = index < 0 ? ~index : index + 1;
            long end = index < starts.Length ? starts[index] : start + MaxBytes;
            return Math.Min(end, start + MaxBytes);
        }

        // 메서드 하나의 코드에서 call과 밖으로 나가는 jmp(꼬리 호출)의 대상. RIP 상대 간접 호출은 그 자리의 포인터를 읽는다.
        private static List<long> Targets(long start, out int bytes, out int instructions)
        {
            long end = EndOf(start);
            bytes = (int)(end - start);
            var buffer = new byte[bytes];
            Marshal.Copy((IntPtr)start, buffer, 0, bytes);
            var decoder = Iced.Intel.Decoder.Create(64, new ByteArrayCodeReader(buffer), DecoderOptions.None);
            decoder.IP = (ulong)start;
            var targets = new List<long>();
            var seen = new HashSet<long>();
            instructions = 0;
            while (decoder.IP < (ulong)end)
            {
                Instruction ins;
                decoder.Decode(out ins);
                if (decoder.LastError != DecoderError.None)
                {
                    break;
                }
                instructions++;
                long target = 0;
                if (ins.IsCallNear)
                {
                    target = (long)ins.NearBranchTarget;
                }
                else if (ins.IsJmpNear)
                {
                    long t = (long)ins.NearBranchTarget;
                    if (t < start || t >= end)
                    {
                        target = t;
                    }
                }
                else if ((ins.IsCallNearIndirect || ins.IsJmpNearIndirect) && ins.IsIPRelativeMemoryOperand)
                {
                    try
                    {
                        target = (long)Marshal.ReadIntPtr((IntPtr)(long)ins.IPRelativeMemoryAddress);
                    }
                    catch (Exception)
                    {
                        target = 0;
                    }
                }
                if (target != 0 && seen.Add(target))
                {
                    targets.Add(target);
                }
            }
            return targets;
        }

        private static void Walk(IntPtr code, int depth, StringBuilder sb, HashSet<long> visited)
        {
            long start = (long)code;
            if (!visited.Add(start) || sb.Length > MaxLines * 120)
            {
                return;
            }
            int bytes, instructions;
            List<long> targets;
            try
            {
                targets = Targets(start, out bytes, out instructions);
            }
            catch (Exception e)
            {
                sb.Append(NameOf(start)).Append(": scan failed ").Append(e.GetType().Name).Append(": ").Append(e.Message).Append('\n');
                return;
            }
            sb.Append(NameOf(start)).Append(" @0x").Append(start.ToString("X")).Append(" bytes=").Append(bytes).Append(" ins=").Append(instructions)
                .Append(" calls=").Append(targets.Count).Append('\n');
            foreach (long target in targets)
            {
                sb.Append("  -> ").Append(NameOf(target)).Append('\n');
            }
            if (depth <= 1)
            {
                return;
            }
            foreach (long target in targets)
            {
                if (gameCode.Contains(target))
                {
                    Walk((IntPtr)target, depth - 1, sb, visited);
                }
            }
        }

        // 명령 하나의 호출·점프 대상(없으면 0). Targets와 같은 규칙.
        private static long TargetOf(Instruction ins, long start, long end)
        {
            if (ins.IsCallNear)
            {
                return (long)ins.NearBranchTarget;
            }
            if (ins.IsJmpNear)
            {
                long t = (long)ins.NearBranchTarget;
                return t < start || t >= end ? t : 0;
            }
            if ((ins.IsCallNearIndirect || ins.IsJmpNearIndirect) && ins.IsIPRelativeMemoryOperand)
            {
                try
                {
                    return (long)Marshal.ReadIntPtr((IntPtr)(long)ins.IPRelativeMemoryAddress);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
            return 0;
        }

        private static string Disasm(string typeName, string methodName, string needle, int before, int after)
        {
            if (starts == null)
            {
                Build();
            }
            Type type = FindType(typeName);
            if (type == null)
            {
                return "no type " + typeName;
            }
            MethodInfo method = null;
            foreach (MethodInfo m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                if (m.Name == methodName)
                {
                    method = m;
                    break;
                }
            }
            if (method == null)
            {
                return "no method " + methodName + " in " + typeName;
            }
            long start = (long)CodeOf(method);
            if (start == 0)
            {
                return "no native code";
            }
            long end = EndOf(start);
            var buffer = new byte[end - start];
            Marshal.Copy((IntPtr)start, buffer, 0, buffer.Length);
            var decoder = Iced.Intel.Decoder.Create(64, new ByteArrayCodeReader(buffer), DecoderOptions.None);
            decoder.IP = (ulong)start;
            var list = new List<Instruction>();
            while (decoder.IP < (ulong)end)
            {
                Instruction ins;
                decoder.Decode(out ins);
                if (decoder.LastError != DecoderError.None)
                {
                    break;
                }
                list.Add(ins);
            }
            int at = -1;
            for (int i = 0; i < list.Count && at < 0; i++)
            {
                long target = TargetOf(list[i], start, end);
                if (target != 0 && NameOf(target).IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    at = i;
                }
            }
            if (at < 0)
            {
                return "no call to *" + needle + "* in " + NameOf(start) + " (" + list.Count + " instructions)";
            }
            var formatter = new IntelFormatter();
            var output = new StringOutput();
            var sb = new StringBuilder();
            sb.Append(NameOf(start)).Append(" @0x").Append(start.ToString("X")).Append(" instructions=").Append(list.Count).Append(" at=").Append(at).Append('\n');
            for (int i = Math.Max(0, at - before); i < Math.Min(list.Count, at + after + 1); i++)
            {
                Instruction ins = list[i];
                formatter.Format(ins, output);
                sb.Append(i == at ? ">> " : "   ").Append(i).Append(' ').Append(ins.IP.ToString("X")).Append("  ").Append(output.ToStringAndReset());
                long target = TargetOf(ins, start, end);
                if (target != 0)
                {
                    sb.Append("   ; ").Append(NameOf(target));
                }
                else if (ins.FlowControl == FlowControl.ConditionalBranch || ins.IsJmpNear)
                {
                    sb.Append("   ; -> ").Append(ins.NearBranchTarget.ToString("X"));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string Xrefl(string typeName, string methodName, int depth)
        {
            if (starts == null)
            {
                Build();
            }
            Type type = FindType(typeName);
            if (type == null)
            {
                return "no type " + typeName;
            }
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            var sb = new StringBuilder();
            var visited = new HashSet<long>();
            int found = 0;
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name != methodName)
                {
                    continue;
                }
                found++;
                IntPtr code = CodeOf(method);
                if (code == IntPtr.Zero)
                {
                    sb.Append(method.Name).Append(": no native code\n");
                    continue;
                }
                Walk(code, depth, sb, visited);
            }
            return found == 0 ? "no method " + methodName + " in " + typeName : sb.ToString();
        }
    }
}
