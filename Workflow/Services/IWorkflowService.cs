﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Workflow.Models;

namespace Workflow.Services
{
    public interface IWorkflowService
    {
        Task<UserGroupPoco> GetUserGroupAsync(int id);
        Task<IEnumerable<UserGroupPoco>> GetUserGroupsAsync();
    }
}