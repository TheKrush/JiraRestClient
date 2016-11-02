using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    public class RemoteLink
    {
        public string id { get; set; }
        public string url { get; set; }
        public string title { get; set; }
        public string summary { get; set; }

        internal static RemoteLink Convert(RemoteLinkResult result)
        {
            result.@object.id = result.id;
            return result.@object;
        }
    }
}
