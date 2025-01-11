using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Controllers;
using Querier.Api.Domain.Entities.Auth;
using Xunit;

namespace Querier.Api.Tests.Controllers
{
    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<ILogger<UserController>> _loggerMock;
        private readonly Mock<UserManager<ApiUser>> _userManagerMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _loggerMock = new Mock<ILogger<UserController>>();

            var store = new Mock<IUserStore<ApiUser>>();
            var optionsAccessor = new Mock<IOptions<IdentityOptions>>();
            optionsAccessor.Setup(x => x.Value).Returns(new IdentityOptions());
            var passwordHasher = new Mock<IPasswordHasher<ApiUser>>();
            var userValidators = new List<IUserValidator<ApiUser>>();
            var passwordValidators = new List<IPasswordValidator<ApiUser>>();
            var keyNormalizer = new Mock<ILookupNormalizer>();
            var errors = new Mock<IdentityErrorDescriber>();
            var services = new Mock<IServiceProvider>();
            var logger = new Mock<ILogger<UserManager<ApiUser>>>();

            _userManagerMock = new Mock<UserManager<ApiUser>>(
                store.Object,
                optionsAccessor.Object,
                passwordHasher.Object,
                userValidators,
                passwordValidators,
                keyNormalizer.Object,
                errors.Object,
                services.Object,
                logger.Object
            );

            _controller = new UserController(_userServiceMock.Object, _loggerMock.Object, _userManagerMock.Object);
        }

        [Fact]
        public async Task ViewAsync_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = "testUserId";
            var userDto = new UserDto
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _userServiceMock.Setup(x => x.View(userId))
                .Returns(Task.FromResult(userDto));

            // Act
            var result = await _controller.ViewAsync(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnedUser.Should().BeEquivalentTo(userDto);
        }

        [Theory]
        [InlineData("")]
        public async Task ViewAsync_WithInvalidId_ReturnsBadRequest(string userId)
        {
            // Act
            var result = await _controller.ViewAsync(userId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AddAsync_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var userCreateDto = new UserCreateDto
            {
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                UserName = "testuser",
                Roles = new List<string> { "User" }
            };

            _userServiceMock.Setup(x => x.Add(It.IsAny<UserCreateDto>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.AddAsync(userCreateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(true);
        }

        [Fact]
        public async Task AddAsync_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var userCreateDto = new UserCreateDto();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.AddAsync(userCreateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateAsync_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var userUpdateDto = new UserUpdateDto
            {
                Id = "testUserId",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _userServiceMock.Setup(x => x.Update(It.IsAny<UserUpdateDto>()))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.UpdateAsync(userUpdateDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(true);
        }

        [Fact]
        public async Task UpdateAsync_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var userUpdateDto = new UserUpdateDto();
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.UpdateAsync(userUpdateDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsOkResult()
        {
            // Arrange
            var userId = "testUserId";

            _userServiceMock.Setup(x => x.Delete(userId))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.DeleteAsync(userId);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(true);
        }

        [Theory]
        [InlineData("")]
        public async Task DeleteAsync_WithInvalidId_ReturnsBadRequest(string userId)
        {
            // Act
            var result = await _controller.DeleteAsync(userId);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAll_ReturnsOkResultWithUsers()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new UserDto
                {
                    Id = "1",
                    Email = "test1@example.com",
                    FirstName = "Test1",
                    LastName = "User1"
                },
                new UserDto
                {
                    Id = "2",
                    Email = "test2@example.com",
                    FirstName = "Test2",
                    LastName = "User2"
                }
            };

            _userServiceMock.Setup(x => x.GetAll())
                .Returns(Task.FromResult(users));

            // Act
            var result = await _controller.GetAll();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUsers = okResult.Value.Should().BeOfType<UserDto[]>().Subject;
            returnedUsers.Should().BeEquivalentTo(users);
        }

        [Fact]
        public async Task ResetPassword_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var resetPasswordDto = new ResetPasswordDto
            {
                Email = "test@example.com",
                Password = "NewPassword123!",
                ConfirmPassword = "NewPassword123!",
                Token = "validToken"
            };

            var expectedResponse = new { success = true, message = "Password has been changed" };

            _userServiceMock.Setup(x => x.ResetPassword(It.IsAny<ResetPasswordDto>()))
                .Returns(Task.FromResult<object>(expectedResponse));

            // Act
            var result = await _controller.ResetPassword(resetPasswordDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().BeEquivalentTo(expectedResponse);
        }

        [Fact]
        public async Task EmailConfirmation_WithValidTokenAndEmail_ReturnsOkResult()
        {
            // Arrange
            var token = "validToken";
            var email = "test@example.com";

            _userServiceMock.Setup(x => x.EmailConfirmation(It.Is<EmailConfirmationDto>(dto =>
                dto.Token == token && dto.Email == email)))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.EmailConfirmation(token, email);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(true);
        }

        [Fact]
        public async Task Me_WhenUserExists_ReturnsOkResult()
        {
            // Arrange
            var userDto = new UserDto
            {
                Id = "testUserId",
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "testUserId")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userServiceMock.Setup(x => x.GetCurrentUser(It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.FromResult(userDto));

            // Act
            var result = await _controller.Me();

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
            returnedUser.Should().BeEquivalentTo(userDto);
        }

        [Fact]
        public async Task Me_WhenUserDoesNotExist_ReturnsNotFound()
        {
            // Arrange
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, "nonExistentUserId")
            };
            var identity = new ClaimsIdentity(claims);
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };

            _userServiceMock.Setup(x => x.GetCurrentUser(It.IsAny<ClaimsPrincipal>()))
                .Returns(Task.FromResult<UserDto>(null));

            // Act
            var result = await _controller.Me();

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task ResendConfirmationEmail_WhenSuccessful_ReturnsOkResult()
        {
            // Arrange
            var email = "test@example.com";

            _userServiceMock.Setup(x => x.ResendConfirmationEmail(email))
                .Returns(Task.FromResult(true));

            // Act
            var result = await _controller.ResendConfirmationEmail(email);

            // Assert
            result.Should().BeOfType<OkResult>();
        }

        [Fact]
        public async Task ResendConfirmationEmail_WhenFailed_ReturnsBadRequest()
        {
            // Arrange
            var email = "test@example.com";

            _userServiceMock.Setup(x => x.ResendConfirmationEmail(email))
                .Returns(Task.FromResult(false));

            // Act
            var result = await _controller.ResendConfirmationEmail(email);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
} 