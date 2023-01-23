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
    public ActionResult<string> CreateMatch(string username)
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
    
    [HttpGet("CheckConnection")]
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
            return BadRequest();
        }
        if (TicTacToeData.TicTacToeMatches.Any(ticTacToeMatch => Guid.Parse(_guid) != ticTacToeMatch.MatchGuid))
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }
        if (TicTacToeData.TicTacToeMatches.Where(iteration => guid.Equals(iteration.MatchGuid)).Any(iteration => iteration.User2 == ""))
        {
            _logger.Log(LogLevel.Error, "Player 2 not connected to match with UUID: {Uuid}", _guid);
            return StatusCode(406);
        }
        _logger.Log(LogLevel.Information, "Player 2 connected to match with UUID: {Uuid}", _guid);
        return Ok();
    }
    
    // TODO add p2 connect method and then game logic last
}