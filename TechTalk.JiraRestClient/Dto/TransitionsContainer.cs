using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Dto
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    internal class TransitionsContainer
    {
        public string expand { get; set; }

        public List<Transition> transitions { get; set; }
    }
}
