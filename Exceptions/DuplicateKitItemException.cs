using System;

namespace ShelfApi.Exceptions;

public class DuplicateKitItemException : Exception
{
    public int KitId { get; }
    public int ItemId { get; }

    public DuplicateKitItemException(int kitId, int itemId)
        : base("Este item já existe na composição deste kit. Incremente a quantidade do item existente em vez de adicioná-lo novamente.")
    {
        KitId = kitId;
        ItemId = itemId;
    }
}
