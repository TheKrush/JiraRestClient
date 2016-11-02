using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TechTalk.JiraRestClient.Attributes
{
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    internal class Property
    {
        private readonly string _deserializeAsName;

        public string FieldName
        {
            get
            {
                if (!string.IsNullOrEmpty(_deserializeAsName))
                    return _deserializeAsName;
                else
                    return PropertyInfo.Name;
            }
        }
        public FieldInformation FieldInformation { get; set; }
        public PropertyInfo PropertyInfo { get; set; }

        public Property(PropertyInfo property, FieldInformation fieldInformation, string deserializeAsName)
        {
            _deserializeAsName = deserializeAsName;
            FieldInformation = fieldInformation ?? new FieldInformation();
            PropertyInfo = property;
        }
    }
}