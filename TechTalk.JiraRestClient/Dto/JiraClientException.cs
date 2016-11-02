using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace TechTalk.JiraRestClient.Dto
{    
    [Serializable]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "ConvertPropertyToExpressionBody")]
    public class JiraClientException : Exception
    {
        private readonly string _response;
        public JiraClientException() { }
        public JiraClientException(string message) : base(message) { }
        public JiraClientException(string message, string response) : base(message) { _response = response; }
        public JiraClientException(string message, Exception inner) : base(message, inner) { }
        protected JiraClientException(SerializationInfo info, StreamingContext context) : base(info, context) { }
        public string ErrorResponse { get { return _response; } }
    }
}
