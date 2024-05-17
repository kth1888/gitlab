namespace Savor22b.States.Trade;

using System;
using Bencodex.Types;
using System.Collections.Immutable;
using Libplanet.Assets;
using Libplanet;
using Libplanet.Headless.Extensions;

public static class TradeGoodFactory
{
    public static TradeGood CreateInstance(Dictionary serialized)
    {
        string type = serialized["Type"].ToDotnetString();

        switch (type)
        {
            case nameof(FoodGoodState):
                return CreateFoodGood(serialized);
            case nameof(ItemsGoodState):
                return CreateItemGood(serialized);
            case nameof(DungeonConquestGoodState):
                return CreateDungeonConquestGood(serialized);
            default:
                throw new ArgumentException($"Unsupported TradeGood type: {type}");
        }
    }

    private static TradeGood CreateFoodGood(Dictionary serialized)
    {
        var food = new RefrigeratorState((Dictionary)serialized["Food"]);
        var (sellerAddress, productId, price) = DeserializeTradeGoodMetaData(serialized);

        return new FoodGoodState(sellerAddress, productId, price, food);
    }

    private static TradeGood CreateItemGood(Dictionary serialized)
    {
        var items = ((List)serialized["Items"]).Select(dict => new ItemState((Dictionary)dict)).ToImmutableList();
        var (sellerAddress, productId, price) = DeserializeTradeGoodMetaData(serialized);

        return new ItemsGoodState(sellerAddress, productId, price, items);
    }

    private static TradeGood CreateDungeonConquestGood(Dictionary serialized)
    {
        var dungeonId = serialized["DungeonId"].ToInteger();
        var (sellerAddress, productId, price) = DeserializeTradeGoodMetaData(serialized);

        return new DungeonConquestGoodState(sellerAddress, productId, price, dungeonId);
    }

    private static (Address, Guid, FungibleAssetValue) DeserializeTradeGoodMetaData(Dictionary serialized)
    {
        Address sellerAddress = serialized[nameof(TradeGood.SellerAddress)].ToAddress();
        Guid productId = serialized[nameof(TradeGood.ProductStateId)].ToGuid();
        FungibleAssetValue price = serialized[nameof(TradeGood.Price)].ToFungibleAssetValue();

        return (sellerAddress, productId, price);
    }
}
