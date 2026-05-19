using ModelContextProtocol.Server;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hercules.AI
{
    public class McpServer
    {
        private readonly AiTools tools;

        public McpServer(AiTools aiTools)
        {
            tools = aiTools;
        }

        public async Task RunMcpAsync(CancellationToken ct = default)
        {
            var serverOptions = new McpServerOptions
            {
                ServerInfo = new ModelContextProtocol.Protocol.Implementation
                {
                    Name = "Hercules",
                    Version = Core.GetVersion().ToString(),
                    Title = "Hercules design data database"
                },
                ServerInstructions = "Hercules is the database of JSON design documents. Each document describes a single ingame entity. Important properties: _id property is unique string id; category property is the document type. Which other properties are available for each category is defined by a special schema document."
            };

            var mcpTools =
                from methodInfo in tools.GetType().GetMethods()
                let attr = methodInfo.GetCustomAttribute<AiToolAttribute>()
                where attr != null
                select McpServerTool.Create(ReflectionHelper.CreateDelegate(methodInfo, tools), new McpServerToolCreateOptions
                {
                    Name = methodInfo.Name,
                    Description = attr.GetDescription(tools),
                    ReadOnly = attr.ReadOnly,
                    Destructive = attr.Destructive,
                    OpenWorld = attr.OpenWorld,
                });

            foreach (var tool in mcpTools)
                serverOptions.ToolCollection.Add(tool);

            var loggerFactory = new HerculesLoggerFactory();
            await using var stdioTransport = new StdioServerTransport(serverOptions, loggerFactory);
            var server = ModelContextProtocol.Server.McpServer.Create(stdioTransport, serverOptions, loggerFactory, null);
            await server.RunAsync(ct);
        }
    }
}
