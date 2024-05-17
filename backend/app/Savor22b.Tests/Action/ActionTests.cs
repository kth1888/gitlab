namespace Savor22b.Tests.Action;

using Microsoft.Extensions.Configuration;
using Libplanet;
using Libplanet.Crypto;
using System;
using Libplanet.State;
using Savor22b.States;
using Savor22b.States.Trade;

public class ActionTests
{
    private readonly PrivateKey _signer = new PrivateKey();
    private readonly string csvDataResourcePath;

    protected DummyRandom random = new DummyRandom(1);

    public ActionTests()
    {
        string configPath = Environment.GetEnvironmentVariable("SAVOR22B_CONFIG_FILE") ?? "appsettings.local.json";

        var configurationBuilder = new ConfigurationBuilder()
            .AddJsonFile(configPath)
            .AddEnvironmentVariables("SAVOR22B_");
        IConfiguration configuration = configurationBuilder.Build();

        csvDataResourcePath = configuration["CsvDataResourcePath"];
        CsvDataHelper.Initialize(csvDataResourcePath);
    }

    public PrivateKey Signer()
    {
        return _signer;
    }

    public Address SignerAddress()
    {
        return _signer.PublicKey.ToAddress();
    }

    public RootState DeriveRootStateFromAccountStateDelta(IAccountStateDelta stateDelta) {
        var rootStateEncoded = stateDelta.GetState(SignerAddress());
        RootState rootState = rootStateEncoded is Bencodex.Types.Dictionary bdict
            ? new RootState(bdict)
            : throw new Exception();
        return rootState;
    }

    public TradeInventoryState DeriveTradeInventoryStateDelta(IAccountStateDelta stateDelta) {
        var tradeInventoryStateEncoded = stateDelta.GetState(TradeInventoryState.StateAddress);
        TradeInventoryState tradeInventoryState = tradeInventoryStateEncoded is Bencodex.Types.Dictionary bdict
            ? new TradeInventoryState(bdict)
            : throw new Exception();
        return tradeInventoryState;
    }

    public GlobalDungeonState DeriveGlobalDungeonStateDelta(IAccountStateDelta stateDelta) {
        var globalDungeonStateEncoded = stateDelta.GetState(GlobalDungeonState.StateAddress);
        GlobalDungeonState globalDungeonState = globalDungeonStateEncoded is Bencodex.Types.Dictionary bdict
            ? new GlobalDungeonState(bdict)
            : throw new Exception();
        return globalDungeonState;
    }
}
