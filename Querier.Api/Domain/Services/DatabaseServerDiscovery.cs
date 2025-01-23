using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Querier.Api.Application.DTOs;

namespace Querier.Api.Domain.Services
{
    public class DatabaseServerDiscovery
    {
        private readonly ILogger<DatabaseServerDiscovery> _logger;
        private const int CONNECTION_TIMEOUT_MS = 200;

        public DatabaseServerDiscovery(ILogger<DatabaseServerDiscovery> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<DBConnectionDatabaseServerInfoDto>> EnumerateServersAsync(string databaseType)
        {
            try
            {
                _logger.LogInformation("Starting server enumeration for database type: {Type}", databaseType);

                var servers = databaseType switch
                {
                    "SQLServer" => await EnumerateServersWithPort(1433, databaseType),
                    "MySQL" => await EnumerateServersWithPort(3306, databaseType),
                    "PostgreSQL" => await EnumerateServersWithPort(5432, databaseType),
                    _ => throw new NotSupportedException($"Database type {databaseType} not supported")
                };

                _logger.LogInformation("Found {Count} {Type} servers", servers.Count, databaseType);
                return servers;
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enumerating {Type} servers", databaseType);
                throw;
            }
        }

        private async Task<List<DBConnectionDatabaseServerInfoDto>> EnumerateServersWithPort(int port, string databaseType)
        {
            var servers = new List<DBConnectionDatabaseServerInfoDto>();

            try
            {
                _logger.LogDebug("Starting network scan for {Type} servers on port {Port}", databaseType, port);
                var activeHosts = await GetActiveHostsInNetwork();
                _logger.LogDebug("Found {Count} active hosts to scan", activeHosts.Count);

                foreach (var ip in activeHosts)
                {
                    try
                    {
                        using var client = new TcpClient();
                        _logger.LogTrace("Testing connection to {IP}:{Port}", ip, port);
                        
                        var connectTask = client.ConnectAsync(ip, port);
                        if (await Task.WhenAny(connectTask, Task.Delay(CONNECTION_TIMEOUT_MS)) == connectTask)
                        {
                            _logger.LogDebug("Found {Type} server at {IP}:{Port}", databaseType, ip, port);
                            servers.Add(new DBConnectionDatabaseServerInfoDto
                            {
                                ServerName = ip.ToString(),
                                NetworkProtocol = "TCP",
                                Port = port
                            });
                        }
                    }
                    catch (SocketException ex)
                    {
                        _logger.LogTrace(ex, "Connection failed to {IP}:{Port}", ip, port);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogDebug(ex, "Unexpected error testing connection to {IP}:{Port}", ip, port);
                    }
                }

                return servers;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scanning for {Type} servers on port {Port}", databaseType, port);
                throw;
            }
        }

        private async Task<List<IPAddress>> GetActiveHostsInNetwork()
        {
            var activeHosts = new HashSet<IPAddress>();
            
            try
            {
                _logger.LogDebug("Retrieving active hosts from ARP cache");
                await GetHostsFromArpCache(activeHosts);
                
                _logger.LogDebug("Retrieving local network interfaces");
                GetLocalNetworkInterfaces(activeHosts);

                _logger.LogDebug("Found {Count} unique active hosts", activeHosts.Count);
                return activeHosts.ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting active hosts");
                throw;
            }
        }

        private async Task GetHostsFromArpCache(HashSet<IPAddress> activeHosts)
        {
            try
            {
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

                var ipPattern = @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b";
                var matches = Regex.Matches(output, ipPattern);
                
                foreach (Match match in matches)
                {
                    if (IPAddress.TryParse(match.Value, out IPAddress ip))
                    {
                        activeHosts.Add(ip);
                    }
                }

                _logger.LogDebug("Found {Count} hosts in ARP cache", matches.Count);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting hosts from ARP cache");
                // Continue execution as this is not critical
            }
        }

        private void GetLocalNetworkInterfaces(HashSet<IPAddress> activeHosts)
        {
            try
            {
                var localIps = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address);

                foreach (var ip in localIps)
                {
                    activeHosts.Add(ip);
                }

                _logger.LogDebug("Found {Count} local network interfaces", localIps.Count());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting local network interfaces");
                // Continue execution as this is not critical
            }
        }
    }
} 