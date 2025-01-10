using System.Threading.Tasks;

namespace Querier.Api.Application.Interfaces.Services;

/// <summary>
/// This interface describes all the methods for flushing the  redis cache in different ways.
/// </summary>
public interface ICacheManagementService
{
    /// <summary>
    ///  This method flush all cache.
    /// </summary>
    /// <returns>Return a task</returns>
    Task FlushAllAsync();

    /// <summary>
    ///  this method removes the caches that contain the substring
    /// </summary>
    /// <returns>Return a task</returns>
    /// <param name="substring">the substring that will be taken as an argument</param>
    Task FlushBySubstringAsync(string substring);

    /// <summary>
    ///  this method removes the caches with the key
    /// </summary>
    /// <returns>Return a task</returns>
    /// <param name="key">the key to a cache that will be taken as an argument</param>
    Task FlushByKeyAsync(string key);
}