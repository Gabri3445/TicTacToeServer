namespace TicTacToeServer.Controllers.Responses;

public class CreateMatchResponse
{
    public CreateMatchResponse(string guid)
    {
        Guid = guid;
    }

    public string Guid { get; set; }
}

public class GetBoardStatusResponse 
{
    public GetBoardStatusResponse(int rows, int columns, int[] flatBoard)
    {
        Rows = rows;
        Columns = columns;
        FlatBoard = flatBoard;
    }

    public int Rows { get; set; }
    public int Columns { get; set; }
    public int[] FlatBoard { get; set; }
}

public class GetPlayerResponse
{
    public GetPlayerResponse(int player)
    {
        Player = player;
    }

    public int Player { get; set; }
}

public class CheckWinResponse
{
    public CheckWinResponse(int matchStatus)
    {
        MatchStatus = matchStatus;
    }

    public int MatchStatus { get; set; }
}