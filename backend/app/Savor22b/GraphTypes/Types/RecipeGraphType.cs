namespace Savor22b.GraphTypes.Types;

using GraphQL.Types;

public class RecipeGraphType
{
    public class RecipeResponseType : ObjectGraphType<RecipeResponse>
    {
        public RecipeResponseType()
        {
            Field<IntGraphType>(
                name: "id",
                description: "The ID of the recipe.",
                resolve: context =>
                {
                    return context.Source.Id;
                }
            );

            Field<StringGraphType>(
                name: "name",
                description: "The name of the recipe.",
                resolve: context => context.Source.Name
            );

            Field<RecipeComponentType>(
                name: "resultFood",
                description: "The Result Food of the recipe.",
                resolve: context => context.Source.ResultFood
            );

            Field<IntGraphType>(
                name: "requiredBlockCount",
                description: "The RequiredBlock of the recipe.",
                resolve: context => context.Source.RequiredBlock
            );

            Field<ListGraphType<RecipeComponentType>>(
                name: "requiredKitchenEquipmentCategoryList",
                description: "The list of ingredients in the recipe.",
                resolve: context => context.Source.RequiredKitchenEquipmentCategoryList.ToList()
            );

            Field<ListGraphType<RecipeComponentType>>(
                name: "ingredientIDList",
                description: "The list of ingredients in the recipe.",
                resolve: context => context.Source.IngredientList.ToList()
            );

            Field<ListGraphType<RecipeComponentType>>(
                name: "foodIDList",
                description: "The list of ingredients in the recipe.",
                resolve: context => context.Source.FoodList.ToList()
            );
        }
    }

    public class RecipeComponentType : ObjectGraphType<RecipeComponent>
    {
        public RecipeComponentType()
        {
            Field<IntGraphType>(
                name: "id",
                description: "The ID of the recipe component.",
                resolve: context =>
                {
                    return context.Source.Id;
                }
            );

            Field<StringGraphType>(
                name: "name",
                description: "The name of the recipe component.",
                resolve: context => context.Source.Name
            );
        }
    }
}
