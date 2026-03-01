FountainGame.DisplayStartGameMessage();
WorldManager grid = new WorldManager(WorldManager.SetWorldSize());
Player player = new Player('P');
FountainGame game = new FountainGame(grid, player);
game.Run();

public class FountainGame
{
    private MessageManager _messageManager;
    private CommandManager _commandManager;
    private WorldManager _worldManager;
    private Player _player;
    private bool _gameOver = false;
    private bool _gameWin = false;

    public FountainGame(WorldManager grid, Player player)
    {
        _messageManager = new MessageManager();
        _commandManager = new CommandManager();
        _worldManager = grid;
        _player = player;
    }

    public void Run()
    {
        _worldManager.Update(_player);
        Console.WriteLine(new string('-', 60));

        while (!_gameOver)
        {
            // Clear old messsages
            _messageManager.ClearMessages();

            // Run command for current room
            _commandManager.SetCommand(_worldManager.GetCurrentRoom().Command);
            _commandManager.RunCommand(this);

            // Check Win/Lose
            if (CheckWin())
            {
                _gameWin = true;
                break;
            }
            
            if (CheckLose()) break;

            // Display World Grid
            _worldManager.DisplayGridState(_player.Symbol);

            // Display messages
            SetMessages();
            _messageManager.DisplayMessages();
            _messageManager.ClearMessages();

            // Get and run player command
            _commandManager.SetCommand(GetCommand());
            _commandManager.RunCommand(this);

            // Update world grid
            _worldManager.Update(_player);

            _messageManager.AddMessage(new string('-', 60));
            _messageManager.DisplayMessages();
        }

        if (_gameWin) _messageManager.AddMessage("You Win!", ConsoleColor.Green);
        else _messageManager.AddMessage("You Lose!", ConsoleColor.Red);

        _worldManager.DisplayGridState(_player.Symbol);
        _messageManager.DisplayMessages();
    }

    public static void DisplayStartGameMessage()
    {
        ConsoleColor foregroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("You enter the Cavern of Objects, a maze of rooms filled with dangerous pits in search of the Fountain of Objects.");
        Console.WriteLine("Light is visible only in the entrance, and no other light is seen anywhere in the caverns.");
        Console.WriteLine("You must navigate the Caverns with your other senses.");
        Console.WriteLine("Find the Fountain of Objects, enable it, and return to the entrance.");
        Console.WriteLine("Look out for pits. You will feel a breeze if a pit is in an adjacent room. If you enter a room with a pit, you will die.");
        Console.ForegroundColor = foregroundColor;

        Console.WriteLine();
        Console.Write("Press any key to continue...");
        Console.ReadKey();
        Console.Clear();
    }

    private string PlayerLocationMessage() => 
        $"You are in the room at (Row={_player.Row}, Column={_player.Column}).";

    private void SetMessages()
    {
        // Player location
        _messageManager.AddMessage(PlayerLocationMessage());

        // Current room message
        IRoom currentRoom = _worldManager.GetRoom(_player.Row, _player.Column);
        if (currentRoom.InRoomMessage != null)
            _messageManager.AddMessage(currentRoom.InRoomMessage, currentRoom.Color);

        // Any adjacent room messages
        List<IRoom> adjacentRooms = _worldManager.GetAdjacentRooms(_player);
        foreach (IRoom room in adjacentRooms)
            if (room.AdjacentMessage != null) _messageManager.AddMessage(room.AdjacentMessage);
    }

    private ICommand GetCommand()
    {
        ICommand command = null!;

        while (command == null || command.GetType() == typeof(DefaultCommand))
        {
            string? commandChoice = AskPlayerWhatToDo();

            
            command = commandChoice?.ToLower() switch
            {
                "move north" => new NorthCommand(),
                "move east" => new EastCommand(),
                "move south" => new SouthCommand(),
                "move west" => new WestCommand(),
                "enable fountain" => new EnableFountainCommand(),
                "help" => new HelpCommand(),
                _ => new DefaultCommand()
            };

            if (command == null || command.GetType() == typeof(DefaultCommand))
                ColourConsole.WriteLineWithColour("That wasn't a valid command.", ConsoleColor.Red);
        }

        return command;
    }

    private string? AskPlayerWhatToDo()
    {
        Console.Write("What do you want to do? ");

        ConsoleColor foregroundColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Cyan;
        string? text = Console.ReadLine();
        Console.ForegroundColor = foregroundColor;
        return text;
    }

    private bool CheckWin()
    {
        FountainOfObjectsRoom fountainRoom = 
            (FountainOfObjectsRoom)_worldManager.GetFountainRoom();

        if (fountainRoom.Enabled && _player.Row == 0 && _player.Column == 0)
            return true;

        return false;
    }

    private bool CheckLose() => _player.Alive ? false : true;

    public MessageManager GetMessageManager() => _messageManager;
    public Player GetPlayer() => _player;
    public WorldManager GetWorldManager() => _worldManager;
    public int GetWorldSize() => _worldManager.Size;
}

// Handles messages related to the game - e.g. current player location and any room messages.
public class MessageManager
{
    private List<(string? text, ConsoleColor colour)> Messages { get; set; } 
        = new List<(string? text, ConsoleColor colour)>();

    public void AddMessage(string? text, ConsoleColor colour = ConsoleColor.White) => Messages.Add((text, colour));
    public void DisplayMessages()
    {
        foreach (var message in Messages)
        {
            if (message.text != null)
                ColourConsole.WriteLineWithColour(message.text, message.colour);
        }            
    }

    public void ClearMessages() => Messages.Clear();
}

// ==================================================================
// |                          World Grid                            |
// ==================================================================
public class WorldManager
{
    private WorldSize _worldSize;
    private IRoom _currentRoom;

    public int Size { get; init; }
    public IRoom[,] Grid { get; private set; }

    public WorldManager(WorldSize worldSize)
    {
        _worldSize = worldSize;
        Size = _worldSize switch
        {
            WorldSize.Small => 4,
            WorldSize.Medium => 6,
            WorldSize.Large => 8,
            _ => 4
        };
        Grid = InitialiseGrid(worldSize, Size);
        _currentRoom = Grid[0, 0];
    }

    public static WorldSize SetWorldSize()
    {
        string? worldSizeChoice = AskPlayerForWorldSize();
        WorldSize worldSize = worldSizeChoice.ToLower() switch
        {
            "small" => WorldSize.Small,
            "medium" => WorldSize.Medium,
            "large" => WorldSize.Large
        };

        Console.Clear();

        return worldSize;
    }

    private static string AskPlayerForWorldSize()
    {
        string worldSizeChoice = null!;
        bool validChoice = false;

        while (!validChoice)
        {
            Console.Write("What size world would you like to play (small, medium, large): ");
            worldSizeChoice = Console.ReadLine().ToLower();

            if (worldSizeChoice == "small" ||
                worldSizeChoice == "medium" ||
                worldSizeChoice == "large")
                validChoice = true;
            else
                ColourConsole.WriteLineWithColour("That wasn't a valid choice.", ConsoleColor.Red);
        }

        return worldSizeChoice;
    }

    private IRoom[,] InitialiseGrid(WorldSize worldSize, int size)
    {
        IRoom[,] grid = new IRoom[size, size];

        // Make every room generic first
        for (int row = 0; row < size; row++)
        {
            for (int col = 0; col < size; col++)
            {
                grid[row, col] = new GenericRoom(row, col);
            }
        }

        // Entrance will always be at 0, 0
        grid[0, 0] = new EntranceRoom(0, 0);

        SetFountainRoom(grid);
        SetPitRoom(grid);
        SetMaelstromRoom(grid);

        return grid;
    }

    private void SetFountainRoom(IRoom[,] grid)
    {
        if (_worldSize == WorldSize.Small) grid[0, 2] = new FountainOfObjectsRoom(0, 2);
        else if (_worldSize == WorldSize.Medium) grid[1, 4] = new FountainOfObjectsRoom(1, 4);
        else if (_worldSize == WorldSize.Large) grid[4, 7] = new FountainOfObjectsRoom(4, 7);
    }

    private void SetPitRoom(IRoom[,] grid)
    {
        if (_worldSize == WorldSize.Small) grid[0, 1] = new PitRoom(0, 1);
        else if (_worldSize == WorldSize.Medium) grid[1, 3] = new PitRoom(1, 3);
        else if (_worldSize == WorldSize.Large) grid[4, 6] = new PitRoom(4, 6);
    }

    private void SetMaelstromRoom(IRoom[,] grid)
    {
        if (_worldSize == WorldSize.Small) grid[2, 0] = new MaelstromRoom(2, 0);
        else if (_worldSize == WorldSize.Medium) grid[1, 3] = new PitRoom(1, 1);
        else if (_worldSize == WorldSize.Large) grid[4, 6] = new PitRoom(3, 4);
    }

    public IRoom GetFountainRoom()
    {
        if (_worldSize == WorldSize.Small) return Grid[0, 2];
        if (_worldSize == WorldSize.Medium) return Grid[1, 4];
        return Grid[4, 7];
    }

    public IRoom GetRoom(int x, int y) => Grid[x, y];
    public IRoom GetCurrentRoom() => _currentRoom;

    public List<IRoom> GetAdjacentRooms(Player player)
    {
        List<IRoom> adjacentRooms = new List<IRoom>();

        int x = player.Row;
        int y = player.Column;

        if (IsValidRoom(x + 1, y)) adjacentRooms.Add(Grid[player.Row + 1, player.Column]);
        if (IsValidRoom(x - 1, y)) adjacentRooms.Add(Grid[player.Row - 1, player.Column]);
        if (IsValidRoom(x, y + 1)) adjacentRooms.Add(Grid[player.Row, player.Column + 1]);
        if (IsValidRoom(x, y - 1)) adjacentRooms.Add(Grid[player.Row, player.Column - 1]);
        if (IsValidRoom(x + 1, y + 1)) adjacentRooms.Add(Grid[player.Row + 1, player.Column + 1]);
        if (IsValidRoom(x + 1, y - 1)) adjacentRooms.Add(Grid[player.Row + 1, player.Column - 1]);
        if (IsValidRoom(x - 1, y - 1)) adjacentRooms.Add(Grid[player.Row - 1, player.Column - 1]);
        if (IsValidRoom(x - 1, y + 1)) adjacentRooms.Add(Grid[player.Row - 1, player.Column + 1]);

        return adjacentRooms;
    }

    private bool IsValidRoom(int x, int y)
    {
        if (x < 0 || x >= Size) return false;
        if (y < 0 || y >= Size) return false;

        return true;
    }

    // Displays the current board state.
    public void DisplayGridState(char playerSymbol)
    {
        _printDivider();
        Console.WriteLine();
        for (int i = 0; i < Size; i++)
        {
            // Print the state of each cell in the Board.
            for (int j = 0; j < Size; j++)
            {
                Console.Write("|");
                // Get the current string to print onto the board.
                char cellText = Grid[i, j].Discovered == true
                                    ? Grid[i, j].RoomSymbol
                                    : ' ';

                Console.Write(" ");

                // Display the state of the cell.
                if (Grid[i, j].PlayerInRoom)
                    ColourConsole.WriteWithColour($"{playerSymbol}", ConsoleColor.Magenta);
                else
                    ColourConsole.WriteWithColour($"{cellText}", Grid[i, j].Color);

                Console.Write(" ");

                if (j == Size - 1)
                    Console.Write("|");
            }
            Console.WriteLine();
            _printDivider();
            Console.WriteLine();
        }

        void _printDivider()
        {
            Console.Write("+");
            for (int k = 0; k < Size; k++)
            {
                Console.Write(new string('-', 3));

                // Print column divider, if not the last cell in the row.
                Console.Write("+");
            }
        }
    }

    public void Update(Player player)
    {
        foreach (IRoom room in Grid)
        {
            if (room.PlayerInRoom) room.PlayerInRoom = false;
        }

        Grid[player.Row, player.Column].PlayerInRoom = true;
        _currentRoom = Grid[player.Row, player.Column];

        if (!_currentRoom.Discovered) _currentRoom.Discovered = true;
    }

    public void ResetBoard()
    {
        Grid = InitialiseGrid(_worldSize, Size);
    }
}

public enum WorldSize { Small, Medium, Large }

public interface IRoom
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool PlayerInRoom { get; set; }
    public bool Discovered { get; set; }
    public char RoomSymbol { get; set; }
    public ConsoleColor Color { get; set; }
    public string? InRoomMessage { get; set; }
    public string? AdjacentMessage { get; set; }
    public ICommand? Command { get; init; }
}

public class GenericRoom : IRoom
{
    public int Row { get; set; }
    public int Column { get; set; }
    public bool PlayerInRoom { get; set; }
    public bool Discovered { get; set; }
    public char RoomSymbol { get; set; }
    public ConsoleColor Color { get; set; }
    public string? InRoomMessage { get; set; }
    public string? AdjacentMessage { get; set; }
    public ICommand? Command { get; init; }

    public GenericRoom(int row, int column)
    {
        Row = row;
        Column = column;
        RoomSymbol = ' ';
        Color = ConsoleColor.White;
        InRoomMessage = null;
        AdjacentMessage = null;
        Command = null;
    }
}

public class FountainOfObjectsRoom : GenericRoom
{
    public bool Enabled { get; set; } = false;

    public FountainOfObjectsRoom(int row, int column) : base(row, column) 
    {
        RoomSymbol = '^';
        Color = ConsoleColor.Blue;
        InRoomMessage = "You hear water dripping in this room. The Fountain of Objects is here!";
    }
}

public class EntranceRoom : GenericRoom
{
    public EntranceRoom(int row, int column) : base(row, column)
    {
        RoomSymbol = '*';
        Color = ConsoleColor.Yellow;
        InRoomMessage = "You see light coming from the cavern entrance.";
    }
}

public class PitRoom : GenericRoom
{
    public PitRoom(int row, int column) : base(row, column)
    {
        Command = new PitCommand();
        RoomSymbol = '_';
        Color = ConsoleColor.Red;
        InRoomMessage = "You fall into a pit and die.";
        AdjacentMessage = "You feel a draft. There is a pit in a nearby room.";
    }
}

public class MaelstromRoom : GenericRoom
{
    public MaelstromRoom(int row, int column) : base(row, column)
    {
        Command = new MaelstromCommand();
        RoomSymbol = '@';
        Color = ConsoleColor.Red;
        InRoomMessage = "You walked into a maelstrom, and have been swept elsewhere.";
        AdjacentMessage = "You hear the growling and groaning of a maelstrom nearby.";
    }
}

public class Player
{
    public int Row { get; set; }
    public int Column { get; set; }
    public char Symbol { get; private set; }
    public bool Alive { get; set; }

    public Player(char symbol)
    {
        Row = 0;
        Column = 0;
        Symbol = symbol;
        Alive = true;
    }
}

public interface ICommand
{
    public void Run(FountainGame game);
}

public class MaelstromCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        IRoom currentRoom = game.GetWorldManager().GetCurrentRoom();

        if (currentRoom.GetType() != typeof(MaelstromRoom))
            return;

        // Move player 1 space south, and two spaces west.
        int worldSize = game.GetWorldSize();
        if (player.Row >= worldSize) player.Row = 0;
        else player.Row += 1;

        if (player.Column == 0) player.Column = worldSize - 2;
        else if (player.Column == 1) player.Column = worldSize - 1;
        else player.Column -= 2;

        game.GetMessageManager().AddMessage(currentRoom.InRoomMessage, ConsoleColor.Red);
        game.GetWorldManager().Update(player);
    }
}

public class PitCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        IRoom currentRoom = game.GetWorldManager().GetCurrentRoom();

        if (currentRoom.GetType() != typeof(PitRoom))
            return;
        
        player.Alive = false;
        game.GetMessageManager().AddMessage(currentRoom.InRoomMessage, ConsoleColor.Red);
    }
}

public class HelpCommand : ICommand
{
    public void Run(FountainGame game)
    {
        ColourConsole.WriteWithColour("move (north, east, south, west): ", ConsoleColor.Cyan);
        Console.WriteLine("Move the player along the grid.");

        ColourConsole.WriteWithColour("enable fountain: ", ConsoleColor.Cyan);
        Console.WriteLine("Enables the Fountain of Objects.");

        Console.WriteLine();
        Console.Write("Press any key to continue...");
        Console.ReadKey();
        Console.WriteLine();
    }
}

public class EnableFountainCommand : ICommand
{
    public void Run(FountainGame game)
    {
        FountainOfObjectsRoom fountainRoom = (FountainOfObjectsRoom)game.GetWorldManager().GetFountainRoom();
        Player player = game.GetPlayer();
        if (fountainRoom.PlayerInRoom)
        {
            fountainRoom.Enabled = true;
            fountainRoom.InRoomMessage = "You hear the rushing waters from the Fountain of Objects. It has been reactivated!";

            game.GetWorldManager().GetRoom(0, 0).InRoomMessage = 
                "The Fountain of Objects has been reactived, and you have escaped with your life!";
        }
    }
}

public class DefaultCommand : ICommand
{
    public void Run(FountainGame game) { }
}

public class NorthCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        if (player.Row != 0) player.Row--;
    }
}

public class EastCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.Column != worldSize - 1) player.Column++;
    }
}

public class SouthCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.Row != worldSize - 1) player.Row++;
    }
}

public class WestCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.Column != 0) player.Column--;
    }
}

class CommandManager
{
    private ICommand? Command;

    public void SetCommand(ICommand? command) => Command = command;
    public void RunCommand(FountainGame game)
    {
        if (Command != null)
            Command?.Run(game);
    }
}

public static class ColourConsole
{
    public static void WriteWithColour(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor = ConsoleColor.Black)
    {
        ConsoleColor defaultForegroundColor = Console.ForegroundColor;
        ConsoleColor defaultBackgroundColor = Console.BackgroundColor;

        Console.ForegroundColor = foregroundColor;
        Console.BackgroundColor = backgroundColor;

        Console.Write(text);

        Console.ForegroundColor = defaultForegroundColor;
        Console.BackgroundColor = defaultBackgroundColor;
    }

    public static void WriteLineWithColour(string text, ConsoleColor foregroundColor, ConsoleColor backgroundColor = ConsoleColor.Black)
    {
        ConsoleColor defaultForegroundColor = Console.ForegroundColor;
        ConsoleColor defaultBackgroundColor = Console.BackgroundColor;

        Console.ForegroundColor = foregroundColor;
        Console.BackgroundColor = backgroundColor;

        Console.WriteLine(text);

        Console.ForegroundColor = defaultForegroundColor;
        Console.BackgroundColor = defaultBackgroundColor;
    }
}