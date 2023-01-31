using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicTacToeServer;

public class TicTacToeMatch
{
    public TicTacToeMatch(Guid matchGuid, string user1)
    {
        Board = new int[3, 3];
        User1 = "";
        User2 = "";
        // TicTacToeMatchStatus = TicTacToeMatchStatus.Ongoing;
        MatchGuid = matchGuid;
        User1 = user1;
        DrawCounter = 0;
        Id = matchGuid.ToString();
    }

    /*
     * 0 = No player
     * 1 = X
     * 2 = O
     */
    public int[,] Board { get; set; }
    public string User1 { get; set; }

    public string User2 { get; set; }
    // public TicTacToeMatchStatus TicTacToeMatchStatus { get; set; }

    public int DrawCounter { get; set; }

    [BsonRepresentation(BsonType.String)] public Guid MatchGuid { get; set; }

    [BsonId] private string Id { get; }

    public TicTacToeMatchStatus CheckVictory()
    {
        if (DrawCounter == 9) return TicTacToeMatchStatus.Draw;
        return TicTacToeMatchStatus.Ongoing;
    }
}

public enum TicTacToeMatchStatus
{
    Ongoing,
    XWon,
    OWon,
    Draw
}