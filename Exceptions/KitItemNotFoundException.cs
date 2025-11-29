using System;

namespace ShelfApi.Exceptions;

public class KitItemNotFoundException : Exception
{
    public KitItemNotFoundException(int kitId, int itemId)
        : base($"KitItem not found for KitId={kitId} and ItemId={itemId}.")
    {
    }
}