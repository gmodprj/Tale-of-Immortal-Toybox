using System;
using System.Reflection;

namespace MOD_sWP6UL
{
    // 빌드 표지. ModMain.csproj의 StampCommit가 InformationalVersion에 넣은 <짧은 커밋>[-dirty]. 없으면 "unknown"(05 문서 49절).
    internal static class BuildInfo
    {
        public static string Commit
        {
            get
            {
                var attribute = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(typeof(BuildInfo).Assembly, typeof(AssemblyInformationalVersionAttribute));
                return attribute == null || string.IsNullOrEmpty(attribute.InformationalVersion) ? "unknown" : attribute.InformationalVersion;
            }
        }
    }
}
