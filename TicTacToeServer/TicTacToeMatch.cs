namespace TicTacToeServer;

public class TicTacToeMatch
{
    public int[,] Board { get; set; }
    public string User1 { get; set; }
    public string User2 { get; set; }

    public TicTacToeMatch()
    {
        this.Board = new int[3, 3];
        User1 = "";
        User2 = "";
        TicTacToeMatchStatus = TicTacToeMatchStatus.Ongoing;
    }
    public TicTacToeMatchStatus TicTacToeMatchStatus { get; set; }
}

public enum TicTacToeMatchStatus
{
    Ongoing,
    XWon,
    OWon,
    Draw
}