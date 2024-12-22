using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Querier.Api.Infrastructure.DependencyInjection;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Querier.Api.Tools;
using JsonSerializer = System.Text.Json.JsonSerializer;
using System.IO;
using Querier.Api.Application.DTOs.Requests.Entity;
using Querier.Api.Application.Interfaces.Infrastructure;
using Querier.Api.Domain.Common.ValueObjects;
using Querier.Api.Infrastructure.DependencyInjection;
namespace Querier.Api.Tools
{
    public static class Utils
    {
        public static DbContext GetDbContextFromTypeName(string contextTypeName)
        {
            List<Type> contextTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(t => t.IsAssignableTo(typeof(DbContext)) && t.FullName == contextTypeName).ToList();


            DbContext target = ServiceActivator.GetScope().ServiceProvider.GetService(contextTypes.First()) as DbContext ??
                               Activator.CreateInstance(contextTypes.First()) as DbContext;
            return target;
        }
        public static string RandomString(int length)
        {
            var random = new Random();
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static Type GetType(string typeName)
        {
            var type = Type.GetType(typeName);
            if (type != null) return type;
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = a.GetType(typeName);
                if (type != null)
                    return type;
            }
            return null;
        }

        public static string? ComputeMd5Hash(byte[] objectAsBytes)
        {
            MD5 md5 = MD5.Create();
            try
            {
                byte[] result = md5.ComputeHash(objectAsBytes);

                // Build the final string by converting each byte
                // into hex and appending it to a StringBuilder
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < result.Length; i++)
                {
                    sb.Append(result[i].ToString("X2"));
                }

                // And return it
                return sb.ToString();
            }
            catch (ArgumentNullException ane)
            {
                //If something occurred during serialization, 
                //this method is called with a null argument. 
                Console.WriteLine("Hash has not been generated.");
                return null;
            }
        }

        public static byte[] ObjectToByteArray(Object objectToSerialize)
        {
            return ASCIIEncoding.ASCII.GetBytes(JsonSerializer.Serialize(objectToSerialize));
        }
    }
}
