﻿using System;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using log4net;
using Umbraco.Web.WebApi;
using Workflow.Helpers;
using Workflow.Models;
using Workflow.Services;
using Workflow.Services.Interfaces;

namespace Workflow.Api
{
    /// <summary>
    /// Provides an endpoint for exporting the current workflow configuration
    /// </summary>
    [RoutePrefix("siteassist/backoffice/api/workflow/import")]
    public class ImportController : UmbracoAuthorizedApiController
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private readonly IImportExportService _exportService;

        public ImportController() : this(new ImportExportService())
        {
        }

        public ImportController(IImportExportService importExportService)
        {
            _exportService = importExportService;
        }

        /// <summary>
        /// Post an object representing the end-to-end workflow configuration for the current environment. Great for importing somewhere else...
        /// </summary>
        /// <returns></returns>
        [Route("")]
        [HttpPost]
        public async Task<IHttpActionResult> Post([FromBody] ImportExportModel model)
        {
            try
            {
                bool success = await _exportService.Import(model);
                return Ok(success);
            }
            catch (Exception ex)
            {
                const string error = "Error importing workflow configuration";
                Log.Error(error, ex);
                return Content(HttpStatusCode.InternalServerError, ViewHelpers.ApiException(ex, error));
            }
        }
    }
}
