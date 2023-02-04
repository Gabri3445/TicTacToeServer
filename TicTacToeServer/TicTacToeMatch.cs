using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace TicTacToeServer;

// TODO add comments to here and to the controller
public class TicTacToeMatch
{
    // TODO add a date and delete the old matches from the DB
    // Might not need this as the DB document is only 258.0 B

    public TicTacToeMatch(Guid matchGuid, string user1)
    {
        CurrentPlayer = TicTacToeMatchStatus.X;
        Board = new int[3, 3];
        User1 = "";
        User2 = "";
        MatchGuid = matchGuid;
        User1 = user1;
        DrawCounter = 0;
        Id = matchGuid.ToString();
    }

    // Does not contain the status of the match, only the current player
    public TicTacToeMatchStatus CurrentPlayer { get; set; }

    /*
     * 0 = No player
     * 1 = X
     * 2 = O
     */
    public int[,] Board { get; set; }
    public string User1 { get; set; }

    public string User2 { get; set; }

    public int DrawCounter { get; set; }

    [BsonRepresentation(BsonType.String)] public Guid MatchGuid { get; set; }

    [BsonId] private string Id { get; }

    public TicTacToeMatchStatus CheckVictory()
    {
        if (DrawCounter == 9) return TicTacToeMatchStatus.Draw;

        // check columns
        for (var i = 0; i < 3; i++)
            if (Board[i, 0] == Board[i, 1] && Board[i, 1] == Board[i, 2] && Board[i, 0] != 0)
                switch (Board[i, 0])
                {
                    case 1:
                        return TicTacToeMatchStatus.XWon;
                    case 2:
                        return TicTacToeMatchStatus.OWon;
                }

        for (var i = 0; i < 3; i++)
            if (Board[0, i] == Board[1, i] && Board[1, i] == Board[2, i] && Board[0, i] != 0)
                switch (Board[0, i])
                {
                    case 1:
                        return TicTacToeMatchStatus.XWon;
                    case 2:
                        return TicTacToeMatchStatus.OWon;
                }

        // check diagonals
        if (Board[0, 0] == Board[1, 1] && Board[1, 1] == Board[2, 2] && Board[0, 0] != 0)
            switch (Board[0, 0])
            {
                case 1:
                    return TicTacToeMatchStatus.XWon;
                case 2:
                    return TicTacToeMatchStatus.OWon;
            }

        if (Board[0, 2] == Board[1, 1] && Board[1, 1] == Board[2, 0] && Board[0, 2] != 0)
            switch (Board[0, 2])
            {
                case 1:
                    return TicTacToeMatchStatus.XWon;
                case 2:
                    return TicTacToeMatchStatus.OWon;
            }


        // no winner
        return TicTacToeMatchStatus.Ongoing;
    }
}

public enum TicTacToeMatchStatus
{
    Ongoing,
    XWon,
    OWon,
    Draw,
    X,
    O
}