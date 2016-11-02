using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Extensions;
using TechTalk.JiraRestClient.Utils;

namespace TechTalk.JiraRestClient
{
    //JIRA REST API documentation: https://docs.atlassian.com/jira/REST/latest

    public class JiraClient<TIssueFields> : IJiraClient<TIssueFields> where TIssueFields : IssueFields, new()
    {
        //TODO-BS: Naming usw. überarbeiten
        private readonly string auth;
        private readonly JsonDeserializer deserializer;
        private readonly string baseApiUrl;
        public JiraClient(string baseUrl, string username, string password)
        {
            baseApiUrl = new Uri(new Uri(baseUrl), "rest/api/2/").ToString();
            auth = Convert.ToBase64String(Encoding.UTF8.GetBytes(String.Format("{0}:{1}", username, password)));
            deserializer = new JsonDeserializer();
        }

        public JiraClient(string baseUrl, string authstring)
        {
            auth = authstring;
            baseApiUrl = new Uri(new Uri(baseUrl), "rest/api/2/").ToString();
            deserializer = new JsonDeserializer();
        }

        private RestRequest CreateRequest(Method method, String path)
        {
            var request = new RestRequest { Method = method, Resource = path, RequestFormat = DataFormat.Json, DateFormat = "yyyy-MM-ddTHH:mm:ss.fffzz00" };
            request.AddHeader("Authorization", String.Format("Basic {0}", auth));
            return request;
        }

        private IRestResponse ExecuteRequest(RestRequest request)
        {
            var client = new RestClient(baseApiUrl);
            return client.Execute(request);
        }

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

                var jiraUser = deserializer.Deserialize<JiraUser>(response);
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

                var data = deserializer.Deserialize<List<Project>>(response);
                return data;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetProjects() error: {0}", ex);
                throw new JiraClientException("Could not load projects", ex);
            }
        }

        public IEnumerable<Issue<TIssueFields>> GetIssues(String projectKey)
        {
            return EnumerateIssues(projectKey, null).ToArray();
        }

        public IEnumerable<Issue<TIssueFields>> GetIssues(String projectKey, String issueType)
        {
            return EnumerateIssues(projectKey, issueType).ToArray();
        }

        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, null), null, 0);
        }

        public IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(string jqlQuery)
        {
            return EnumerateIssuesInternal("", "", jqlQuery);
        }
        
        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey, String issueType)
        {
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0);
        }

        private static string CreateCommonJql(String projectKey, String issueType)
        {
            var queryParts = new List<String>();
            if (!String.IsNullOrEmpty(projectKey))
                queryParts.Add(String.Format("project={0}", projectKey));
            if (!String.IsNullOrEmpty(issueType))
                queryParts.Add(String.Format("issueType={0}", issueType));
            return String.Join(" AND ", queryParts);
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex).ToArray() instead")]
        public IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery)
        {
            var jql = CreateCommonJql(projectKey, issueType);
            if (!String.IsNullOrEmpty(jql) && !String.IsNullOrEmpty(jqlQuery))
                jql += "+AND+";// if neither are empty, join them with an 'and'
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), null, 0).ToArray();
        }

        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, fields, startIndex) instead")]
        public IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey, String issueType, String fields)
        {
            var fieldDef = fields == null ? null
                : fields.Split(',').Select(str => (str ?? "").Trim())
                    .Where(str => !string.IsNullOrEmpty(str)).ToArray();
            return EnumerateIssuesByQuery(CreateCommonJql(projectKey, issueType), fieldDef, 0);
        }

        public IEnumerable<Issue<TIssueFields>> EnumerateIssuesByQuery(String jqlQuery, String[] fields, Int32 startIndex)
        {
            try
            {
                return EnumerateIssuesByQueryInternal(Uri.EscapeUriString(jqlQuery), fields, startIndex);
            }
            catch (Exception ex)
            {
                Trace.TraceError("EnumerateIssuesByQuery(jqlQuery, fields, startIndex) error: {0}", ex);
                throw new JiraClientException("Could not load issues", ex);
            }
        }

        private IEnumerable<Issue<TIssueFields>> EnumerateIssuesByQueryInternal(String jqlQuery, String[] fields, Int32 startIndex)
        {
            var queryCount = 50;
            var resultCount = startIndex;
            while (true)
            {
                var path = String.Format("search?jql={0}&startAt={1}&maxResults={2}", jqlQuery, resultCount, queryCount);
                if (fields != null) path += String.Format("&fields={0}", String.Join(",", fields));

                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<IssueContainer<TIssueFields>>(response);
                var issues = data.issues ?? Enumerable.Empty<Issue<TIssueFields>>();

                foreach (var item in issues) yield return item;
                resultCount += issues.Count();

                if (resultCount < data.total) continue;
                else /* all issues received */ break;
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

        public Issue<TIssueFields> LoadIssue(String issueRef)
        {
            try
            {
                var path = String.Format("issue/{0}", issueRef);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var issue = deserializer.Deserialize<Issue<TIssueFields>>(response);
                issue.fields.comments = GetComments(issue).ToList();
                issue.fields.watchers = GetWatchers(issue).ToList();
                Issue.ExpandLinks(issue);

                ////Customfield name as attribute on a property
                //var propertyInfos = typeof(TIssueFields).GetProperties();
                //var propertiesFromAttribute = propertyInfos
                //    .Select(p => new { Property = p, FieldAttribute = p.GetAttribute<FieldAttribute>() })
                //    .Where(a => a.FieldAttribute != null)
                //    .Select(p => new NamedProperty(p.Property, p.FieldAttribute.FieldName));

                //var customFields = propertyInfos
                //    .Where(p => p.Name.StartsWith("customfield_"))
                //    .Select(p => new NamedProperty(p, p.Name));

                //var propertyList = customFields.Concat(propertiesFromAttribute).ToList();
                //if (!propertyList.Any()) return issue;

                //foreach (var namedProperty in propertyList)
                //{

                //    //namedProperty.FieldName

                //    var jsonResponse = deserializer.Deserialize<Dictionary<string, string>>(response);
                    
                    
                //    //var jsonFields = deserializer.Deserialize<Dictionary<string, string>>(jsonResponse["fields"]); 
                //    Console.WriteLine(jsonResponse);

                //    namedProperty.Property.SetValue(issue.fields, Convert.ChangeType("123", namedProperty.Property.PropertyType), null);


                //    //var value = namedProperty.Property.GetValue(issue, null);
                //    //if (value != null)
                //    //{
                //    //    //issue..Add(namedProperty.FieldName, value);   
                //    //}
                //}
                
                return issue;
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssue(issueRef) error: {0}", ex);
                throw new JiraClientException("Could not load issue", ex);
            }
        }

        public Issue<TIssueFields> CreateIssue(String projectKey, String issueType, String summary)
        {
            return CreateIssue(projectKey, issueType, new TIssueFields { summary = summary });
        }

        public Issue<TIssueFields> CreateIssue(String projectKey, String issueType, TIssueFields issue)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issue");
                request.AddHeader("ContentType", "application/json");

                var issueData = new Dictionary<string, object>();
                issueData.Add("project", new { key = projectKey });
                issueData.Add("issuetype", new { name = issueType });

                if (issue.assignee != null)
                    issueData.Add("assignee", issue.assignee);
                if (issue.reporter != null)
                    issueData.Add("reporter", issue.reporter);
                if (issue.summary != null)
                    issueData.Add("summary", issue.summary);
                if (issue.description != null)
                    issueData.Add("description", issue.description);
                if (issue.labels != null && issue.labels.Count > 0)
                    issueData.Add("labels", issue.labels);
                if (issue.timetracking != null)
                    issueData.Add("timetracking", new { originalEstimate = issue.timetracking.originalEstimate });

                //************************************************************
                //Customfield name as attribute on a property
                var propertyInfos = typeof(TIssueFields).GetProperties();
                var propertiesFromAttribute = propertyInfos
                    .Select(p => new { Property = p, FieldAttribute = p.GetAttribute<DeserializeAsAttribute>() })
                    .Where(a => a.FieldAttribute != null)
                    .Select(p => new NamedProperty(p.Property, p.FieldAttribute.Name));

                var customFields = propertyInfos
                    .Where(p => p.Name.StartsWith("customfield_"))
                    .Select(p => new NamedProperty(p, p.Name));

                var propertyList = customFields.Concat(propertiesFromAttribute);

                foreach (var namedProperty in propertyList)
                {
                    var value = namedProperty.Property.GetValue(issue, null);
                    if (value != null) issueData.Add(namedProperty.FieldName, value);
                }
                //************************************************************

                //************************************************************
                //Customfield name included into property name
                //var propertyList = typeof(TIssueFields).GetProperties().Where(p => p.Name.StartsWith("customfield_"));
                //foreach (var property in propertyList)
                //{
                //    var value = property.GetValue(issue, null);
                //    if (value != null) issueData.Add(property.Name, value);
                //}
                //************************************************************

                request.AddBody(new { fields = issueData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                var issueRef = deserializer.Deserialize<IssueRef>(response);
                return LoadIssue(issueRef);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssue(projectKey, typeCode) error: {0}", ex);
                throw new JiraClientException("Could not create issue", ex);
            }
        }

        public Issue<TIssueFields> UpdateIssue(Issue<TIssueFields> issueFields)
        {
            try
            {
                var path = String.Format("issue/{0}", issueFields.JiraIdentifier);
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var issueData = new Dictionary<string, object>();
                if (issueFields.fields.summary != null)
                    issueData.Add("summary", new[] { new { set = issueFields.fields.summary } });
                if (issueFields.fields.description != null)
                    issueData.Add("description", new[] { new { set = issueFields.fields.description } });
                if (issueFields.fields.labels != null)
                    issueData.Add("labels", new[] { new { set = issueFields.fields.labels } });
                if (issueFields.fields.timetracking != null)
                    issueData.Add("timetracking", new[] { new { set = new { originalEstimate = issueFields.fields.timetracking.originalEstimate } } });

                //************************************************************
                //Customfield name as attribute on a property
                var propertyInfos = typeof(TIssueFields).GetProperties();
                var propertiesFromAttribute = propertyInfos
                    .Select(p => new { Property = p, FieldAttribute = p.GetAttribute<DeserializeAsAttribute>() })
                    .Where(a => a.FieldAttribute != null)
                    .Select(p => new NamedProperty(p.Property, p.FieldAttribute.Name));

                var customFields = propertyInfos
                    .Where(p => p.Name.StartsWith("customfield_"))
                    .Select(p => new NamedProperty(p, p.Name));

                var propertyList = customFields.Concat(propertiesFromAttribute);

                foreach (var namedProperty in propertyList)
                {
                    var value = namedProperty.Property.GetValue(issueFields.fields, null);
                    if (value != null) issueData.Add(namedProperty.FieldName, value);
                }
                //************************************************************

                //************************************************************
                //Customfield name included into property name
                //var propertyList = typeof(TIssueFields).GetProperties().Where(p => p.Name.StartsWith("customfield_"));
                //foreach (var property in propertyList)
                //{
                //    var value = property.GetValue(issue, null);
                //    if (value != null) issueData.Add(property.Name, value);
                //}
                //************************************************************

                request.AddBody(new { update = issueData });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.NoContent);

                return LoadIssue(issueFields);
            }
            catch (Exception ex)
            {
                Trace.TraceError("UpdateIssue(issue) error: {0}", ex);
                throw new JiraClientException("Could not update issue", ex);
            }
        }

        public void DeleteIssue(IssueRef issue)
        {
            try
            {
                var path = String.Format("issue/{0}?deleteSubtasks=true", issue.id);
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
                var path = String.Format("issue/{0}/transitions?expand=transitions.fields", issue.JiraIdentifier);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<TransitionsContainer>(response);
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
                var path = String.Format("issue/{0}/transitions", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var update = new Dictionary<string, object>();
                update.Add("transition", new { id = transition.id });
                if (transition.fields != null)
                    update.Add("fields", transition.fields);

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

        public List<JiraUser> FindUsers(string search)
        {
            try
            {
                var path = String.Format("user/search?username={0}", search);
                var request = CreateRequest(Method.GET, path);
                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var result = deserializer.Deserialize<List<JiraUser>>(response);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("FindUsers(issue) error: {0}", ex);
                throw new JiraClientException(String.Format("Could find user {0}. {1}", search, ex));
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
                var path = String.Format("issue/{0}/watchers", issue.id);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<WatchersContainer>(response).watchers;
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

                return deserializer.Deserialize<List<T>>(response);
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
                var path = String.Format("user/search?username={0}", search);
                var request = CreateRequest(Method.GET, path);
                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var result = deserializer.Deserialize<List<T>>(response);
                return result;
            }
            catch (Exception ex)
            {
                Trace.TraceError("FindUsers(issue) error: {0}", ex);
                throw new JiraClientException(String.Format("Could find user {0}. {1}", search, ex));
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
                var path = String.Format("issue/{0}/comment", issue.id);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<CommentsContainer>(response);
                return data.comments ?? Enumerable.Empty<Comment>();
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetComments(issue) error: {0}", ex);
                throw new JiraClientException("Could not load comments", ex);
            }
        }

        public Comment CreateComment(IssueRef issue, String comment)
        {
            try
            {
                var path = String.Format("issue/{0}/comment", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new Comment { body = comment });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return deserializer.Deserialize<Comment>(response);
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
                var path = String.Format("issue/{0}/comment/{1}", issue.id, comment.id);
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
                var path = String.Format("issue/{0}/worklog", issue.id);
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<WorklogsContainer>(response);
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
                var path = String.Format("issue/{0}/worklog", issue.id);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var insert = new Dictionary<string, object>();
                insert.Add("started", started.ToString("yyyy-MM-ddTHH:mm:ss.fffzz00"));
                insert.Add("comment", comment);
                insert.Add("timeSpentSeconds", timespentSeconds);

                request.AddBody(insert);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return deserializer.Deserialize<Worklog>(response);
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
                var path = string.Format("issue/{0}/worklog/{1}", issue.id, worklog.id);
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (worklog.comment != null) updateData.Add("comment", worklog.comment);
                if (worklog.started != DateTime.MinValue) updateData.Add("started", worklog.started.ToString("yyyy-MM-ddTHH:mm:ss.fffzz00"));
                if (worklog.timeSpentSeconds != 0) updateData.Add("timeSpentSeconds", worklog.timeSpentSeconds);
                request.AddBody(updateData);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<Worklog>(response);
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
                var path = String.Format("issue/{0}/worklog/{1}", issue.id, worklog.id);
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
            return LoadIssue(issue).fields.attachment;
        }

        public Attachment CreateAttachment(IssueRef issue, Stream fileStream, String fileName)
        {
            try
            {
                var path = String.Format("issue/{0}/attachments", issue.JiraIdentifier);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("X-Atlassian-Token", "nocheck");
                request.AddHeader("ContentType", "multipart/form-data");
                request.AddFile("file", stream => fileStream.CopyTo(stream), fileName);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<List<Attachment>>(response).Single();
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
                var path = String.Format("attachment/{0}", attachment.id);
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
            return LoadIssue(issue).fields.issuelinks;
        }

        public IssueLink LoadIssueLink(IssueRef parent, IssueRef child, String relationship)
        {
            try
            {
                var issue = LoadIssue(parent);
                var links = issue.fields.issuelinks
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

        public IssueLink CreateIssueLink(IssueRef parent, IssueRef child, String relationship)
        {
            try
            {
                var request = CreateRequest(Method.POST, "issueLink");
                request.AddHeader("ContentType", "application/json");
                request.AddBody(new
                {
                    type = new { name = relationship },
                    inwardIssue = new { id = parent.id },
                    outwardIssue = new { id = child.id }
                });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                return LoadIssueLink(parent, child, relationship);
            }
            catch (Exception ex)
            {
                Trace.TraceError("CreateIssueLink(parent, child, relationship) error: {0}", ex);
                throw new JiraClientException("Could not link issues", ex);
            }
        }

        public void DeleteIssueLink(IssueLink link)
        {
            try
            {
                var path = String.Format("issueLink/{0}", link.id);
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
                var path = string.Format("issue/{0}/remotelink", issue.id);
                var request = CreateRequest(Method.GET, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<List<RemoteLinkResult>>(response)
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
                var path = string.Format("issue/{0}/remotelink", issue.id);
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
                        url = remoteLink.url,
                        title = remoteLink.title,
                        summary = remoteLink.summary
                    }
                });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.Created);

                //returns: { "id": <id>, "self": <url> }
                var linkId = deserializer.Deserialize<RemoteLink>(response).id;
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
                var path = string.Format("issue/{0}/remotelink/{1}", issue.id, remoteLink.id);
                var request = CreateRequest(Method.PUT, path);
                request.AddHeader("ContentType", "application/json");

                var updateData = new Dictionary<string, object>();
                if (remoteLink.url != null) updateData.Add("url", remoteLink.url);
                if (remoteLink.title != null) updateData.Add("title", remoteLink.title);
                if (remoteLink.summary != null) updateData.Add("summary", remoteLink.summary);
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
                var path = string.Format("issue/{0}/remotelink/{1}", issue.id, remoteLink.id);
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
                var path = string.Format("worklog/updated?since={0}", unixSince);
                var request = CreateRequest(Method.GET, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<WorklogUpdated>(response);
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
                request.AddBody(new { ids = ids });

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<List<Worklog>>(response);
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

                var data = deserializer.Deserialize<List<T>>(response);
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

                var data = deserializer.Deserialize<List<T>>(response);
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

                var data = deserializer.Deserialize<List<T>>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetIssuePriorities() error: {0}", ex);
                throw new JiraClientException("Could not load issue priorities", ex);
            }
        }

        //TODO-BS: PRüfen ob doppelt, dies wäre das "neue"
        public JiraUser GetUser(string username)
        {
            try
            {
                var path = string.Format("user?username={0}", username);
                var request = CreateRequest(Method.POST, path);
                request.AddHeader("ContentType", "application/json");

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                var data = deserializer.Deserialize<JiraUser>(response);
                return data;

            }
            catch (Exception ex)
            {
                Trace.TraceError("GetUser({0}) error: {1}", username, ex);
                throw new JiraClientException("Could not load user", ex);
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

                return deserializer.Deserialize<ServerInfo>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetServerInfo() error: {0}", ex);
                throw new JiraClientException("Could not retrieve server information", ex);
            }
        }

        public IEnumerable<ProjectVersion> GetVersions(String projectKey)
        {
            try
            {
                var path = String.Format("project/{0}/versions", Uri.EscapeUriString(projectKey));
                var request = CreateRequest(Method.GET, path);

                var response = ExecuteRequest(request);
                AssertStatus(response, HttpStatusCode.OK);

                return deserializer.Deserialize<List<ProjectVersion>>(response);
            }
            catch (Exception ex)
            {
                Trace.TraceError("GetVersions(projectKey) error: {0}", ex);
                throw new JiraClientException("Could not load project version", ex);
            }
        }
    }
}
