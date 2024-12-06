using System.Threading.Tasks;
using Querier.Api.Models.Common;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using System;

namespace Querier.Api.Services
{
    public interface ISettingService
    {
        /// <summary>
        /// Get all settings
        /// </summary>
        /// <returns>The settings</returns>
        Task<QSetting> GetSettings();
        /// <summary>
        /// Update a setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        Task<QSetting> UpdateSetting(QSetting setting);
        /// <summary>
        /// Configure an application setting
        /// </summary>
        /// <param name="setting"></param>
        /// <returns>The updated setting</returns>
        Task<QSetting> Configure(QSetting setting);
        /// <summary>
        /// Get if the application is configured
        /// </summary>
        /// <returns>True if the application is configured, false otherwise</returns>
        Task<bool> GetIsConfigured();
        /// <summary>
        /// Create a new setting
        /// </summary>
        /// <param name="name">The name/key of the setting</param>
        /// <param name="value">The value of the setting</param>
        /// <returns>The created setting</returns>
        Task<QSetting> CreateSetting(string name, string value);
        /// <summary>
        /// Get the value of a setting. If defaultValue is provided and the setting doesn't exist, creates it.
        /// </summary>
        /// <param name="name">The name/key of the setting</param>
        /// <param name="defaultValue">Optional default value. If provided and setting doesn't exist, creates it</param>
        /// <returns>The value of the setting</returns>
        Task<string?> GetSettingValue(string name, string? defaultValue = null);
    }

    
} 