﻿using log4net;
using System;
using System.Net;
using System.Reflection;
using System.Web.Http;
using Umbraco.Web.WebApi;
using Workflow.Models;
using Workflow.Helpers;
using Workflow.Services;
using System.Threading.Tasks;
using Workflow.Services.Interfaces;

namespace Workflow.Api
{
    [RoutePrefix("siteassist/backoffice/api/workflow/groups")]
    public class GroupsController : UmbracoAuthorizedApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IGroupService _groupService;
        private readonly Utility _utility;

        public GroupsController() : this(new GroupService())
        {
        }

        public GroupsController(IGroupService groupService)
        {
            _groupService = groupService;
            _utility = new Utility();
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
                    UserGroupPoco result = await _groupService.GetUserGroupAsync(id.Value);
                    if (result != null)
                    {
                        return Json(result, ViewHelpers.CamelCase);
                    }
                }
                else
                {
                    return Json(await _groupService.GetUserGroupsAsync(), ViewHelpers.CamelCase);
                }

                throw new HttpResponseException(HttpStatusCode.NotFound);
            }
            catch (Exception e)
            {
                string error = Constants.ErrorGettingGroup.Replace("{id}", id.ToString());
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
        public async Task<IHttpActionResult> Post([FromBody]Model model)
        {
            string name = model.Data;

            try
            {
                UserGroupPoco poco = await _groupService.CreateUserGroupAsync(name);

                // check that it doesn't already exist
                if (poco == null)
                {
                    return Ok(new { status = 200, success = false, msg = Constants.GroupNameExists });
                }

                string msg = Constants.GroupCreated.Replace("{name}", name);
                Log.Debug(msg);

                // return the id of the new group, to update the front-end route to display the edit view
                return Ok(new { status = 200, success = true, msg, id = poco.GroupId });
            }
            catch (Exception ex)
            {
                string error = $"Error creating user group '{name}' - group has likely been deleted. Group names cannot be reused.";
                Log.Error(error, ex);
                // if we are here, something isn't right...
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, error));
            }
        }


        /// <summary>
        /// Save changes to an existing group
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        [HttpPut]
        [Route("save")]
        public async Task<IHttpActionResult> Put(UserGroupPoco group)
        {
            try
            {
                UserGroupPoco result = await _groupService.UpdateUserGroupAsync(group);

                // need to check the new name/alias isn't already in use
                if (result == null)
                {
                    return Content(HttpStatusCode.OK, new { status = 500, msg = Constants.GroupNameExists });
                }

            }
            catch (Exception ex)
            {
                Log.Error(Constants.ErrorUpdatingGroup, ex);

                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, Constants.ErrorUpdatingGroup));
            }

            // feedback to the browser
            string msgText = Constants.GroupUpdated.Replace("{name}", group.Name);
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
        public async Task<IHttpActionResult> Delete(int id)
        {
            // existing workflow processes are left as is, and need to be managed by a human person
            try
            {
                await _groupService.DeleteUserGroupAsync(id);
            }
            catch (Exception ex)
            {
                Log.Error(Constants.ErrorDeletingGroup, ex);
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, Constants.ErrorDeletingGroup));
            }

            // gone.
            Log.Debug($"User group {id} deleted by {_utility.GetCurrentUser()?.Name}");
            return Ok("User group has been deleted");
        }
    }
}