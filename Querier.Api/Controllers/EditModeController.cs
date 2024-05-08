using Querier.Api.Models;
using Querier.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EditModeController : ControllerBase
    {
        private readonly ILogger<EditModeController> _logger;
        private readonly IEditModeService _editModeService;

        public EditModeController(IEditModeService editPermissionService, ILogger<EditModeController> logger)
        {
            _logger = logger;
            _editModeService = editPermissionService;
        }
        [AllowAnonymous]
        [HttpGet("GetAuth")]
        public IActionResult GetAuth()
        {
            var claims = this.User.Claims.FirstOrDefault(c => c.Type == "Id");
            if (claims == null)
                return new OkObjectResult(_editModeService.GetAuth(new List<IdentityRole>()));
            var userId = claims.Value;

            var userRoles = _editModeService.GetRolesForUser(userId);

            return new OkObjectResult(_editModeService.GetAuth(userRoles));
        }
    }
}
