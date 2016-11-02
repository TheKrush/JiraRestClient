using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TechTalk.JiraRestClient.Dto;

namespace TechTalk.JiraRestClient.Client
{
    public interface IJiraClient<TIssueFields> where TIssueFields : IssueFields, new()
    {
        /// <Summary>Returns current logged in user</Summary>
        JiraUser GetLoggedInUser();

        /// <summary>Returns a list of projects</Summary>
        IEnumerable<Project> GetProjects();

        /// <Summary>Returns all Issues for the given project</Summary>
        IEnumerable<Issue<TIssueFields>> GetIssues(String projectKey);
        /// <Summary>Returns all Issues of the specified type for the given project</Summary>
        IEnumerable<Issue<TIssueFields>> GetIssues(String projectKey, String issueType);
        /// <Summary>Returns all Issues filtered by only the given JQL query</Summary>
        IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(String jqlQuery);
        /// <Summary>Enumerates through all Issues for the given project</Summary>
        IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey);
        /// <Summary>Enumerates through all Issues of the specified type for the given project</Summary>
        IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey, String issueType);
        /// <Summary>Enumerates through all Issues filtered by the specified jqlQuery starting form the specified startIndex</Summary>
        IEnumerable<Issue<TIssueFields>> EnumerateIssuesByQuery(String jqlQuery, String[] fields, Int32 startIndex);
        /// <Summary>Returns a query provider for this JIRA connection</Summary>
        IQueryable<Issue<TIssueFields>> QueryIssues();

        /// <Summary>Returns all Issues of the given type and the given project filtered by the given JQL query</Summary>
        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, Fields, startIndex).ToArray() instead")]
        IEnumerable<Issue<TIssueFields>> GetIssuesByQuery(String projectKey, String issueType, String jqlQuery);
        /// <Summary>Enumerates through all Issues of the specified type for the given project, returning the given issue Fields</Summary>
        [Obsolete("This method is no longer supported and might be removed in a later release. Use EnumerateIssuesByQuery(jqlQuery, Fields, startIndex) instead")]
        IEnumerable<Issue<TIssueFields>> EnumerateIssues(String projectKey, String issueType, String fields);

        /// <Summary>Returns the issue identified by the given ref</Summary>
        Issue<TIssueFields> LoadIssue(String issueRef);
        /// <Summary>Returns the issue identified by the given ref</Summary>
        Issue<TIssueFields> LoadIssue(IssueRef issueRef);
        /// <Summary>Creates an issue of the specified type for the given project</Summary>
        Issue<TIssueFields> CreateIssue(String projectKey, String issueType, String summary);
        /// <Summary>Creates an issue of the specified type for the given project</Summary>
        Issue<TIssueFields> CreateIssue(String projectKey, String issueType, TIssueFields issue);        
        /// <Summary>Updates the given issue on the remote system</Summary>
        Issue<TIssueFields> UpdateIssue(Issue<TIssueFields> issueFields);
        /// <Summary>Deletes the given issue from the remote system</Summary>
        void DeleteIssue(IssueRef issue);

        /// <Summary>Returns all transitions avilable to the given issue</Summary>
        IEnumerable<Transition> GetTransitions(IssueRef issue);
        /// <Summary>Changes the state of the given issue as described by the transition</Summary>
        Issue<TIssueFields> TransitionIssue(IssueRef issue, Transition transition);

        /// <Summary>Returns all watchers for the given issue</Summary>
        IEnumerable<JiraUser> GetWatchers(IssueRef issue);

        List<T> GetProjects<T>() where T : JiraProject;

        /// <summary>Finds the users specified by the search </summary>    
        /// <param name="search">Text to search on (user name, email addres)</param>    
        IEnumerable<JiraUser> FindUsers(string search);

        /// <summary>Returns the first user specified by the search</summary>    
		/// <param name="search">Text to search on (user name, email addres)</param>  
        JiraUser FindUser(string search);
        
        /// <Summary>Returns all Comments for the given issue</Summary>
        IEnumerable<Comment> GetComments(IssueRef issue);
        /// <Summary>Adds a comment to the given issue</Summary>
        Comment CreateComment(IssueRef issue, String comment);
        /// <summary>Update a comment to the given issue and comment</summary>
        Comment UpdateComment(IssueRef issue, Comment comment);
        /// <Summary>Deletes the given comment</Summary>
        void DeleteComment(IssueRef issue, Comment comment);

        /// <Summary>Returns all worklogs for the given issue</Summary>
        IEnumerable<Worklog> GetWorklogs(IssueRef issue);

        /// <Summary>Adds a worklog to the given issue</Summary>
        Worklog CreateWorklog(IssueRef issue, int timespentSeconds, string comment, DateTime started);
        /// <Summary>Update a worklog to the given issue</Summary>
        Worklog UpdateWorklog(IssueRef issue, Worklog worklog);
        /// <Summary>Delete a worklog to the given issue</Summary>
        void DeleteWorklog(IssueRef issue, Worklog worklog);

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

        /// <Summary>Returns worklogs Id and update time of worklogs that was updated since given time</Summary>
        WorklogUpdated GetWorklogUpdated(DateTime since);
        /// <Summary>Returns worklogs for given worklog ids</Summary>
        IEnumerable<Worklog> GetWorklogList(int[] ids);

        /// <Summary>Returns all issue types</Summary>
        IEnumerable<T> GetIssueTypes<T>() where T : IssueType;

        /// <Summary>Returns all issue statuses</Summary>
        IEnumerable<T> GetIssueStatuses<T>() where T : Status;

        /// <Summary>Returns all issue priorities</Summary>
        IEnumerable<T> GetIssuePriorities<T>() where T : IssuePriority;

        /// <Summary>Returns information about the JIRA server</Summary>
        ServerInfo GetServerInfo();

        /// <Summary>Gets all the versions for a project</Summary>
        IEnumerable<ProjectVersion> GetVersions(String projectKey);
    }
}
