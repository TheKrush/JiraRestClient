using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using RestSharp.Deserializers;
using TechTalk.JiraRestClient.Attributes;

#pragma warning disable 169

namespace TechTalk.JiraRestClient.Dto
{
    //TODO: Impl. SerializeAs (open RestSharp -> PR)  and DeserializeAs to fix property naming (all Dto classes!)
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    [SuppressMessage("ReSharper", "CollectionNeverUpdated.Global")]
    public class IssueFields
    {
        private string _summary;
        private string _description;
        private int _serialNumber;

        public IssueFields()
        {
            Labels = new List<string>();
            Status = new Status();
            TimeTracking = new Timetracking();
            Project = new Project();
            Type = new IssueType();
            Comments = new List<Comment>();
            Worklogs = new List<Worklog>();
            IssueLinks = new List<IssueLink>();
            Attachment = new List<Attachment>();
            Watchers = new List<JiraUser>();
        }

        [DeserializeAs(Name = "summary")]
        [FieldInformation(UpdateRelevant = true)]
        public string Summary { get; set; }
        [DeserializeAs(Name = "description")]
        [FieldInformation(UpdateRelevant = true)]
        public string Description { get; set; }
        /// <summary>
        /// Change JiraUser -> "Name" (login) to udate an issue
        /// /// Or use .FindUser("Firstname.Lastname@domain.com") || .FindUser("SamAccountName")
        /// </summary>
        [DeserializeAs(Name = "reporter")]
        [FieldInformation(UpdateRelevant = true)]
        public JiraUser Reporter { get; set; }
        /// <summary>
        /// Change JiraUser -> "Name" (login) to udate an issue
        /// Or use .FindUser("Firstname.Lastname@domain.com") || .FindUser("SamAccountName")
        /// Set Unassigned => fields.Assignee.name = "";
        /// </summary>
        [DeserializeAs(Name = "assignee")]
        [FieldInformation(UpdateRelevant = true)]
        public JiraUser Assignee { get; set; }
        [DeserializeAs(Name = "labels")]
        [FieldInformation(UpdateRelevant = true)]
        public List<string> Labels { get; private set; }
        [DeserializeAs(Name = "comments")]
        [FieldInformation(UpdateRelevant = true, SpecialFunction = SpecialFunction.Comments)]
        public List<Comment> Comments { get; set; }
        //There is no move function, not impl. yet! https://jira.atlassian.com/browse/JRA-16494
        [DeserializeAs(Name = "project")]
        public Project Project { get; private set; }
        //There is no function to change the issue type .. TODO: Find a function to change the issuetype
        [DeserializeAs(Name = "issuetype")]
        [FieldInformation(UpdateRelevant = true)]
        public IssueType Type { get; set; }
        [DeserializeAs(Name = "timetracking")]
        public Timetracking TimeTracking { get; set; }
        [DeserializeAs(Name = "status")]
        public Status Status { get; set; }
        [DeserializeAs(Name = "watchers")]
        public List<JiraUser> Watchers { get; set; }
        [DeserializeAs(Name = "worklogs")]
        public List<Worklog> Worklogs { get; set; }
        [DeserializeAs(Name = "issuelinks")]
        public List<IssueLink> IssueLinks { get; set; }
        [DeserializeAs(Name = "attachment")]
        public List<Attachment> Attachment { get; set; }
        /// <Summary>
        /// remaining time estimate in seconds
        /// </Summary>
        [DeserializeAs(Name = "timeestimate")]
        public int? TimeEstimate { get; set; }
        /// <Summary>
        /// original time estimate in seconds
        /// </Summary>
        [DeserializeAs(Name = "timeoriginalestimate")]
        public int? TimeOriginalEstimate { get; set; }
        /// <Summary>
        /// time logged in seconds
        /// </Summary>
        [DeserializeAs(Name = "timespent")]
        public int? TimeSpent { get; set; }
        [DeserializeAs(Name = "resolution")]
        public Resolution Resolution { get; set; }
        [DeserializeAs(Name = "resolutiondate")]
        public DateTime? ResolutionDate { get; set; }
        [DeserializeAs(Name = "priority")]
        public Priority Priority { get; set; }

        #region Jira specific fields (new customfields)
        [DeserializeAs(Name = "customfield_10502")]
        [FieldInformation(UpdateRelevant = true, NullableAllowed = true)]
        public int? SerialNumber { get; set; }
        #endregion
    }
}
