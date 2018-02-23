﻿using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Web.Http;
using Umbraco.Core.Persistence;
using Umbraco.Web.WebApi;
using Workflow.Models;
using Workflow.Helpers;
using Workflow.Services;
using System.Threading.Tasks;

namespace Workflow.Api
{
    [RoutePrefix("umbraco/backoffice/api/workflow/groups")]
    public class GroupsController : UmbracoAuthorizedApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly PocoRepository Pr;
        private readonly IWorkflowService workflowService;

        public GroupsController()
        {
            Pr = new PocoRepository(DatabaseContext.Database);
            workflowService = new WorkflowService();
        }

        /// <summary>
        /// Get group and associated users and permissions by id
        /// </summary>
        /// <param name="id">Optional, returns all groups if omitted</param>
        /// <returns></returns>
        [Route("get/{id:int?}")]
        public async Task<IHttpActionResult> Get(int? id = null)
        {
            try
            {
                if (id.HasValue)
                {
                    var result = await workflowService.GetUserGroupAsync(id.Value);
                    if (result != null)
                        return Json(result, ViewHelpers.CamelCase);
                }
                else
                {
                    return Json(await workflowService.GetUserGroupsAsync(), ViewHelpers.CamelCase);
                }

                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                string error = $"Error getting group by id {id}";
                Log.Error(error, e);
                // if we are here, something isn't right...
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(e, error));
            }
        }

        /// <summary>
        /// Add a new group
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("add")]
        public IHttpActionResult Post([FromBody]Model model)
        {
            string name = model.Data;

            try
            {
                // check that it doesn't already exist
                if (Pr.UserGroupsByName(name).Any())
                {
                    return Ok(new { status = 500, msg = "Group name already exists" });
                }

                // doesnt exist so create it with the given name. The alias will be generated from the name.
                DatabaseContext.Database.Insert(new UserGroupPoco
                {
                    Name = name,
                    Alias = name.ToLower().Replace(" ", "-"),
                    Deleted = false
                });
            }
            catch (Exception ex)
            {
                string error = $"Error creating user group '{name}'";
                Log.Error(error, ex);
                // if we are here, something isn't right...
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, error));
            }

            int id = Pr.NewestGroup().GroupId;

            string msg = $"Successfully created new user group '{name}'.";
            Log.Debug(msg);

            // return the id of the new group, to update the front-end route to display the edit view
            return Ok(new { status = 200, msg, id });
        }


        /// <summary>
        /// Save changes to an existing group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("save")]
        public IHttpActionResult Put(UserGroupPoco group)
        {
            bool nameExists = Pr.UserGroupsByName(group.Name).Any();
            bool aliasExists = Pr.UserGroupsByAlias(group.Alias).Any();

            try
            {
                UserGroupPoco userGroup = Pr.UserGroupsById(group.GroupId).First();

                // need to check the new name/alias isn't already in use
                if (userGroup.Name != group.Name && nameExists)
                {
                    return Content(HttpStatusCode.OK, new { status = 500, msg = "Group name already exists" });
                }

                if (userGroup.Alias != group.Alias && aliasExists)
                {
                    return Content(HttpStatusCode.OK, new { status = 500, msg = "Group alias already exists" });
                }

                // Update the Members - TODO - should find a more efficient way to do this...
                Database db = DatabaseContext.Database;

                db.Execute("DELETE FROM WorkflowUser2UserGroup WHERE GroupId = @0", userGroup.GroupId);

                if (group.Users.Count > 0)
                {
                    foreach (User2UserGroupPoco user in group.Users)
                    {
                        db.Insert(user);
                    }
                }

                db.Update(group);

            }
            catch (Exception ex)
            {
                const string msg = "An error occurred updating the user group";
                Log.Error(msg, ex);

                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, msg));
            }

            // feedback to the browser
            string msgText = $"User group '{group.Name}' has been saved.";
            Log.Debug(msgText);

            return Ok(new { status = 200, msg = msgText });
        }

        /// <summary>
        /// Delete group
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        [Route("delete/{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            // existing workflow processes are left as is, and need to be managed by a human person
            try
            {
                DatabaseContext.Database.Execute("UPDATE WorkflowUserGroups SET Deleted = 1 WHERE GroupId = @0", id);
            }
            catch (Exception ex)
            {
                const string msg = "Error deleting user group";
                Log.Error(msg, ex);
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, msg));
            }

            // gone.
            return Ok("User group has been deleted");
        }
    }
}