using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

switch (args.FirstOrDefault())
{
    case "serve" when args is [_, "granular" or "focused"]:
        await Server.RunAsync(args[1]);
        break;
    case "check" when args is {Length: 1} or [_, "--output", _]:
        await Checks.RunAsync(args is {Length: 3} ? args[2] : null);
        break;
    case "evaluate":
        await Experiment.RunAsync(args[1..]);
        break;
    default:
        Console.Error.WriteLine("Usage: dotnet run -- serve granular|focused | check [--output PATH] | evaluate [options]");
        Environment.ExitCode = 1;
        break;
}

public static class Server
{
    public static async Task RunAsync(string variant)
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            DisableDefaults = true,
            ContentRootPath = AppContext.BaseDirectory
        });
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        var mcp = builder.Services.AddMcpServer().WithStdioServerTransport();
        if (variant == "granular")
            mcp.WithTools<GranularTools>();
        else
            mcp.WithTools<FocusedTools>();
        await builder.Build().RunAsync();
    }

    public static Task<McpClient> ConnectAsync(string variant, CancellationToken cancellationToken = default)
    {
        if (variant is not ("granular" or "focused"))
            throw new ArgumentException("Unknown server variant.", nameof(variant));

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = $"order-support-{variant}",
            Command = "dotnet",
            Arguments = [typeof(Server).Assembly.Location, "serve", variant]
        });
        return McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    public static string ResultJson(CallToolResult result)
        => result.Content.OfType<TextContentBlock>().Single().Text;
}
