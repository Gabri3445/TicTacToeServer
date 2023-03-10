namespace TicTacToeServer.Controllers.RequestBody;

public class Location
{
    public int X { get; set; }
    public int Y { get; set; }
}

public class MakeMoveArguments
{
    public string Guid { get; set; }
    public int Player { get; set; }
    public Location Location { get; set; }
}

public class ConnectP2Arguments
{
    public string Guid { get; set; }
    public string Username { get; set; }
}

public class CreateMatchArguments
{
    public string Username { get; set; }
}

public class ResetRequest
{
    public string Guid { get; set; }
}