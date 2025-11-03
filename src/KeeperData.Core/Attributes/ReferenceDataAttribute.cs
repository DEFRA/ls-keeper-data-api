using KeeperData.Core.Repositories;
using System;
using System.Collections.Generic;

namespace KeeperData.Core.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ReferenceDataAttribute<TListDocument, TItem> : Attribute
    where TListDocument : class, IEntity
    where TItem : class, INestedEntity
{
    public string DocumentId { get; }
    public string ItemsPropertyName { get; }

    public ReferenceDataAttribute(string documentId, string itemsPropertyName)
    {
        DocumentId = documentId;
        ItemsPropertyName = itemsPropertyName;
    }
}
