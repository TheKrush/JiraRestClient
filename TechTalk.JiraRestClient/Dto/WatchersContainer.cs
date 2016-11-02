using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class WatchersContainer
    {
        public bool isWatching { get; set; }
        public int watchCount { get; set; }
        public List<JiraUser> watchers { get; set; }
    }
}
