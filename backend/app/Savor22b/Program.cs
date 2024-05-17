using System.Net;
using Cocona;
using GraphQL;
using GraphQL.MicrosoftDI;
using GraphQL.Server;
using GraphQL.Server.Transports.AspNetCore;
using GraphQL.Server.Transports.WebSockets;
using GraphQL.SystemTextJson;
using Libplanet.Blockchain.Renderers;
using Libplanet.Crypto;
using Libplanet.Explorer.Interfaces;
using Libplanet.Explorer.Queries;
using Libplanet.Extensions.Cocona;
using Libplanet.Headless;
using Libplanet.Headless.Hosting;
using Libplanet.KeyStore;
using Savor22b;
using Savor22b.Action;
using Savor22b.GraphTypes;
using Savor22b.GraphTypes.Mutation;
using Savor22b.GraphTypes.Query;
using Savor22b.GraphTypes.Subscription;
using Savor22b.Helpers;
using Serilog;
using PlanetExplorerSchema = Libplanet.Explorer.Schemas.LibplanetExplorerSchema;

var app = CoconaApp.Create();

app.AddCommand(
    "execute",
    (
        [Argument(
            Description = "The private key of the validator.  If not given, the node will not "
                + "validate blocks.  You can create a new private key with the `dotnet planet key create` "
                + "command.  You can list the created keys in your machine with `dotnet planet key` "
                + "command."
        )]
            Guid? validatorKeyId,
        PassphraseParameters passphrase
    ) =>
    {
        // Get configuration
        string configPath =
            Environment.GetEnvironmentVariable("SAVOR22B_CONFIG_FILE") ?? "appsettings.json";

        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile(configPath)
            .AddEnvironmentVariables("SAVOR22B_");
        IConfiguration config = configurationBuilder.Build();

        var loggerConf = new LoggerConfiguration().ReadFrom.Configuration(config);
        Log.Logger = loggerConf.CreateLogger();

        var svrConfig = new Savor22bConfiguration();
        config.Bind(svrConfig);
        CsvDataHelper.Initialize(svrConfig.CsvDataResourcePath);

        PrivateKey? validatorKey = null;
        if (validatorKeyId is { } keyId)
        {
            IKeyStore keyStore = Web3KeyStore.DefaultKeyStore;
            ProtectedPrivateKey protectedKey = keyStore.Get(keyId);
            while (validatorKey is null)
            {
                try
                {
                    validatorKey = protectedKey.Unprotect(
                        passphrase.Take($"Passphrase ({keyId}): ")
                    );
                }
                catch (IncorrectPassphraseException)
                {
                    Console.Error.WriteLine("Incorrect passphrase.  Try again.");
                }
            }

            if (svrConfig.Network is null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.BackgroundColor = ConsoleColor.DarkBlue;
                Console.WriteLine(new string('-', Console.WindowWidth));
                Console.WriteLine(
                    "NOTE: The node is running in the solo validaton mode. The swarm "
                        + "is not running, and the node will not be able to sync blocks to remote nodes. "
                        + $"If this is not what you want, configure the \"Network\" section of {configPath}."
                );
                Console.Write(new string('-', Console.WindowWidth));
                Console.ResetColor();
                Console.WriteLine();
                Thread.Sleep(3000);
            }
        }
        else
        {
            // FIXME: These should be printed to stderr.
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.BackgroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(new string('-', Console.WindowWidth));
            Console.WriteLine(
                "WARNING: The validator key is not given.  The node will not validate "
                    + "blocks.  If it is intended, ignore this message.  If you want to validate blocks, "
                    + "you first need to create a new private key with the following command:"
            );
            Console.WriteLine();
            Console.WriteLine("\tdotnet planet key create");
            Console.WriteLine();
            Console.WriteLine("... and then pass the key ID to this command with its argument.");
            Console.Write(new string('-', Console.WindowWidth));
            if (svrConfig.Network is null)
            {
                Console.WriteLine(
                    "As the network is not configured, this node will not be able to "
                        + "see any new blocks. If you intended to sync blocks from remote nodes, please "
                        + "configure the network."
                );
                Console.Write(new string('-', Console.WindowWidth));
            }
            Console.ResetColor();
            Console.WriteLine();
            Thread.Sleep(3000);
        }

        var builder = WebApplication.CreateBuilder(args);
        var renderer = new BlockRenderer();

        builder.Services
            .AddLibplanet(builder =>
            {
                builder
                    .UseConfiguration(svrConfig)
                    .UseActionLoader(new SVRActionLoader())
                    .UseRenderers(new List<IRenderer> { renderer })
                    .UseBlockPolicy(BlockPolicySource.GetPolicy());

                if (validatorKey is { } key)
                {
                    builder.UseValidator(key);
                }
            })
            .AddGraphQL(builder =>
            {
                builder
                    .AddSchema<Schema>()
                    .AddSchema<PlanetExplorerSchema>()
                    .AddWebSockets()
                    .AddGraphTypes(typeof(ExplorerQuery).Assembly)
                    .AddGraphTypes(typeof(Query).Assembly)
                    .AddGraphTypes(typeof(Subscription).Assembly)
                    .AddUserContextBuilder<ExplorerContextBuilder>()
                    .AddSystemTextJson();
            })
            .AddCors()
            .AddSingleton<Schema>()
            .AddSingleton<Query>()
            .AddSingleton<Mutation>()
            .AddSingleton<Subscription>()
            .AddSingleton<BlockRenderer>(_ => renderer)
            .AddSingleton<GraphQLHttpMiddleware<Schema>>()
            .AddSingleton<GraphQLHttpMiddleware<PlanetExplorerSchema>>()
            .AddSingleton<IBlockChainContext, ExplorerContext>()
            .AddSingleton<GraphQLWebSocketsMiddleware<Schema>>();

        if (svrConfig.GraphQLUri is { } graphqlUri)
        {
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Parse(graphqlUri.DnsSafeHost), graphqlUri.Port);
            });
        }

        using WebApplication app = builder.Build();
        app.UseCors(builder =>
        {
            builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        });
        app.UseRouting();

        app.UseWebSockets();
        app.UseGraphQL<Schema>("/graphql");
        app.UseGraphQLWebSockets<Schema>("/graphql");

        if (svrConfig.GraphQLUri is { LocalPath: { } localPath })
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGraphQLPlayground();
            });
            app.UseGraphQL<Schema>(localPath);
            app.UseGraphQL<PlanetExplorerSchema>($"{localPath.TrimEnd('/')}/explorer");
        }

        app.Run();
    }
);

app.AddCommand(
    "genesis",
    (String path, Guid? validatorKeyId, PassphraseParameters passphrase) =>
    {
        PrivateKey? validatorKey = null;
        if (validatorKeyId is { } keyId)
        {
            IKeyStore keyStore = Web3KeyStore.DefaultKeyStore;
            ProtectedPrivateKey protectedKey = keyStore.Get(keyId);
            while (validatorKey is null)
            {
                try
                {
                    validatorKey = protectedKey.Unprotect(
                        passphrase.Take($"Passphrase ({keyId}): ")
                    );
                }
                catch (IncorrectPassphraseException)
                {
                    Console.Error.WriteLine("Incorrect passphrase.  Try again.");
                }
            }
        }
        if (validatorKey is not null)
        {
            var gesisBlock = GenesisBlockHelper.ProposeGenesisBlock(
                new PrivateKey[] { validatorKey },
                validatorKey
            );
            var gesisBlockBytes = GenesisBlockHelper.ExportBlock(gesisBlock);
            File.WriteAllBytes(path, gesisBlockBytes);
        }
    }
);

app.Run();
