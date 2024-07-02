using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Querier.Api.Models.Auth;
using Querier.Api.Models.Common;
using Querier.Api.Models.Requests.User;

namespace Querier.Api.Models.Interfaces
{
    public interface IQPlugin
    {
        void ConfigureApp(IApplicationBuilder app, IWebHostEnvironment env);

        /// <summary>
        /// Method to configure application specific services
        /// </summary>
        /// <param name="services">Services interface</param>
        /// <param name="configuration">Configuration interface</param>
        void ConfigureServices(IServiceCollection services, IConfiguration configuration);
        /// <summary>
        /// Get the list of required Content Directories for the app
        /// </summary>
        /// <returns>List of directories to be created in wwwroot</returns>
        List<string> GetRequiredContentDirectories();
        /// <summary>
        /// Get A specific properties for the application
        /// </summary>
        /// <returns>A specific properties for the application</returns>
        ApplicationSpecificProperties GetSpecificProperties();
        /// <summary>
        /// This method will be called whenever a new ApiUser is created and will allow any application that implement it to perform a specific task
        /// </summary>
        /// <param name="user"></param>
        void QuerierUserCreated(ApiUser user);
        /// <summary>
        /// This method will be called whenever a new ApiUser is updated and will allow any application that implement it to perform a specific task
        /// </summary>
        /// <param name="userRequest"></param>
        void QuerierUserUpdated(UserRequest userRequest);
        /// <summary>
        /// This method will be called whenever a new ApiUser is deleted and will allow any application that implement it to perform a specific task
        /// </summary>
        /// <param name="userId"></param>
        void QuerierUserDeleted(string userId);
        /// <summary>
        /// This method will be called to create template mail when the solution starts
        /// </summary>
        /// <param name="configuration">Configuration interface</param>
        /// <param name="environment">Environment interface</param>
        Task CreateTemplateEmail();
    }
}
