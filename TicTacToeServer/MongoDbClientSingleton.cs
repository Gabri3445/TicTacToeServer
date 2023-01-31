using MongoDB.Driver;

namespace TicTacToeServer;

public class MongoDbClientSingleton
{
    private static IMongoClient client;

    private MongoDbClientSingleton()
    {
        client = new MongoClient("mongodb://localhost:27017");
    }

    public static MongoDbClientSingleton Instance { get; } = new();

    public IMongoClient Client => client;
}