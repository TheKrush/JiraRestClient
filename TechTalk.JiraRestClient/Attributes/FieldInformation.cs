using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Attributes
{
    internal enum SpecialFunction
    {
        None,
        Comments        
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    internal sealed class FieldInformation : System.Attribute
    {
        public FieldInformation()
        {
            SpecialFunction = SpecialFunction.None;
        }
        public bool UpdateRelevant { get; set; }
        public bool NullableAllowed { get; set; }
        public SpecialFunction SpecialFunction { get; set; }
    }
}
