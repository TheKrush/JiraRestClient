using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace TechTalk.JiraRestClient.Helper
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    internal class List
    {
        internal static bool IsList(object o)
        {
            return o is IList &&
               o.GetType().IsGenericType &&
               o.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(List<>));
        }

        internal static bool HasListEntries(IList list)
        {
            return list == null || list.Count != 0;
        }
    }
}
