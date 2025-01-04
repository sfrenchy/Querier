using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs.Responses.DBConnection;

namespace Querier.Api.Domain.Services
{
    public class DatabaseServerDiscovery
    {
        private readonly ILogger<DatabaseServerDiscovery> _logger;

        public DatabaseServerDiscovery(ILogger<DatabaseServerDiscovery> logger)
        {
            _logger = logger;
        }

        public async Task<List<DatabaseServerInfo>> EnumerateServersAsync(string databaseType)
        {
            try
            {
                return databaseType switch
                {
                    "SQLServer" => await EnumerateServersWithPort(1433),
                    "MySQL" => await EnumerateServersWithPort(3306),
                    "PostgreSQL" => await EnumerateServersWithPort(5432),
                    _ => throw new NotSupportedException($"Database type {databaseType} not supported")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating {DatabaseType} servers", databaseType);
                throw;
            }
        }

        private async Task<List<DatabaseServerInfo>> EnumerateServersWithPort(int port)
        {
            var servers = new List<DatabaseServerInfo>();

            try
            {
                var activeHosts = await GetActiveHostsInNetwork();

                foreach (var ip in activeHosts)
                {
                    try
                    {
                        using var client = new TcpClient();
                        var connectTask = client.ConnectAsync(ip, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(200)) == connectTask)
                        {
                            servers.Add(new DatabaseServerInfo
                            {
                                ServerName = ip.ToString(),
                                NetworkProtocol = "TCP",
                                Port = port
                            });
                        }
                    }
                    catch
                    {
                        // Ignore connection errors
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for servers on port {Port}", port);
            }

            return servers;
        }

        private async Task<List<IPAddress>> GetActiveHostsInNetwork()
        {
            var activeHosts = new List<IPAddress>();
            try
            {
                // Utiliser ARP pour trouver les hôtes actifs
                using var process = new System.Diagnostics.Process();
                process.StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "arp",
                    Arguments = "-a",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };
                process.Start();
                string output = await process.StandardOutput.ReadToEndAsync();
                await process.WaitForExitAsync();

                // Parser la sortie de ARP pour extraire les adresses IP
                var ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                var matches = System.Text.RegularExpressions.Regex.Matches(output, ipPattern);
                foreach (System.Text.RegularExpressions.Match match in matches)
                {
                    if (IPAddress.TryParse(match.Value, out IPAddress ip))
                    {
                        activeHosts.Add(ip);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active hosts from ARP cache");
            }

            // Ajouter aussi les IPs locales
            var localIps = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.Address);
            activeHosts.AddRange(localIps);

            return activeHosts.Distinct().ToList();
        }
    }
} 