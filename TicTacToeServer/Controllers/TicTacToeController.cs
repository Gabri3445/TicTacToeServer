using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using TicTacToeServer.Controllers.Responses;
using TicTacToeServer.Controllers.RequestBody;
using MongoDB.Driver;

namespace TicTacToeServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TicTacToeController : ControllerBase
{
    private readonly IMongoCollection<TicTacToeMatch> _collection;
    private readonly IMongoDatabase _database;
    private readonly ILogger<TicTacToeController> _logger;
    
    // TODO Consider using custom response codes DO NOT FORGET TO DOCUMENT THEM

    public TicTacToeController(ILogger<TicTacToeController> logger)
    {
        var client = MongoDbClientSingleton.Instance;
        _database = client.Client.GetDatabase("tictactoe");
        _collection = _database.GetCollection<TicTacToeMatch>("matches");
        _logger = logger;
    }

    [HttpGet("Ping")]
    [ProducesResponseType(200)]
    public ActionResult Ping()
    {
        _logger.Log(LogLevel.Information, "Ping from {Ip}", HttpContext.Request.Host);
        return Ok();
    }
    
    [HttpPost("Create")]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(CreateMatchResponse), 200)]
    public ActionResult<CreateMatchResponse> CreateMatch([FromBody] CreateMatchArguments createMatchArguments)
    {
        var username = createMatchArguments.Username;
        if (username.Equals(""))
        {
            _logger.Log(LogLevel.Error, "Username is empty");
            return BadRequest();
        }

        // Won't need this as uuid's are the unique IDs, not the usernames
        /*
         var filter = Builders<TicTacToeMatch>.Filter.Or(
            Builders<TicTacToeMatch>.Filter.Eq(x => x.User1, username),
            Builders<TicTacToeMatch>.Filter.Eq(x => x.User2, username)
        );
        var ticTacToeMatches = _collection.Find(filter).ToList();
        if (ticTacToeMatches.Any())
        {
            _logger.Log(LogLevel.Error, "Username {Username} already created a match or is already in one", username);
            return BadRequest();
        }
        */

        var guid = Guid.NewGuid();
        _collection.InsertOne(new TicTacToeMatch(guid, username));
        _logger.Log(LogLevel.Information, "{Username} created a match with UUID: {Guid}", username, guid);
        CreateMatchResponse createMatchResponse = new(guid.ToString());
        return Ok(createMatchResponse);
    }

    private TicTacToeMatch? GetMatch(Guid guid)
    {
        var filter = Builders<TicTacToeMatch>.Filter.Eq(x => x.MatchGuid, guid);
        var ticTacToeMatch = _collection.Find(filter).ToList();
        switch (ticTacToeMatch.Count)
        {
            case 1:
                return ticTacToeMatch[0];
            case 0:
            case <= 1:
                return null;
            default:
                _logger.Log(LogLevel.Error, "Fatal Error: Two matches with the same ID");
                throw new Exception("Two matches with the same ID, check the database");
        }
    }


    [HttpGet("CheckP2Connection")]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(typeof(string), 404)]
    [ProducesResponseType(typeof(string), 406)]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    // ReSharper disable once InconsistentNaming
    // Returns the player 2 username
    public ActionResult<PlayerResponse> CheckP2Connected([FromQuery(Name = "guid")] string _guid)
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

        var ticTacToeMatch = GetMatch(guid);

        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound("Not found");
        }

        if (ticTacToeMatch.User2.Equals(""))
        {
            _logger.Log(LogLevel.Error, "Player 2 not connected to match with UUID: {Uuid}", _guid);
            return StatusCode(406, "Not connected");
        }

        _logger.Log(LogLevel.Information, "Player 2 connected to match with UUID: {Uuid}", _guid);
        return Ok(new PlayerResponse(ticTacToeMatch.User2));
    }

    [HttpPut("ConnectP2")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(PlayerResponse), 200)]
    // Returns the player 1 username
    // ReSharper disable once InconsistentNaming
    public ActionResult<PlayerResponse> ConnectP2([FromBody] ConnectP2Arguments arguments)
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

        var ticTacToeMatch = GetMatch(guid);
        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        if (ticTacToeMatch.User2 != "")
        {
            _logger.Log(LogLevel.Error, "Already connected to match with UUID: {Uuid}", _guid);
            return BadRequest();
        }

        ticTacToeMatch.User2 = username;
        var filter = Builders<TicTacToeMatch>.Filter.Eq(x => x.MatchGuid, guid);
        var update = Builders<TicTacToeMatch>.Update.Set(x => x.User2, username);
        _collection.UpdateOne(filter, update);
        // ticTacToeMatch.User2 = username;
        _logger.Log(LogLevel.Information, "Player 2 connected to match with UUID: {Uuid}", _guid);
        return Ok(new PlayerResponse(ticTacToeMatch.User1));
    }

    // This is for javascript
    /*
     * const apiResponse = {
      "rows": 3,
      "columns": 3,
      "board": [
        1,
        0,
        0,
        1,
        0,
        0,
        0,
        0,
        0
      ]
    };

    const rows = apiResponse.rows;
    const columns = apiResponse.columns;
    const board = apiResponse.board;

    const twoDimensionalArray = [];

    for (let i = 0; i < rows; i++) {
      const startIndex = i * columns;
      const row = board.slice(startIndex, startIndex + columns);
      twoDimensionalArray.push(row);
    }

     */

    [HttpGet("GetBoardStatus")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(GetBoardStatusResponse), 200)]
    // ReSharper disable once InconsistentNaming
    public ActionResult<GetBoardStatusResponse> GetBoardStatus([FromQuery(Name = "guid")] string _guid)
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

        var ticTacToeMatch = GetMatch(guid);
        if (ticTacToeMatch != null)
        {
            var rows = ticTacToeMatch.Board.GetLength(0);
            var columns = ticTacToeMatch.Board.GetLength(1);
            var flatBoard = new int[rows * columns];
            var index = 0;
            for (var i = 0; i < rows; i++)
            for (var j = 0; j < columns; j++)
                flatBoard[index++] = ticTacToeMatch.Board[i, j];

            var response = new GetBoardStatusResponse(rows, columns, flatBoard);

            return Ok(response);
        }

        _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
        return NotFound();
    }

    [HttpPut("MakeMove")]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
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

        var ticTacToeMatch = GetMatch(guid);


        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        if ((player == 1 && ticTacToeMatch.CurrentPlayer != TicTacToeMatchStatus.X) ||
            (player == 2 && ticTacToeMatch.CurrentPlayer != TicTacToeMatchStatus.O))
        {
            _logger.Log(LogLevel.Error, "Wrong player : {Uuid}", _guid);
            return BadRequest("Wrong player");
        }

        if (ticTacToeMatch.CheckVictory() != TicTacToeMatchStatus.Ongoing)
        {
            _logger.Log(LogLevel.Error, "Match over with : {Uuid}", _guid);
            return BadRequest("Match over");
        }

        if (ticTacToeMatch.Board[x, y] != 0) return BadRequest();
        var filter = Builders<TicTacToeMatch>.Filter.Eq(z => z.MatchGuid, guid);
        ticTacToeMatch = _collection.Find(filter).FirstOrDefault();
        ticTacToeMatch.MatchGuid = guid; // Match guid gets reset for some reason, so need to set it back
        switch (player)
        {
            case 1:
                ticTacToeMatch.Board[x, y] = 1;
                ticTacToeMatch.CurrentPlayer = TicTacToeMatchStatus.O;
                ticTacToeMatch.DrawCounter++;
                _collection.ReplaceOne(filter, ticTacToeMatch);
                return Ok();
            case 2:
                ticTacToeMatch.Board[x, y] = 2;
                ticTacToeMatch.CurrentPlayer = TicTacToeMatchStatus.X;
                ticTacToeMatch.DrawCounter++;
                _collection.ReplaceOne(filter, ticTacToeMatch);
                return Ok();
        }

        return BadRequest();
    }

    [HttpGet("GetPlayer")]
    [ProducesResponseType(typeof(string), 400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(406)]
    [ProducesResponseType(typeof(GetPlayerResponse), 200)]
    public ActionResult<GetPlayerResponse> GetPlayer([FromQuery(Name = "guid")] string _guid)
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

        var ticTacToeMatch = GetMatch(guid);
        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        if (ticTacToeMatch.CheckVictory() != TicTacToeMatchStatus.Ongoing)
        {
            _logger.Log(LogLevel.Error, "Match over with : {Uuid}", _guid);
            return StatusCode(406, "Match over");
        }

        return ticTacToeMatch.CurrentPlayer switch
        {
            TicTacToeMatchStatus.X => Ok(new GetPlayerResponse(1)),
            TicTacToeMatchStatus.O => Ok(new GetPlayerResponse(2)),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [HttpGet("CheckWin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(CheckWinResponse), 200)]
    public ActionResult<CheckWinResponse> CheckWin([FromQuery(Name = "guid")] string _guid)
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

        var ticTacToeMatch = GetMatch(guid);
        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }

        return ticTacToeMatch.CheckVictory() switch
        {
            TicTacToeMatchStatus.X => Ok(new CheckWinResponse(1)),
            TicTacToeMatchStatus.O => Ok(new CheckWinResponse(2)),
            TicTacToeMatchStatus.Ongoing => Ok(new CheckWinResponse(3)),
            TicTacToeMatchStatus.Draw => Ok(new CheckWinResponse(4)),
            TicTacToeMatchStatus.OWon => Ok(new CheckWinResponse(5)),
            TicTacToeMatchStatus.XWon => Ok(new CheckWinResponse(6)),
            _ => StatusCode(500)
        };
    }

    [HttpPut("Reset")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
    public ActionResult Reset([FromBody] ResetRequest resetRequest)
    {
        var _guid = resetRequest.Guid;
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

        var ticTacToeMatch = GetMatch(guid);
        if (ticTacToeMatch == null)
        {
            _logger.Log(LogLevel.Error, "No matches found with UUID: {Uuid}", _guid);
            return NotFound();
        }
        
        /*
         * CurrentPlayer = TicTacToeMatchStatus.X;
        Board = new int[3, 3];
        User1 = "";
        User2 = "";
        MatchGuid = matchGuid;
        User1 = user1;
        DrawCounter = 0;
        Id = matchGuid.ToString();
         */

        ticTacToeMatch.CurrentPlayer = TicTacToeMatchStatus.X;
        ticTacToeMatch.Board = new int[3, 3];
        ticTacToeMatch.DrawCounter = 0;
        ticTacToeMatch.MatchGuid = guid; // Might get reset like in MakeMove()

        var filter = Builders<TicTacToeMatch>.Filter.Eq(x => x.MatchGuid, guid);
        _collection.ReplaceOne(filter, ticTacToeMatch);
        return Ok();
    }
}