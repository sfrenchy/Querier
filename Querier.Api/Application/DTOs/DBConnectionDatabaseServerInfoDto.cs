namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object containing database server connection information
    /// </summary>
    public class DBConnectionDatabaseServerInfoDto
    {
        /// <summary>
        /// Hostname or IP address of the database server
        /// </summary>
        public string ServerName { get; set; }

        /// <summary>
        /// Port number where the database server listens for connections
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Network protocol used for database connections (defaults to "TCP")
        /// </summary>
        public string NetworkProtocol { get; set; } = "TCP";
    }
} 