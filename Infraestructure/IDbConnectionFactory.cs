using System.Data;

namespace ShelfApi.Infrastructure;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}