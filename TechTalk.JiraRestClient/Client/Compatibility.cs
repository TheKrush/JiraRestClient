using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TechTalk.JiraRestClient.Dto;

namespace TechTalk.JiraRestClient.Client
{
    public interface IJiraClient
    {        
        /// <Summary>Returns a list of projects</Summary>
        IEnumerable<Project> GetProjects();

        /// <Summary>Returns all Issues for the given project</Summary>
        IEnumerable<Issue> GetIssues(String projectKey);
        /// <Summary>Returns all Issues of the specified type for the given project</Summary>
        IEnumerable<Issue> GetIssues(String projectKey, String issueType);
        /// <Summary>Enumerates through all Issues for the given project</Summary>
        IEnumerable<Issue> EnumerateIssues(String projectKey);
        /// <Summary>Enumerates through all Issues of the specified type for the given project</Summary>
        IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType);

        /// <Summary>Returns all Issues of the given type and the given project filtered by the given JQL query</Summary>
        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        IEnumerable<Issue> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery);
        /// <Summary>Enumerates through all Issues of the specified type for the given project, returning the given issue Fields</Summary>
        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType, String fields);

        /// <Summary>Returns the issue identified by the given ref</Summary>
        Issue LoadIssue(String issueRef);
        /// <Summary>Returns the issue identified by the given ref</Summary>
        Issue LoadIssue(IssueRef issueRef);
        /// <Summary>Creates an issue of the specified type for the given project</Summary>
        Issue CreateIssue(String projectKey, String issueType, String summary);
        /// <Summary>Creates an issue of the specified type for the given project</Summary>
        Issue CreateIssue(String projectKey, String issueType, IssueFields issueFields);
        /// <Summary>Updates the given issue on the remote system</Summary>
        Issue UpdateIssue(Issue issue);
        /// <Summary>Deletes the given issue from the remote system</Summary>
        void DeleteIssue(IssueRef issue);

        /// <Summary>Returns all transitions available to the given issue</Summary>
        IEnumerable<Transition> GetTransitions(IssueRef issue);
        /// <Summary>Changes the state of the given issue as described by the transition</Summary>
        Issue TransitionIssue(IssueRef issue, Transition transition);

        /// <Summary>Returns all watchers for the given issue</Summary>
        IEnumerable<JiraUser> GetWatchers(IssueRef issue);

        /// <Summary>Returns all Comments for the given issue</Summary>
        IEnumerable<Comment> GetComments(IssueRef issue);
        /// <Summary>Adds a comment to the given issue</Summary>
        Comment CreateComment(IssueRef issue, String comment);
        /// <summary>Update a comment to the given issue and comment</summary>
        Comment UpdateComment(IssueRef issue, Comment comment);        
        /// <Summary>Deletes the given comment</Summary>
        void DeleteComment(IssueRef issue, Comment comment);

        /// <Summary>Return all attachments for the given issue</Summary>
        IEnumerable<Attachment> GetAttachments(IssueRef issue);
        /// <Summary>Creates an attachment to the given issue</Summary>
        Attachment CreateAttachment(IssueRef issue, Stream stream, String fileName);
        /// <Summary>Deletes the given attachment</Summary>
        void DeleteAttachment(Attachment attachment);

        /// <Summary>Returns all links for the given issue</Summary>
        IEnumerable<IssueLink> GetIssueLinks(IssueRef issue);
        /// <Summary>Returns the link between two Issues of the given relation</Summary>
        IssueLink LoadIssueLink(IssueRef parent, IssueRef child, String relationship);
        /// <Summary>Creates a link between two Issues with the given relation</Summary>
        IssueLink CreateIssueLink(IssueRef parent, IssueRef child, String relationship);
        /// <Summary>Removes the given link of two Issues</Summary>
        void DeleteIssueLink(IssueLink link);

        /// <Summary>Returns all remote links (attached urls) for the given issue</Summary>
        IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue);
        /// <Summary>Creates a remote link (attached url) for the given issue</Summary>
        RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink);
        /// <Summary>Updates the given remote link (attached url) of the specified issue</Summary>
        RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink);
        /// <Summary>Removes the given remote link (attached url) of the specified issue</Summary>
        void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink);

        /// <Summary>Returns worklogs Id and update time of worklogs that was updated since given time.</Summary>
        WorklogUpdated GetWorklogUpdated(DateTime since);
        /// <Summary>Returns worklogs for given worklog ids</Summary>
        IEnumerable<Worklog> GetWorklogList(int[] ids);

        /// <Summary>Returns all issue types</Summary>
        IEnumerable<IssueType> GetIssueTypes();

        /// <Summary>Returns information about the JIRA server</Summary>
        ServerInfo GetServerInfo();
    }

    public class JiraClient : IJiraClient
    {
        private readonly IJiraClient<IssueFields> _client;
        public JiraClient(string baseUrl, string username, string password)
        {
            _client = new JiraClient<IssueFields>(baseUrl, username, password);
        }

        public IEnumerable<Project> GetProjects()
        {
            return _client.GetProjects();
        }

        public IEnumerable<Issue> GetIssues(String projectKey)
        {
            return _client.GetIssues(projectKey).Select(Issue.From).ToArray();
        }

        public IEnumerable<Issue> GetIssues(String projectKey, String issueType)
        {
            return _client.GetIssues(projectKey, issueType).Select(Issue.From).ToArray();
        }

        public IEnumerable<Issue> EnumerateIssues(String projectKey)
        {
            return _client.EnumerateIssues(projectKey).Select(Issue.From);
        }

        public IEnumerable<Issue> EnumerateIssues(String projectKey, String issueType)
        {
            return _client.EnumerateIssues(projectKey, issueType).Select(Issue.From);
        }

        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        public IEnumerable<Issue> GetIssuesByQuery(string projectKey, string issueType, string jqlQuery)
        {
            return _client.GetIssuesByQuery(projectKey, issueType, jqlQuery).Select(Issue.From).ToArray();
        }

        [Obsolete("This method is no longer supported and might be removed in a later release.")]
        public IEnumerable<Issue> EnumerateIssues(string projectKey, string issueType, string fields)
        {
            return _client.EnumerateIssues(projectKey, issueType, fields).Select(Issue.From);
        }

        public Issue LoadIssue(String issueRef)
        {
            return Issue.From(_client.LoadIssue(issueRef));
        }

        public Issue LoadIssue(IssueRef issueRef)
        {
            return Issue.From(_client.LoadIssue(issueRef));
        }

        public Issue CreateIssue(String projectKey, String issueType, String summary)
        {
            return Issue.From(_client.CreateIssue(projectKey, issueType, summary));
        }

        public Issue CreateIssue(String projectKey, String issueType, IssueFields issueFields)
        {
            return Issue.From(_client.CreateIssue(projectKey, issueType, issueFields));
        }

        public Issue UpdateIssue(Issue issue)
        {
            return Issue.From(_client.UpdateIssue(issue));
        }

        public void DeleteIssue(IssueRef issue)
        {
            _client.DeleteIssue(issue);
        }

        public IEnumerable<Transition> GetTransitions(IssueRef issue)
        {
            return _client.GetTransitions(issue);
        }

        public Issue TransitionIssue(IssueRef issue, Transition transition)
        {
            return Issue.From(_client.TransitionIssue(issue, transition));
        }
        
        public IEnumerable<JiraUser> GetWatchers(IssueRef issue)
        {
            return _client.GetWatchers(issue);
        }


        public IEnumerable<JiraUser> FindUsers(string search)
        {
            return _client.FindUsers(search);
        }

        public JiraUser FindUser(string search)
        {
            return _client.FindUser(search);
        }

        public IEnumerable<Comment> GetComments(IssueRef issue)
        {
            return _client.GetComments(issue);
        }

        public Comment CreateComment(IssueRef issue, string comment)
        {
            return _client.CreateComment(issue, comment);
        }

        public Comment UpdateComment(IssueRef issue, Comment comment)
        {
            return _client.UpdateComment(issue, comment);
        }

        public void DeleteComment(IssueRef issue, Comment comment)
        {
            _client.DeleteComment(issue, comment);
        }

        public IEnumerable<Worklog> GetWorklogs(IssueRef issue)
        {
            return _client.GetWorklogs(issue);
        }

        public Worklog CreateWorklog(IssueRef issue, int timespentSeconds, string comment, DateTime started)
        {
            return _client.CreateWorklog(issue, timespentSeconds, comment, started);
        }

        public Worklog UpdateWorklog(IssueRef issue, Worklog worklog)
        {
            return _client.UpdateWorklog(issue, worklog);
        }

        public void DeleteWorklog(IssueRef issue, Worklog worklog)
        {
            _client.DeleteWorklog(issue, worklog);
        }

        public IEnumerable<Attachment> GetAttachments(IssueRef issue)
        {
            return _client.GetAttachments(issue);
        }

        public Attachment CreateAttachment(IssueRef issue, Stream stream, string fileName)
        {
            return _client.CreateAttachment(issue, stream, fileName);
        }

        public void DeleteAttachment(Attachment attachment)
        {
            _client.DeleteAttachment(attachment);
        }

        public IEnumerable<IssueLink> GetIssueLinks(IssueRef issue)
        {
            return _client.GetIssueLinks(issue);
        }

        public IssueLink LoadIssueLink(IssueRef parent, IssueRef child, string relationship)
        {
            return _client.LoadIssueLink(parent, child, relationship);
        }

        public IssueLink CreateIssueLink(IssueRef parent, IssueRef child, string relationship)
        {
            return _client.CreateIssueLink(parent, child, relationship);
        }

        public void DeleteIssueLink(IssueLink link)
        {
            _client.DeleteIssueLink(link);
        }

        public IEnumerable<RemoteLink> GetRemoteLinks(IssueRef issue)
        {
            return _client.GetRemoteLinks(issue);
        }

        public RemoteLink CreateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            return _client.CreateRemoteLink(issue, remoteLink);
        }

        public RemoteLink UpdateRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            return _client.UpdateRemoteLink(issue, remoteLink);
        }

        public void DeleteRemoteLink(IssueRef issue, RemoteLink remoteLink)
        {
            _client.DeleteRemoteLink(issue, remoteLink);
        }

        public WorklogUpdated GetWorklogUpdated(DateTime since)
        {
            return _client.GetWorklogUpdated(since);
        }

        public IEnumerable<Worklog> GetWorklogList(int[] ids)
        {
            return _client.GetWorklogList(ids);
        }

        public IEnumerable<IssueType> GetIssueTypes()
        {
            return _client.GetIssueTypes<IssueType>();
        }

        public IEnumerable<Status> GetIssueStatuses()
        {
            return _client.GetIssueStatuses<Status>();
        }

        public IEnumerable<IssuePriority> GetIssuePriorities()
        {
            return _client.GetIssuePriorities<IssuePriority>();
        }

        public ServerInfo GetServerInfo()
        {
            return _client.GetServerInfo();
        }

        public IEnumerable<ProjectVersion> GetVersions(string projectKey)
        {
            return _client.GetVersions(projectKey);
        }
    }

    public class Issue : Issue<IssueFields>
    {
        internal static Issue From(Issue<IssueFields> other)
        {
            if (other == null)
                return null;

            var issue =  new Issue
            {
                expand = other.expand,
                id = other.id,
                key = other.key,
                self = other.self,
                fields = other.fields,
            };

            //TOTO-BS: Memento / ChangeTracker implementieren => Klasse kopieren in private property...
            //Hier beginnt das "Change" event .. 
            //Wenn dies implementiert wird, dann müsste bei Update Issue dies auch berücksichtigt werden!
            //quick and dirty example: http://www.sullinger.us/blog/2014/1/22/quick-dirty-object-change-tracking-in-c
            //issue.Fields.ModifiedProperties.Clear();

            return issue;
        }
    }
}
