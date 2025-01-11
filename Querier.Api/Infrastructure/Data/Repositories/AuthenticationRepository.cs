using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.Interfaces.Repositories;
using Querier.Api.Domain.Entities.Auth;
using Querier.Api.Infrastructure.Data.Context;

namespace Querier.Api.Infrastructure.Data.Repositories
{
    public class AuthenticationRepository(
        IDbContextFactory<ApiDbContext> contextFactory,
        ILogger<AuthenticationRepository> logger)
        : IAuthenticationRepository
    {
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            try
            {
                if (refreshToken == null)
                {
                    logger.LogError("AddRefreshTokenAsync called with null token");
                    throw new ArgumentNullException(nameof(refreshToken));
                }

                logger.LogDebug("Adding refresh token for user: {UserId}", refreshToken.UserId);

                await using var context = await contextFactory.CreateDbContextAsync();

                var exists = await context.RefreshTokens.AnyAsync(t => t.Token == refreshToken.Token);
                if (exists)
                {
                    const string error = "Refresh token already exists";
                    logger.LogWarning(error);
                    throw new InvalidOperationException(error);
                }

                await context.RefreshTokens.AddAsync(refreshToken);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully added refresh token for user: {UserId}", refreshToken.UserId);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not InvalidOperationException)
            {
                logger.LogError(ex, "Failed to add refresh token for user: {UserId}", refreshToken?.UserId);
                throw;
            }
        }

        public async Task<RefreshToken> GetRefreshTokenAsync(string refreshToken)
        {
            try
            {
                if (string.IsNullOrEmpty(refreshToken))
                {
                    logger.LogError("GetRefreshTokenAsync called with null or empty token");
                    throw new ArgumentException("Refresh token cannot be null or empty", nameof(refreshToken));
                }

                logger.LogDebug("Retrieving refresh token: {Token}", refreshToken);

                await using var context = await contextFactory.CreateDbContextAsync();
                var token = await context.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken);

                if (token == null)
                {
                    logger.LogWarning("Refresh token not found: {Token}", refreshToken);
                    return null;
                }

                logger.LogDebug("Successfully retrieved refresh token for user: {UserId}", token.UserId);
                return token;
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                logger.LogError(ex, "Failed to retrieve refresh token: {Token}", refreshToken);
                throw;
            }
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            try
            {
                if (refreshToken == null)
                {
                    logger.LogError("UpdateRefreshTokenAsync called with null token");
                    throw new ArgumentNullException(nameof(refreshToken));
                }

                logger.LogDebug("Updating refresh token for user: {UserId}", refreshToken.UserId);

                await using var context = await contextFactory.CreateDbContextAsync();
                
                var exists = await context.RefreshTokens.AnyAsync(t => t.Token == refreshToken.Token);
                if (!exists)
                {
                    var error = $"Refresh token not found: {refreshToken.Token}";
                    logger.LogWarning(error);
                    throw new KeyNotFoundException(error);
                }

                context.RefreshTokens.Update(refreshToken);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully updated refresh token for user: {UserId}", refreshToken.UserId);
            }
            catch (Exception ex) when (ex is not ArgumentNullException && ex is not KeyNotFoundException)
            {
                logger.LogError(ex, "Failed to update refresh token for user: {UserId}", refreshToken?.UserId);
                throw;
            }
        }

        public async Task DeleteRefreshTokensForUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    logger.LogError("DeleteRefreshTokensForUserAsync called with null or empty user ID");
                    throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
                }

                logger.LogDebug("Deleting all refresh tokens for user: {UserId}", userId);

                await using var context = await contextFactory.CreateDbContextAsync();
                var tokens = await context.RefreshTokens
                    .Where(t => t.UserId == userId)
                    .ToListAsync();

                if (!tokens.Any())
                {
                    logger.LogDebug("No refresh tokens found for user: {UserId}", userId);
                    return;
                }

                context.RefreshTokens.RemoveRange(tokens);
                await context.SaveChangesAsync();

                logger.LogInformation("Successfully deleted {Count} refresh tokens for user: {UserId}", 
                    tokens.Count, userId);
            }
            catch (Exception ex) when (ex is not ArgumentException)
            {
                logger.LogError(ex, "Failed to delete refresh tokens for user: {UserId}", userId);
                throw;
            }
        }
    }
}