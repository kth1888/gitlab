namespace Savor22b.Action;

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Bencodex.Types;
using Libplanet.Action;
using Libplanet.Headless.Extensions;

[Serializable]
public abstract class SVRAction : SVRBaseAction
{
    public Guid Id { get; private set; }
    public override IValue PlainValue => Dictionary.Empty
        .Add("type_id", this.GetType().GetCustomAttribute<ActionTypeAttribute>() is { } attribute
            ? attribute.TypeIdentifier
            : throw new NullReferenceException($"Type is missing {nameof(ActionTypeAttribute)}: {this.GetType()}"))
#pragma warning disable LAA1002
        .Add("values", new Bencodex.Types.Dictionary(
            PlainValueInternal
                .SetItem("id", Id.Serialize())
                .Select(kv => new KeyValuePair<IKey, IValue>((Text) kv.Key, kv.Value))));
#pragma warning restore LAA1002

    protected abstract IImmutableDictionary<string, IValue> PlainValueInternal { get; }

    protected SVRAction()
    {
        Id = Guid.NewGuid();
    }

    public override void LoadPlainValue(IValue plainValue)
    {
#pragma warning disable LAA1002
        var dict = ((Dictionary)((Dictionary)plainValue)["values"])
            .Select(kv => new KeyValuePair<string, IValue>((Text) kv.Key, kv.Value))
            .ToImmutableDictionary();
#pragma warning restore LAA1002
        Id = dict["id"].ToGuid();
        LoadPlainValueInternal(dict);
    }

    protected abstract void LoadPlainValueInternal(IImmutableDictionary<string, IValue> plainValue);
}
