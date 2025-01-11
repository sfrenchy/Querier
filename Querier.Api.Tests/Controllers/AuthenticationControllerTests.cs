using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Querier.Api.Application.DTOs;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Controllers;
using Querier.Api.Domain.Entities.Auth;
using Xunit;

namespace Querier.Api.Tests.Controllers
{
    public class AuthenticationControllerTests
    {
        private readonly Mock<IAuthenticationService> _authServiceMock;
        private readonly Mock<ILogger<AuthenticationController>> _loggerMock;
        private readonly AuthenticationController _controller;

        public AuthenticationControllerTests()
        {
            _authServiceMock = new Mock<IAuthenticationService>();
            _loggerMock = new Mock<ILogger<AuthenticationController>>();
            _controller = new AuthenticationController(_authServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task SignUp_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var signUpDto = new SignUpDto { Email = "test@test.com", Password = "Test123!" };
            var expectedResult = new SignUpResultDto { Success = true };
            
            _authServiceMock.Setup(x => x.SignUp(signUpDto))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SignUp(signUpDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var signUpResult = okResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            signUpResult.Success.Should().BeTrue();
            _authServiceMock.Verify(x => x.SignUp(signUpDto), Times.Once);
        }

        [Fact]
        public async Task SignUp_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var signUpDto = new SignUpDto();
            _controller.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _controller.SignUp(signUpDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var signUpResult = badRequestResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            signUpResult.Success.Should().BeFalse();
            signUpResult.Errors.Should().Contain("Invalid payload");
        }

        [Fact]
        public async Task SignIn_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var signInDto = new SignInDto { Email = "test@test.com", Password = "Test123!" };
            var expectedResult = new SignUpResultDto { Success = true };
            
            _authServiceMock.Setup(x => x.SignIn(signInDto))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.SignIn(signInDto);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var signInResult = okResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            signInResult.Success.Should().BeTrue();
            _authServiceMock.Verify(x => x.SignIn(signInDto), Times.Once);
        }

        [Fact]
        public async Task SignIn_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var signInDto = new SignInDto();
            _controller.ModelState.AddModelError("Email", "Required");

            // Act
            var result = await _controller.SignIn(signInDto);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var signInResult = badRequestResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            signInResult.Success.Should().BeFalse();
            signInResult.Errors.Should().Contain("Invalid payload");
        }

        [Fact]
        public async Task RefreshToken_WithValidModel_ReturnsOkResult()
        {
            // Arrange
            var tokenRequest = new TokenRequest { Token = "valid-token", RefreshToken = "valid-refresh-token" };
            var expectedResult = new SignUpResultDto { Success = true };
            
            _authServiceMock.Setup(x => x.RefreshToken(tokenRequest))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.RefreshToken(tokenRequest);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var refreshResult = okResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            refreshResult.Success.Should().BeTrue();
            _authServiceMock.Verify(x => x.RefreshToken(tokenRequest), Times.Once);
        }

        [Fact]
        public async Task RefreshToken_WithInvalidModel_ReturnsBadRequest()
        {
            // Arrange
            var tokenRequest = new TokenRequest();
            _controller.ModelState.AddModelError("Token", "Required");

            // Act
            var result = await _controller.RefreshToken(tokenRequest);

            // Assert
            var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            var refreshResult = badRequestResult.Value.Should().BeOfType<SignUpResultDto>().Subject;
            refreshResult.Success.Should().BeFalse();
            refreshResult.Errors.Should().Contain("Invalid payload");
        }
    }
} 