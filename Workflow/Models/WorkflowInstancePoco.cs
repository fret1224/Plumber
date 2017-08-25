﻿using System;
using System.Collections.Generic;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Core.Models.Membership;
using Umbraco.Core.Persistence;
using Umbraco.Core.Persistence.DatabaseAnnotations;
using Umbraco.Core.Services;
using Workflow.Helpers;

namespace Workflow.Models
{
    [TableName("WorkflowInstance")]
    [ExplicitColumns]
    [PrimaryKey("Id", autoIncrement = true)]
    public sealed class WorkflowInstancePoco
    {
        private IPublishedContent _node;
        private IUser _authorUser;
        private readonly IContentService _cs = ApplicationContext.Current.Services.ContentService;

        public WorkflowInstancePoco()
        {
            TaskInstances = new HashSet<WorkflowTaskInstancePoco>();
            Status = (int)WorkflowStatus.PendingApproval;
            CreatedDate = DateTime.Now;
            CompletedDate = null;
        }

        public WorkflowInstancePoco(int nodeId, int authorUserId, string authorComment, WorkflowType type) : this()
        {
            NodeId = nodeId;
            AuthorUserId = authorUserId;
            AuthorComment = authorComment;
            Type = (int)type;
        }

        [Column("Id")]
        [PrimaryKeyColumn(AutoIncrement = true)]
        public int Id { get; set; }

        [Column("Guid")]
        public Guid Guid { get; set; }

        [Column("NodeId")]
        public int NodeId { get; set; }

        [Column("Type")]
        public int Type { get; set; }

        [Column("TotalSteps")]
        public int TotalSteps { get; set; }

        [Column("AuthorUserId")]
        public int AuthorUserId { get; set; }

        [Column("Status")]
        public int Status { get; set; }

        [Column("CreatedDate")]
        public DateTime CreatedDate { get; set; }

        [Column("AuthorComment")]
        public string AuthorComment { get; set; }

        [ResultColumn]
        public WorkflowStatus _Status => (WorkflowStatus)Status;

        [ResultColumn]
        public WorkflowType _Type => (WorkflowType)Type;

        public void SetScheduledDate()
        {
            var content = _cs.GetById(NodeId);
            if (Type ==  (int)WorkflowType.Publish && content.ReleaseDate.HasValue)
            {
                ScheduledDate = content.ReleaseDate;
            }
            else if (Type == (int)WorkflowType.Unpublish && content.ExpireDate.HasValue)
            {
                ScheduledDate = content.ExpireDate;
            }
            else
            {
                ScheduledDate = null;
            }
        }

        /// <summary>
        /// Title case text name for the workflow type.
        /// </summary>
        [ResultColumn]
        public string TypeName => WorkflowTypeName(_Type);

        [ResultColumn]
        public string TypeDescriptionPastTense => TypeDescription.Replace("ish", "ished").Replace("dule", "duled").Replace("for", "to be");

        /// <summary>
        /// Describe the workflow type by including details for release at / expire at scheduling.
        /// </summary>
        [ResultColumn]
        public string TypeDescription => WorkflowTypeDescription(_Type, ScheduledDate);

        /// <summary>
        /// The document object associated with this workflow.
        /// </summary>
        [ResultColumn]
        public IPublishedContent Node => _node ?? (_node = Utility.GetNode(NodeId));

        /// <summary>
        /// The author user who initiated this workflow instance.
        /// </summary>
        [ResultColumn]
        public IUser AuthorUser => _authorUser ?? (_authorUser = Utility.GetUser(AuthorUserId));

        /// <summary>
        /// Title case text name for the workflow status.
        /// </summary>
        [ResultColumn]       
        public string StatusName => Utility.PascalCaseToTitleCase(_Status.ToString());

        /// <summary>
        /// Indicates whether the workflow instance is currently active.
        /// </summary>
        [ResultColumn]        
        public bool Active => _Status != WorkflowStatus.Cancelled && _Status != WorkflowStatus.Rejected;

        [ResultColumn]
        public DateTime? CompletedDate { get; set; }

        [ResultColumn]
        public DateTime? ScheduledDate { get; set; }

        [ResultColumn]
        public ICollection<WorkflowTaskInstancePoco> TaskInstances { get; set; }

        public static string WorkflowTypeDescription(WorkflowType type, DateTime? scheduledDate)
        {
            if (scheduledDate.HasValue)
            {
                return "Schedule for " + WorkflowTypeName(type) + " at " + scheduledDate.Value.ToString("dd/MM/yy HH:mm");
            }

            return WorkflowTypeName(type);
        }

        public static string WorkflowTypeName(WorkflowType type)
        {
            return Utility.PascalCaseToTitleCase(type.ToString());
        }

        public static string EmailTypeName(EmailType type)
        {
            return Utility.PascalCaseToTitleCase(type.ToString());
        }
    }
}
