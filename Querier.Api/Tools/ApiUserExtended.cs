using System;
using Querier.Api.Models.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Models;
using Querier.Api.Models.Common;

namespace Querier.Tools
{
    public abstract class ApiUserExtended<T,U> where T : ApiUserExtended<T,U>, new() where U: ApiDbContext
    {
        private ApiUser _apiUser;
        private IServiceScope _serviceScope;
        private UserManager<ApiUser> _userManager;
        private Type _userType = typeof(T);
        protected U _context;

        protected ApiUserExtended()
        {
        }

        public static T FromEmail(string email)
        {
            T result = new T();
            result._serviceScope = ServiceActivator.GetScope();
            result._userManager = result._serviceScope.ServiceProvider.GetService<UserManager<ApiUser>>();
            result._apiUser = result._userManager.FindByEmailAsync(email).GetAwaiter().GetResult();
            result._context = result._serviceScope.ServiceProvider.GetService<U>();
            return result;
        }

        public static T FromId(string id)
        {
            T result = new T();
            result._serviceScope = ServiceActivator.GetScope();
            result._userManager = result._serviceScope.ServiceProvider.GetService<UserManager<ApiUser>>();
            result._apiUser = result._userManager.FindByIdAsync(id).GetAwaiter().GetResult();
            result._context = result._serviceScope.ServiceProvider.GetService<U>();
            return result;
        }

        public static T FromName(string name)
        {
            T result = new T();
            result._serviceScope = ServiceActivator.GetScope();
            result._userManager = result._serviceScope.ServiceProvider.GetService<UserManager<ApiUser>>();
            result._apiUser = result._userManager.FindByNameAsync(name).GetAwaiter().GetResult();
            result._context = result._serviceScope.ServiceProvider.GetService<U>();
            return result;
        }
        
        public ApiUser BaseUser
        {
            get
            {
                return _apiUser;
            }
        }

        protected dynamic GetAttribute([CallerMemberName] string callerName = null)
        {
            if (callerName == null)
                throw new NullReferenceException();
            return _apiUser.QApiUserAttributes.First(a => a.EntityAttribute.Label == callerName).EntityAttribute.Value;
        }
        private void SetAttribute(object? attributeValue, bool nullable, string callerName = null)
        {
            if (callerName == null)
                throw new NullReferenceException();

            if (!_apiUser.QApiUserAttributes.Any(a => a.EntityAttribute.Label == callerName))
            {
                var newAttribute = new QApiUserAttributes()
                {
                    User = _apiUser,
                    EntityAttribute = new QEntityAttribute()
                    {
                        Label = callerName,
                        Nullable = nullable
                    }
                };
                newAttribute.EntityAttribute.Value = attributeValue;
                _apiUser.QApiUserAttributes.Add(newAttribute);
            }
            else
            {
                _apiUser.QApiUserAttributes.First(a => a.EntityAttribute.Label == callerName).EntityAttribute.Value = attributeValue;
            }
            _userManager.UpdateAsync(_apiUser).GetAwaiter().GetResult();
        }
        protected void SetAttribute<T>(object? attributeValue, [CallerMemberName] string callerName = null)
        {
            SetAttribute(attributeValue, Nullable.GetUnderlyingType(typeof(T)) != null, callerName);
        }
        protected void SetAttribute(object? attributeValue, [CallerMemberName] string callerName = null)
        {
            SetAttribute(attributeValue, false, callerName);
        }
    }
}

