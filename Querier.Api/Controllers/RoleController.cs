using Querier.Api.Models.Requests.Role;
using Querier.Api.Models.Responses.Role;
using Querier.Api.Services.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RoleController : ControllerBase
    {
        private readonly IRoleService _svc;

        public RoleController(IRoleService svc)
        {
            _svc = svc;
        }

        [HttpGet("GetAll")]
        [ProducesResponseType(typeof(List<RoleResponse>), 200)]
        public async Task<IActionResult> GetAllAsync()
        {
            return Ok(await _svc.GetAll());
        }

        [HttpPost("AddRole")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AddRoleAsync(RoleRequest role)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Add(role));
        }

        [HttpPost("UpdateRole")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> UpdateRoleAsync(RoleRequest role)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Edit(role));
        }

        [HttpDelete("DeleteRole/{id}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> DeleteRoleAsync(string id)
        {
            if (!ModelState.IsValid)
                return BadRequest();
            return Ok(await _svc.Delete(id));
        }

        [HttpGet("Categories")]
        [ProducesResponseType(typeof(List<CategoryActionsList>), 200)]
        public async Task<IActionResult> CategoriesAsync()
        {
            return Ok(await _svc.GetCategories());
        }

        [HttpPost("UpdateRoleActions")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> UpdateRoleActionsAsync([FromBody] CategoryActionsList[] actions)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updated = await _svc.UpdateCategories(actions);
            if (updated)
                return Ok(updated);
            return BadRequest();
        }

        [HttpPost("AddActionsMissing")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AddActionsMissing([FromBody] ActionsMissing actions)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest();
            }

            var updated = await _svc.AddActionsMissing(actions);
            if (updated)
                return Ok(updated);
            return BadRequest();
        }

        [HttpGet("GetAllRolesAndPagesAndRelationBetween")]
        [ProducesResponseType(typeof(GetAllRolesAndPagesAndRelationBetweenResponse), 200)]
        public async Task<IActionResult> GetAllRolesAndPagesAndRelationBetween()
        {
            return new OkObjectResult(await _svc.GetAllRolesAndPagesAndRelationBetween());
        }

        [HttpPost("AddOrRemoveRoleViewOnPage")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> AddOrRemoveRoleViewOnPage([FromBody] ModifyRoleViewOnPageRequest request)
        {
            return new OkObjectResult(await _svc.AddOrRemoveRoleViewOnPage(request));
        }

        [HttpPost("InsertViewPageRole")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> InsertViewPageRole([FromBody] InsertViewPageRoleRequest request)
        {
            return new OkObjectResult(await _svc.InsertViewPageRole(request));
        }

        [HttpGet("GetRolesForUser/{idUser}")]
        [ProducesResponseType(typeof(List<RoleResponse>), 200)]
        public async Task<IActionResult> GetRolesForUser(string idUser)
        {
            return Ok(await _svc.GetRolesForUser(idUser));
        }

        [HttpGet("GetCurrentUserRoles")]
        [ProducesResponseType(typeof(List<RoleResponse>), 200)]
        public async Task<IActionResult> GetCurrentUserRole()
        {
            var userId = this.User.Claims.FirstOrDefault(c => c.Type == "Id")?.Value;
            return Ok(await _svc.GetRolesForUser(userId));
        }

        [ProducesResponseType(typeof(List<GetAllPagesWithRolesResponse>), 200)]
        [HttpGet("GetAllPagesWithRoles")]
        public async Task<IActionResult> GetAllPagesWithRoles()
        {
            return Ok(await _svc.GetAllPagesWithRoles());
        }
    }
}
