using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using TechTalk.JiraRestClient.Attributes;
using TechTalk.JiraRestClient.Dto;
using TechTalk.JiraRestClient.Helper;

namespace TechTalk.JiraRestClient.Client
{
    //JIRA REST API documentation: https://docs.atlassian.com/jira/REST/latest

    public class JiraClient<TIssueFields> : IJiraClient<TIssueFields> where TIssueFields : IssueFields, new()
    {
        private readonly string _authorizathionInformation;
        private readonly JsonDeserializer _deserializer;
        private readonly string _baseApiUrl;
        public JiraClient(string baseUrl, string username, string password)
        {
            _baseApiUrl = new Uri(new Uri(baseUrl), "rest/api/2/").ToString();
            _authorizathionInformation = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
            _deserializer = new JsonDeserializer();
        }

        public JiraClient(string baseUrl, string authstring)
        {
            _authorizathionInformation = authstring;
            _baseApiUrl = new Uri(new Uri(baseUrl), "rest/api/2/").ToString();
            _deserializer = new JsonDeserializer();
        }

        private RestRequest CreateRequest(Method method, string path)
        {
            var request = new RestRequest { Method = method, Resource = path, RequestFormat = DataFormat.Json, DateFormat = "yyyy-MM-ddTHH:mm:ss.fffzz00" };
            request.AddHeader("Authorization", $"Basic {_authorizathionInformation}");
            return request;
        }

        private IRestResponse ExecuteRequest(RestRequest request)
        {
            var client = new RestClient(_baseApiUrl);
            return client.Execute(request);
        }

        // ReSharper disable once UnusedParameter.Local
        private void AssertStatus(IRestResponse response, HttpStatusCode status)
        {
            if (response.ErrorException != null)
                throw new JiraClientException("Transport level error: " + response.ErrorMessage, response.ErrorException);
            if (response.StatusCode != status)
                throw new JiraClientException("JIRA returned wrong status: " + response.StatusDescription, response.Content);
        }

        public JiraUser GetLoggedInUser()
        {
            try
            {
                var path = "myself";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var jiraUser = _deserializer.Deserialize<JiraUser>(response);
                return jiraUser;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetLoggedInUser() error: {0}", ex);
                throw new JiraClientException("Could not load user", ex);
            }
        }


        public IEnumerable<Project> GetProjects()
        {
            try
            {
                var request = CreateRequest(Method.GET, "project");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<List<Project>>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"GetProjects() error: {ex}");
                throw new JiraClientException("Could not load projects", ex);
            }
        }

        public IEnumerable<Issue<TIssueFields>> GetIssues(string projectKey)
        {
            return EnumerateIssues(projectKey, null).ToArray();
        }

        public IEnumerable<Issue<TIssueFields>> GetIssues(string projectKey, string issueType)
        {
            return EnumerateIssues(projectKey, issueType).ToArray();
        }

        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(string projectKey)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, null), null, 0);
        }

        public IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(string jqlQuery)
        {
            return EnumerateIssuesByQueryInternal(jqlQuery, null, 0);
        }

        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(string projectKey, string issueType)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0);
        }

        private static string CreateCommonJql(string projectKey, string issueType)
        {
            var queryParts = new List<string>();
            if (!string.IsNullOrEmpty(projectKey))
                queryParts.Add($"project={projectKey}");
            if (!string.IsNullOrEmpty(issueType))
                queryParts.Add($"issueType={issueType}");
            return string.Join(" AND ", queryParts);
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, Fields, startIndex).ToArray() instead")]
        public IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(string projectKey, string issueType, string jqlQuery)
        {
            var jql = CreateCommonJql(projectKey, issueType);
            // if neither are empty, join them with an 'and'
            if (!string.IsNullOrEmpty(jql) && !string.IsNullOrEmpty(jqlQuery))
            {
                // ReSharper disable once RedundantAssignment
                jql += "+AND+";
            }
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0).ToArray();
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, Fields, startIndex) instead")]
        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(string projectKey, string issueType, string fields)
        {
            var fieldDef = fields?.Split(',').Select(str => (str ?? "").Trim())
                .Where(str => !string.IsNullOrEmpty(str)).ToArray();
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), fieldDef, 0);
        }

        public IEnumerable<Issue<TIssueFields>> EnumerateIssuesByQuery(string jqlQuery, string[] fields, int startIndex)
        {
            try
            {
                return EnumerateIssuesByQueryInternal(Uri.EscapeUriString(jqlQuery), fields, startIndex);
            }
            catch (Exception ex)
            {
                Trace.TraceError($"EnumerateIssuesByQuery(jqlQuery, Fields, startIndex) error: {ex}");
                throw new JiraClientException("Could not load Issues", ex);
            }
        }

        private IEnumerable<Issue<TIssueFields>> EnumerateIssuesByQueryInternal(string jqlQuery, string[] fields, int startIndex)
        {
            const int queryCount = 50;
            var resultCount = startIndex;
            while (true)
            {
                var path = $"search?jql={jqlQuery}&startAt={resultCount}&maxResults={queryCount}";
                if (fields != null) path += $"&Fields={string.Join(",", fields)}";

                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<IssueContainer<TIssueFields>>(response);
                var issues = data.issues ?? Enumerable.Empty<Issue<TIssueFields>>();

                // ReSharper disable once PossibleMultipleEnumeration
                foreach (var item in issues) yield return item;
                // ReSharper disable once PossibleMultipleEnumeration
                resultCount += issues.Count();

                //all Issues received?
                if (resultCount >= data.total) break;
            }
        }

        public IQueryable<Issue<TIssueFields>> QueryIssues()
        {
            return new QueryableIssueCollection<TIssueFields>(this);
        }


        public Issue<TIssueFields> LoadIssue(IssueRef issueRef)
        {
            return LoadIssue(issueRef.JiraIdentifier);
        }

        public Issue<TIssueFields> LoadIssue(string issueRef)
        {
            try
            {
                var path = $"issue/{issueRef}";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var issue = _deserializer.Deserialize<Issue<TIssueFields>>(response);
                issue.fields.Comments = GetComments(issue).ToList();
                issue.fields.Watchers = GetWatchers(issue).ToList();
                Issue.ExpandLinks(issue);

                return issue;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssue(issueRef) error: {0}", ex);
                throw new JiraClientException("Could not load issue", ex);
            }
        }

        public Issue<TIssueFields> CreateIssue(string projectKey, string issueType, string summary)
        {
            return CreateIssue(projectKey, issueType, new TIssueFields { Summary = summary });
        }

        public Issue<TIssueFields> CreateIssue(string projectKey, string issueType, TIssueFields issue)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issue");
                request.AddHeader("ContentType", "application/json");

                //Project + issuetype wird über die übergabeparameter "hardcoded" und nicht von den Fields ausgelesen! (deshalb sind diese auch mit private set)
                var issueData = new Dictionary<string, object> { { "project", new { key = projectKey } }, { "issuetype", new { name = issueType } } };

                //Alle properties auslesen welche FieldInformations hinterlegt haben!
                var propertyInfos = typeof(TIssueFields).GetProperties();
                var propertiesWithFieldInfos = propertyInfos
                    .Select(p => new { Property = p, FieldInfo = p.GetAttribute<FieldInformation>(), DeserializeAsAttribute = p.GetAttribute<DeserializeAsAttribute>() })
                    .Select(p => new Property(p.Property, p.FieldInfo, p.DeserializeAsAttribute?.Name));

                //Update relevante Properties
                foreach (var property in propertiesWithFieldInfos.Where(a => a.FieldInformation.UpdateRelevant && a.FieldInformation.SpecialFunction == SpecialFunction.None))
                {
                    if (!string.IsNullOrEmpty(property.FieldName))
                    {
                        var value = property.PropertyInfo.GetValue(issue, null);
                        if (property.FieldInformation.NullableAllowed || value != null)
                        {
                            var addEntry = true;

                            //Prüfen ob es sich um eine Liste handelt, wenn ja dann auch noch prüfen ob mind. ein Item enthalten ist!
                            //Jira hat sonst teilweise ein Problem wenn eine leere liste zu aktualisieren wäre.                            
                            if (List.IsList(value))
                            {
                                addEntry = List.HasListEntries(value as IList);
                            }

                            if (addEntry)
                                issueData.Add(property.FieldName, new[] { new { set = value } });
                        }
                    }
                }

                request.AddBody(new { fields = issueData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                var issueRef = _deserializer.Deserialize<IssueRef>(response);
                return LoadIssue(issueRef);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssue(projectKey, typeCode) error: {0}", ex);
                throw new JiraClientException("Could not create issue", ex);
            }
        }

        public Issue<TIssueFields> UpdateIssue(Issue<TIssueFields> issue)
        {
            try
            {
                var path = $"issue/{issue.JiraIdentifier}";
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var issueData = new Dictionary<string, object>();

                //Alle properties auslesen welche FieldInformations hinterlegt haben!
                var propertyInfos = typeof(TIssueFields).GetProperties();
                var propertiesWithFieldInfos = propertyInfos
                    .Select(p => new { Property = p, FieldInfo = p.GetAttribute<FieldInformation>(), DeserializeAsAttribute = p.GetAttribute<DeserializeAsAttribute>() })
                    .Select(p => new Property(p.Property, p.FieldInfo, p.DeserializeAsAttribute?.Name)).ToList();

                //Update relevante Properties (welche kein spezialupdate sind)
                foreach (var property in propertiesWithFieldInfos.Where(a => a.FieldInformation.UpdateRelevant && a.FieldInformation.SpecialFunction == SpecialFunction.None))
                {
                    if (!string.IsNullOrEmpty(property.FieldName))
                    {
                        var value = property.PropertyInfo.GetValue(issue.fields, null);
                        if (property.FieldInformation.NullableAllowed || value != null)
                        {
                            var addEntry = true;

                            //Prüfen ob es sich um eine Liste handelt, wenn ja dann auch noch prüfen ob mind. ein Item enthalten ist!
                            //Jira hat sonst teilweise ein Problem wenn eine leere liste zu aktualisieren wäre.                            
                            if (List.IsList(value))
                            {
                                addEntry = List.HasListEntries(value as IList);
                            }

                            if (addEntry)
                                issueData.Add(property.FieldName, new[] { new { set = value } });
                        }
                    }
                }

                request.AddBody(new { update = issueData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                //Execute special updates...            
                var propertiesWithSpecialFunctions = propertiesWithFieldInfos.Where(a => a.FieldInformation.UpdateRelevant && a.FieldInformation.SpecialFunction != SpecialFunction.None).ToList();
                if (propertiesWithSpecialFunctions.Count > 0)
                {
                    var issueRef = new IssueRef() { id = issue.id, key = issue.key };
                    foreach (var property in propertiesWithSpecialFunctions)
                        DealSpecialFunction(property, issue.fields, issueRef);
                }
                //Reload issue to get ids etc.
                return LoadIssue(issue);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateIssue(issue) error: {0}", ex);
                throw new JiraClientException("Could not update issue", ex);
            }
        }

        private void DealSpecialFunction(Property property, TIssueFields issueFields, IssueRef issueRef)
        {
            switch (property.FieldInformation.SpecialFunction)
            {
                case SpecialFunction.None:
                    //Wurden schon ausgefiltert, aber falls nichts wird hier nichts durchgeführt!
                    break;
                case SpecialFunction.Comments:
                    //Prüfen ob es sich um eine Liste handelt (muss bei comments sein!)
                    var value = property.PropertyInfo.GetValue(issueFields, null);
                    if (List.IsList(value))
                    {
                        var comments = (value as IList);
                        if (comments != null)
                        {
                            foreach (Comment comment in comments)
                            {
                                //Neuanlage?
                                if (string.IsNullOrEmpty(comment.id) && !string.IsNullOrEmpty(comment.body))
                                {
                                    CreateComment(issueRef, comment.body);
                                }
                                else
                                {
                                    UpdateComment(issueRef, comment);
                                }
                            }
                        }
                    }
                    break;
                default:
                    //Kann eigentlich nie auftreten.
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void DeleteIssue(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.id}?deleteSubtasks=true";
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteIssue(issue) error: {0}", ex);
                throw new JiraClientException("Could not delete issue", ex);
            }
        }


        public IEnumerable<Transition> GetTransitions(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.JiraIdentifier}/transitions?expand=transitions.Fields";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<TransitionsContainer>(response);
                return data.transitions;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetTransitions(issue) error: {0}", ex);
                throw new JiraClientException("Could not load issue transitions", ex);
            }
        }

        public Issue<TIssueFields> TransitionIssue(IssueRef issue, Transition transition)
        {
            try
            {
                var path = $"issue/{issue.id}/transitions";
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var update = new Dictionary<string, object>();
                // ReSharper disable once RedundantAnonymousTypePropertyName
                update.Add("transition", new { id = transition.id });
                if (transition.fields != null)
                    update.Add("Fields", transition.fields);

                request.AddBody(update);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return LoadIssue(issue);
            }
            catch (Exception ex)
            {
                Trace.TraceError("TransitionIssue(issue, transition) error: {0}", ex);
                throw new JiraClientException("Could not transition issue state", ex);
            }
        }

        public IEnumerable<JiraUser> FindUsers(string search)
        {
            try
            {
                var path = $"user/search?username={search}";
                var request = CreateRequest(Method.GET, path);
                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var result = _deserializer.Deserialize<List<JiraUser>>(response);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("FindUsers(issue) error: {0}", ex);
                throw new JiraClientException($"Could find user {search}. {ex}");
            }
        }

        public JiraUser FindUser(string search)
        {
            return FindUsers(search).FirstOrDefault();
        }

        public IEnumerable<JiraUser> GetWatchers(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.id}/watchers";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<WatchersContainer>(response).watchers;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWatchers(issue) error: {0}", ex);
                throw new JiraClientException("Could not load watchers", ex);
            }
        }

        public List<T> GetProjects<T>() where T : JiraProject
        {
            try
            {
                var path = "project";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<List<T>>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetProjects() error: {0}", ex);
                throw new JiraClientException("Could not load projects", ex);
            }

        }

        public List<T> FindUsers<T>(string search) where T : JiraUser
        {
            try
            {
                var path = $"user/search?username={search}";
                var request = CreateRequest(Method.GET, path);
                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var result = _deserializer.Deserialize<List<T>>(response);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("FindUsers(issue) error: {0}", ex);
                throw new JiraClientException($"Could find user {search}. {ex}");
            }
        }

        public T FindUser<T>(string search) where T : JiraUser
        {
            return FindUsers<T>(search).FirstOrDefault();
        }



        public IEnumerable<Comment> GetComments(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.id}/comment";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<CommentsContainer>(response);
                return data.comments ?? Enumerable.Empty<Comment>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetComments(issue) error: {0}", ex);
                throw new JiraClientException("Could not load Comments", ex);
            }
        }

        public Comment CreateComment(IssueRef issue, string comment)
        {
            try
            {
                var path = $"issue/{issue.id}/comment";
                var request = CreateRequest(Method.POST, path); //Bei create muss hier POST verwendet werden!
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new Comment { body = comment });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return _deserializer.Deserialize<Comment>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not create comment", ex);
            }
        }

        public Comment UpdateComment(IssueRef issue, Comment comment)
        {
            try
            {
                var path = $"issue/{issue.id}/comment/{comment.id}";
                var request = CreateRequest(Method.PUT, path); //Bei update muss hier PUT verwendet werden!
                request.AddHeader("ContentType", "application/json");
                request.AddBody(comment);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<Comment>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not create comment", ex);
            }
        }

        public void DeleteComment(IssueRef issue, Comment comment)
        {
            try
            {
                var path = $"issue/{issue.id}/comment/{comment.id}";
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not delete comment", ex);
            }
        }

        public IEnumerable<Worklog> GetWorklogs(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.id}/worklog";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<WorklogsContainer>(response);
                return data.Worklogs ?? Enumerable.Empty<Worklog>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWorklogs(issue) error: {0}", ex);
                throw new JiraClientException("Could not load worklogs", ex);
            }
        }

        public Worklog CreateWorklog(IssueRef issue, int timespentSeconds, string comment, DateTime started)
        {
            try
            {
                var path = $"issue/{issue.id}/worklog";
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var insert = new Dictionary<string, object>();
                insert.Add("started", started.ToString("yyyy-MM-ddTHH:mm:ss.fffzz00"));
                insert.Add("comment", comment);
                insert.Add("timeSpentSeconds", timespentSeconds);

                request.AddBody(insert);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return _deserializer.Deserialize<Worklog>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateComment(issue, comment) error: {0}", ex);
                throw new JiraClientException("Could not create worklog", ex);
            }
        }

        public Worklog UpdateWorklog(IssueRef issue, Worklog worklog)
        {
            try
            {
                var path = $"issue/{issue.id}/worklog/{worklog.id}";
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (worklog.comment != null) updateData.Add("comment", worklog.comment);
                if (worklog.started != DateTime.MinValue) updateData.Add("started", worklog.started.ToString("yyyy-MM-ddTHH:mm:ss.fffzz00"));
                if (worklog.timeSpentSeconds != 0) updateData.Add("timeSpentSeconds", worklog.timeSpentSeconds);
                request.AddBody(updateData);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<Worklog>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateWorklog(issue, worklog) error: {0}", ex);
                throw new JiraClientException("Could not update worklog for issue", ex);
            }
        }

        public void DeleteWorklog(IssueRef issue, Worklog worklog)
        {
            try
            {
                var path = $"issue/{issue.id}/worklog/{worklog.id}";
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteWorklog(issue, worklog) error: {0}", ex);
                throw new JiraClientException("Could not delete worklog", ex);
            }
        }


        public IEnumerable<Attachment> GetAttachments(IssueRef issue)
        {
            return LoadIssue(issue).fields.Attachment;
        }

        public Attachment CreateAttachment(IssueRef issue, Stream fileStream, string fileName)
        {
            try
            {
                var path = $"issue/{issue.JiraIdentifier}/attachments";
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("X-Atlassian-Token", "nocheck");
                request.AddHeader("ContentType", "multipart/form-data");
                request.AddFile("file", stream => fileStream.CopyTo(stream), fileName);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<List<Attachment>>(response).Single();
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateAttachment(issue, fileStream, fileName) error: {0}", ex);
                throw new JiraClientException("Could not create attachment", ex);
            }
        }

        public void DeleteAttachment(Attachment attachment)
        {
            try
            {
                var path = $"attachment/{attachment.id}";
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteAttachment(attachment) error: {0}", ex);
                throw new JiraClientException("Could not delete attachment", ex);
            }
        }


        public IEnumerable<IssueLink> GetIssueLinks(IssueRef issue)
        {
            return LoadIssue(issue).fields.IssueLinks;
        }

        public IssueLink LoadIssueLink(IssueRef parent, IssueRef child, string relationship)
        {
            try
            {
                var issue = LoadIssue(parent);
                var links = issue.fields.IssueLinks
                    .Where(l => l.type.name == relationship)
                    .Where(l => l.inwardIssue.id == parent.id)
                    .Where(l => l.outwardIssue.id == child.id)
                    .ToArray();

                if (links.Length > 1)
                    throw new JiraClientException("Ambiguous issue link");
                return links.SingleOrDefault();
            }
            catch (Exception ex)
            {
                Trace.TraceError("LoadIssueLink(parent, child, relationship) error: {0}", ex);
                throw new JiraClientException("Could not load issue link", ex);
            }
        }

        public IssueLink CreateIssueLink(IssueRef parent, IssueRef child, string relationship)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issueLink");
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new
                {
                    type = new { name = relationship },
                    // ReSharper disable RedundantAnonymousTypePropertyName
                    inwardIssue = new { id = parent.id },                    
                    outwardIssue = new { id = child.id }
                    // ReSharper restore RedundantAnonymousTypePropertyName
                });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return LoadIssueLink(parent, child, relationship);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssueLink(parent, child, relationship) error: {0}", ex);
                throw new JiraClientException("Could not link Issues", ex);
            }
        }

        public void DeleteIssueLink(IssueLink link)
        {
            try
            {
                var path = $"issueLink/{link.id}";
                var request = CreateRequest(Method.DELETE, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteIssueLink(link) error: {0}", ex);
                throw new JiraClientException("Could not delete issue link", ex);
            }
        }


        public IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue)
        {
            try
            {
                var path = $"issue/{issue.id}/remotelink";
                var request = CreateRequest(Method.GET, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<List<RemoteLinkResult>>(response)
                    .Select(RemoteLink.Convert).ToList();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetRemoteLinks(issue) error: {0}", ex);
                throw new JiraClientException("Could not load external links for issue", ex);
            }
        }

        public RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = $"issue/{issue.id}/remotelink";
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new
                {
                    application = new
                    {
                        type = "TechTalk.JiraRestClient",
                        name = "JIRA REST client"
                    },
                    @object = new
                    {
                        // ReSharper disable RedundantAnonymousTypePropertyName
                        url = remoteLink.url,                        
                        title = remoteLink.title,
                        summary = remoteLink.summary
                        // ReSharper restore RedundantAnonymousTypePropertyName
                    }
                });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                //returns: { "Id": <Id>, "self": <url> }
                var linkId = _deserializer.Deserialize<RemoteLink>(response).id;
                return GetRemoteLinks(issue).Single(rl => rl.id == linkId);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not create external link for issue", ex);
            }
        }

        public RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = $"issue/{issue.id}/remotelink/{remoteLink.id}";
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (remoteLink.url != null) updateData.Add("url", remoteLink.url);
                if (remoteLink.title != null) updateData.Add("title", remoteLink.title);
                if (remoteLink.summary != null) updateData.Add("Summary", remoteLink.summary);
                request.AddBody(new { @object = updateData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return GetRemoteLinks(issue).Single(rl => rl.id == remoteLink.id);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not update external link for issue", ex);
            }
        }

        public void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            try
            {
                var path = $"issue/{issue.id}/remotelink/{remoteLink.id}";
                var request = CreateRequest(Method.DELETE, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                Trace.TraceError("DeleteRemoteLink(issue, remoteLink) error: {0}", ex);
                throw new JiraClientException("Could not delete external link for issue", ex);
            }
        }

        public WorklogUpdated GetWorklogUpdated(DateTime since)
        {
            try
            {
                var unixSince = TimeUtils.ToUnixTime(since);
                var path = $"worklog/updated?since={unixSince}";
                var request = CreateRequest(Method.GET, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<WorklogUpdated>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWorklogUpdated() error: {0}", ex);
                throw new JiraClientException("Could not load worklog updated", ex);
            }
        }

        public IEnumerable<Worklog> GetWorklogList(int[] ids)
        {
            try
            {
                var request = CreateRequest(Method.POST, "worklog/list");
                request.AddHeader("ContentType", "application/json");
                // ReSharper disable once RedundantAnonymousTypePropertyName
                request.AddBody(new { ids = ids });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<List<Worklog>>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetWorklogList() error: {0}", ex);
                throw new JiraClientException("Could not load worklog list", ex);
            }
        }

        public IEnumerable<T> GetIssueTypes<T>() where T : IssueType
        {
            try
            {
                var request = CreateRequest(Method.GET, "issuetype");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<List<T>>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssueTypes() error: {0}", ex);
                throw new JiraClientException("Could not load issue types", ex);
            }
        }

        public IEnumerable<T> GetIssueStatuses<T>() where T : Status
        {
            try
            {
                var request = CreateRequest(Method.GET, "status");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<List<T>>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssueStatuses() error: {0}", ex);
                throw new JiraClientException("Could not load issue statuses", ex);
            }
        }

        public IEnumerable<T> GetIssuePriorities<T>() where T : IssuePriority
        {
            try
            {
                var request = CreateRequest(Method.GET, "priority");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = _deserializer.Deserialize<List<T>>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssuePriorities() error: {0}", ex);
                throw new JiraClientException("Could not load issue priorities", ex);
            }
        }

        public ServerInfo GetServerInfo()
        {
            try
            {
                var request = CreateRequest(Method.GET, "serverInfo");
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<ServerInfo>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetServerInfo() error: {0}", ex);
                throw new JiraClientException("Could not retrieve server information", ex);
            }
        }

        public IEnumerable<ProjectVersion> GetVersions(string projectKey)
        {
            try
            {
                var path = $"project/{Uri.EscapeUriString(projectKey)}/versions";
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return _deserializer.Deserialize<List<ProjectVersion>>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetVersions(projectKey) error: {0}", ex);
                throw new JiraClientException("Could not load project version", ex);
            }
        }
    }
}
