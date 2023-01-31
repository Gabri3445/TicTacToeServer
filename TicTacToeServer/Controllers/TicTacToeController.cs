using Microsoft.AspNetCore.Mvc;

namespace TicTacToeServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TicTacToeController : ControllerBase
{
    private readonly ILogger<TicTacToeController> _logger;

    public TicTacToeController(ILogger<TicTacToeController> logger)
    {
        _logger = logger;
    }

    [HttpPost("Create")]
    public ActionResult<string> CreateMatch([FromBody] string username)
    {
        if (TicTacToeData.TicTacToeMatches.Any(ticTacToeMatch =>
                ticTacToeMatch.User1.Equals(username) || ticTacToeMatch.User2.Equals(username)))
        {
            _logger.Log(LogLevel.Error, "Username {Username} already created a match or is already in one", username);
            return BadRequest();
        }

        if (username.Equals(""))
        {
            _logger.Log(LogLevel.Error, "Username is empty");
            return BadRequest();
        }

        var guid = Guid.NewGuid();

        TicTacToeData.TicTacToeMatches.Add(new TicTacToeMatch(guid, username));
        _logger.Log(LogLevel.Information, "{Username} created a match with UUID: {Guid}", username, guid);
        return Ok(guid.ToString());
    }

    private TicTacToeMatch? GetMatch(Guid guid, List<TicTacToeMatch> ticTacToeMatches)
    {
        foreach (var ticTacToeMatch in ticTacToeMatches)
            if (ticTacToeMatch.MatchGuid.Equals(guid))
                return ticTacToeMatch;
        return null;
    }

    [HttpGet("CheckP2Connection")]
    // ReSharper disable once InconsistentNaming
    public ActionResult CheckP2Connected(string _guid)
    {
        Guid guid;
        try
        {
            guid = Guid.Parse(_guid);
        }
        catch (Exception)
        {
            _logger.Log(LogLevel.Error, "Failed to parse UUID: {Uuid}", _guid);
            return BadRequest("Invalid UUID");
        }

        if (GetMatch(guid, TicTacToeData.TicTacToeMatches) == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound("Not found");
        }

        if (TicTacToeData.TicTacToeMatches.Where(iteration => guid.Equals(iteration.MatchGuid))
            .Any(iteration => iteration.User2 == ""))
        {
            _logger.Log(LogLevel.Error, "Player 2 not connected to match with UUID: {Uuid}", _guid);
            return StatusCode(406, "Not connected");
        }

        _logger.Log(LogLevel.Information, "Player 2 connected to match with UUID: {Uuid}", _guid);
        return Ok();
    }

    [HttpPut("ConnectP2")]
    // ReSharper disable once InconsistentNaming
    public ActionResult ConnectP2([FromBody] ConnectP2Arguments arguments)
    {
        var _guid = arguments.Guid;
        var username = arguments.Username;
        Guid guid;
        try
        {
            guid = Guid.Parse(_guid);
        }
        catch (Exception)
        {
            _logger.Log(LogLevel.Error, "Failed to parse UUID: {Uuid}", _guid);
            return BadRequest();
        }

        var ticTacToeMatch = GetMatch(guid, TicTacToeData.TicTacToeMatches);
        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        ticTacToeMatch.User2 = username;
        _logger.Log(LogLevel.Information, "Player 2 connected to match with UUID: {Uuid}", _guid);
        return Ok();
    }

    [HttpGet("GetBoardStatus")]
    // ReSharper disable once InconsistentNaming
    public ActionResult GetBoardStatus(string _guid)
    {
        // JSON return
        Guid guid;
        try
        {
            guid = Guid.Parse(_guid);
        }
        catch (Exception)
        {
            _logger.Log(LogLevel.Error, "Failed to parse UUID: {Uuid}", _guid);
            return BadRequest();
        }

        var ticTacToeMatch = GetMatch(guid, TicTacToeData.TicTacToeMatches);
        if (ticTacToeMatch != null) return Ok(ticTacToeMatch.Board);
        _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
        return NotFound();
    }

    [HttpPut("MakeMove")]
    public ActionResult MakeMove([FromBody] MakeMoveArguments arguments)
    {
        var _guid = arguments.Guid;
        var player = arguments.Player;
        var x = arguments.Location.X;
        var y = arguments.Location.Y;

        if (x > 3 || y > 3 || (player != 1 && player != 2)) return BadRequest();

        Guid guid;
        try
        {
            guid = Guid.Parse(_guid);
        }
        catch (Exception)
        {
            _logger.Log(LogLevel.Error, "Failed to parse UUID: {Uuid}", _guid);
            return BadRequest();
        }

        var ticTacToeMatch = GetMatch(guid, TicTacToeData.TicTacToeMatches);

        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        if (ticTacToeMatch.Board[x, y] != 0) return BadRequest();
        switch (player)
        {
            case 1:
                ticTacToeMatch.Board[x, y] = 1;
                ticTacToeMatch.DrawCounter++;
                return Ok();
            case 2:
                ticTacToeMatch.Board[x, y] = 2;
                ticTacToeMatch.DrawCounter++;
                return Ok();
        }

        return BadRequest();
    }

    // TODO check win

    public class ConnectP2Arguments
    {
        public string Guid { get; set; }
        public string Username { get; set; }
    }

    public class MakeMoveArguments
    {
        public string Guid { get; set; }
        public int Player { get; set; }

        public Location Location { get; set; }
    }

    public class Location
    {
        public int X { get; set; }
        public int Y { get; set; }
    }
}