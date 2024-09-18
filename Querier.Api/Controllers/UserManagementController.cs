using Querier.Api.Models;
using Querier.Api.Models.Datatable;
using Querier.Api.Models.Requests.User;
using Querier.Api.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Querier.Api.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserManagementController : ControllerBase
    {
        private readonly IUserService _svc;

        public UserManagementController(IUserService svc)
        {
            _svc = svc;
        }

        /// <summary>
        /// Get a user by it's Id
        /// </summary>
        /// <param name="id">User Identifier</param>
        /// <returns></returns>
        [HttpGet("View/{id}")]
        public async Task<IActionResult> ViewAsync(string id)
        {
            if (string.IsNullOrEmpty(id))
                return BadRequest("Request parameter is not valid");

            return Ok(await _svc.View(id));
        }

        /// <summary>
        /// Used to update a new user
        /// </summary>
        /// <param name="user">The user registration model</param>
        /// <returns>Return an htttp response which holds errors or success code</returns>
        [HttpPut]
        [Route("Add")]
        public async Task<IActionResult> AddAsync([FromBody] UserRequest user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Request body is not valid");
            }

            if (await _svc.Add(user))
                return Ok(true);
            else
                return StatusCode(500);
        }

        /// <summary>
        /// Used to update a new user
        /// </summary>
        /// <param name="user">The user registration model</param>
        /// <returns>Return an htttp response which holds errors or success code</returns>
        [HttpPut]
        [Route("Update")]
        public async Task<IActionResult> UpdateAsync([FromBody] UserRequest user)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Request body is not valid");
            }

            if (await _svc.Update(user))
                return Ok(true);
            else
                return StatusCode(500);
        }

        /// <summary>
        /// Used to delete a user
        /// </summary>
        /// <param name="id">The user's model</param>
        /// <returns>Return an htttp response which holds errors or success code</returns>
        [HttpDelete]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Request is not valid");
            }

            var res = await _svc.Delete(id);
            if (res)
                return Ok(res);

            return BadRequest($"Cannot delete user with id = {id}");
        }

        /// <summary>
        /// Used to have all users in Datatable
        /// </summary>
        /// <returns>Return a list of users with servisideResponse</returns>
        [HttpPost]
        [Route("GetUsers")]
        public async Task<ActionResult> GetUsersAync([FromBody] ServerSideRequest datatableRequest)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Request is not valid");
            }

            var res = await _svc.GetAll(datatableRequest);
            return Ok(res);
        }

        /// <summary>
        /// Used to have all users
        /// </summary>
        /// <returns>Return a list of users</returns>
        [HttpGet]
        [Route("GetAll")]
        public async Task<ActionResult> GetAllAync()
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Request is not valid");
            }

            var res = await _svc.GetAll();
            return Ok(res);
        }

        /// <summary>
        /// Used to send mail for change password
        /// </summary>
        /// <param name="user_mail">The model of ForgotPassword contains the email of the user</param>
        /// <returns>Return if the mail has been send or not + message description</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("SendMailForForgotPassword")]
        public async Task<ActionResult> SendMailForForgotPassword([FromBody] SendMailForgotPassword user_mail)
        {
            var response = await _svc.SendMailForForgotPassword(user_mail);
            return Ok(response);
        }

        /// <summary>
        /// Used to reset password of the user 
        /// </summary>
        /// <param name="reset_password_infos">The model of ResetPassword contains informations needeed to reset password</param>
        /// <returns>Return if the password has been change or not + message description</returns>
        [HttpPost]
        [AllowAnonymous]
        [Route("resetPassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPassword reset_password_infos)
        {
            var response = await _svc.ResetPassword(reset_password_infos);
            return Ok(response);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("checkPassword")]
        public async Task<ActionResult> CheckPassword([FromBody] CheckPassword Checkpassword)
        {
            var response = await _svc.CheckPassword(Checkpassword);
            return Ok(response);
        }

        [HttpGet]
        [Route("emailConfirmation/{token}/{mail}")]
        public async Task<ActionResult> EmailConfirmation(string token, string mail)
        {            
            var response = await _svc.EmailConfirmation(new EmailConfirmation { Email = mail, Token = token });
            return Ok(response);
        }
    }
}

