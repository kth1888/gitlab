using GraphQL.Types;
using Savor22b.Model;
using Savor22b.States;

namespace Savor22b.GraphTypes.Types;

public class SeedStateType : ObjectGraphType<SeedState>
{
    public SeedStateType()
    {
        Field<GuidGraphType>(
            name: "stateId",
            description: "The ID of the seed state.",
            resolve: context => context.Source.StateID
        );

        Field<IntGraphType>(
            name: "seedId",
            description: "The ID of the seed.",
            resolve: context => context.Source.SeedID
        );

        Field<StringGraphType>(
            name: "name",
            description: "The name of the seed.",
            resolve: context =>
            {
                Seed seed = CsvDataHelper.GetSeedById(context.Source.SeedID)!;
                return seed.Name;
            }
        );
    }
}
