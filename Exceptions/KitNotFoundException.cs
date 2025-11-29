using System;

namespace ShelfApi.Exceptions;

public class KitNotFoundException : Exception
{
    public KitNotFoundException(int id)
        : base($"Kit with id {id} not found.")
    {
    }
}