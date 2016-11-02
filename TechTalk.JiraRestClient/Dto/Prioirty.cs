using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public class Priority
    {
        public string self { get; set; }
        public int id { get; set; }
        public string name { get; set; }
        public string iconUrl { get; set; }
    }
}
