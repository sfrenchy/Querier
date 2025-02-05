using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Querier.Api.Domain.Common.Enums;
using Querier.Api.Domain.Entities.QDBConnection.Endpoints;
using System.Data.Common;

namespace Querier.Api.Domain.Entities.DBConnection
{
    public class DBConnection
    {
        public DBConnection()
        {
            Endpoints = new HashSet<EndpointDescription>();
            Parameters = new HashSet<ConnectionStringParameter>();
        }

        [Key]
        public int Id { get; set; }

        public DbConnectionType ConnectionType { get; set; }
        public string Name { get; set; }

        [NotMapped]
        public string ConnectionString 
        { 
            get => BuildConnectionString();
            set => ParseConnectionString(value);
        }
        
        [Required]
        public string ContextName { get; set; }
        public string ApiRoute { get; set; }
        public string Description { get; set; }
        public string AssemblyHash { get; set; }
        
        // Contenu des fichiers d'assembly
        public byte[] AssemblyDll { get; set; }
        public byte[] AssemblyPdb { get; set; }
        public byte[] AssemblySourceZip { get; set; }

        // Description des endpoints générés
        [InverseProperty("DBConnection")]
        public virtual ICollection<EndpointDescription> Endpoints { get; set; }

        [InverseProperty("DBConnection")]
        public virtual ICollection<ConnectionStringParameter> Parameters { get; set; }

        public string BuildConnectionString()
        {
            var builder = new DbConnectionStringBuilder();
            foreach (var param in Parameters)
            {
                builder[param.Key] = param.StoredValue;
            }
            return builder.ConnectionString;
        }

        private void ParseConnectionString(string connectionString)
        {
            var pairs = connectionString.Split(';')
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Split('='))
                .Where(pair => pair.Length == 2);

            Parameters.Clear();
            foreach (var pair in pairs)
            {
                Parameters.Add(new ConnectionStringParameter
                {
                    Key = pair[0].Trim(),
                    StoredValue = pair[1].Trim(),
                    // Définir IsEncrypted selon la clé
                    IsEncrypted = ShouldEncrypt(pair[0].Trim())
                });
            }
        }

        private bool ShouldEncrypt(string key)
        {
            // Liste des paramètres sensibles selon le type de connexion
            var sensitiveParams = ConnectionType switch
            {
                DbConnectionType.SqlServer => new[] { "Password", "User Id", "Uid", "Pwd" },
                // Ajouter d'autres cas pour différents types de SGBD
                _ => new[] { "Password", "User", "Username" }
            };

            return sensitiveParams.Contains(key);
        }
    }
}