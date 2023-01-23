namespace TicTacToeServer;

public class TicTacToeMatch
{
    public TicTacToeMatch(Guid matchGuid, string user1)
    {
        Board = new int[3, 3];
        User1 = "";
        User2 = "";
        TicTacToeMatchStatus = TicTacToeMatchStatus.Ongoing;
        MatchGuid = matchGuid;
        User1 = user1;
    }

    public int[,] Board { get; set; }
    public string User1 { get; set; }
    public string User2 { get; set; }
    public TicTacToeMatchStatus TicTacToeMatchStatus { get; set; }

    public Guid MatchGuid { get; private set; }
}

public enum TicTacToeMatchStatus
{
    Ongoing,
    XWon,
    OWon,
    Draw
}