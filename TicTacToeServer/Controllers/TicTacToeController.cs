using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace TicTacToeServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TicTacToeController : ControllerBase
{
    private readonly IMongoCollection<TicTacToeMatch> _collection;
    private readonly IMongoDatabase _database;
    private readonly ILogger<TicTacToeController> _logger;

    // TODO Make classes for the responses
    // TODO Consider using custom response codes DO NOT FORGET TO DOCUMENT THEM

    public TicTacToeController(ILogger<TicTacToeController> logger)
    {
        var client = MongoDbClientSingleton.Instance;
        _database = client.Client.GetDatabase("tictactoe");
        _collection = _database.GetCollection<TicTacToeMatch>("matches");
        _logger = logger;
    }

    [HttpPost("Create")]
    [ProducesResponseType(400)]
    [ProducesResponseType(typeof(string), 200)]
    public async Task<ActionResult<string>> CreateMatch([FromBody] string username)
    {
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
        await _collection.InsertOneAsync(new TicTacToeMatch(guid, username));
        _logger.Log(LogLevel.Information, "{Username} created a match with UUID: {Guid}", username, guid);
        return Ok(guid.ToString());
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
    [ProducesResponseType(200)]
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
        return Ok();
    }

    [HttpPut("ConnectP2")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(200)]
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
        return Ok();
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
    [ProducesResponseType(typeof(int[]), 200)]
    // ReSharper disable once InconsistentNaming
    public ActionResult<int[]> GetBoardStatus(string _guid)
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

            var response = new
            {
                Rows = rows,
                Columns = columns,
                Board = flatBoard
            };

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
    [ProducesResponseType(typeof(int), 200)]
    public ActionResult<int> GetPlayer(string _guid)
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
            return BadRequest("Match over");
        }

        return ticTacToeMatch.CurrentPlayer switch
        {
            TicTacToeMatchStatus.X => Ok(1),
            TicTacToeMatchStatus.O => Ok(2),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    [HttpGet("CheckWin")]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(typeof(int), 200)]
    public ActionResult<int> CheckWin(string _guid)
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
            TicTacToeMatchStatus.X => Ok(1),
            TicTacToeMatchStatus.O => Ok(2),
            TicTacToeMatchStatus.Ongoing => Ok(3),
            TicTacToeMatchStatus.Draw => Ok(4),
            TicTacToeMatchStatus.OWon => Ok(5),
            TicTacToeMatchStatus.XWon => Ok(6),
            _ => StatusCode(500)
        };
    }


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