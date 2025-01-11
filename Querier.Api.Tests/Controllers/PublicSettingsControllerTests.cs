using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Querier.Api.Application.Interfaces.Services;
using Querier.Api.Controllers;
using Xunit;

namespace Querier.Api.Tests.Controllers
{
    public class PublicSettingsControllerTests
    {
        private readonly Mock<ISettingService> _settingServiceMock;
        private readonly PublicSettingsController _controller;

        public PublicSettingsControllerTests()
        {
            _settingServiceMock = new Mock<ISettingService>();
            _controller = new PublicSettingsController(_settingServiceMock.Object);
        }

        [Fact]
        public async Task GetApiIsConfigured_WhenCalled_ReturnsConfigurationStatus()
        {
            // Arrange
            var expectedResult = true;
            _settingServiceMock.Setup(x => x.GetApiIsConfiguredAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetApiIsConfigured();

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.Value.Should().Be(expectedResult);
            _settingServiceMock.Verify(x => x.GetApiIsConfiguredAsync(), Times.Once);
        }

        [Fact]
        public async Task GetApiIsConfigured_WhenNotConfigured_ReturnsFalse()
        {
            // Arrange
            var expectedResult = false;
            _settingServiceMock.Setup(x => x.GetApiIsConfiguredAsync())
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _controller.GetApiIsConfigured();

            // Assert
            var actionResult = result.Result as OkObjectResult;
            actionResult.Should().NotBeNull();
            actionResult!.Value.Should().Be(expectedResult);
            _settingServiceMock.Verify(x => x.GetApiIsConfiguredAsync(), Times.Once);
        }
    }
} 