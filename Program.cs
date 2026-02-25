WorldManager grid = new WorldManager(WorldManager.SetWorldSize());
Player player = new Player('#');
FountainGame game = new FountainGame(grid, player);
game.Run();

public class FountainGame
{
    private CommandManager _commandManager;
    private WorldManager _worldManager;
    private Player _player;
    private bool _gameOver = false;
    private bool _gameWin = false;

    public FountainGame(WorldManager grid, Player player)
    {
        _commandManager = new CommandManager();
        _worldManager = grid;
        _player = player;
    }

    public void Run()
    {
        _worldManager.Update(_player);
        while (!_gameOver)
        {
            CheckWin();
            if (_gameOver) break;
            _worldManager.DisplayGridState(_player.Symbol);
            DisplayRoundStatus();
            _commandManager.SetCommand(GetCommand());
            _commandManager.RunCommand(this);
            _worldManager.Update(_player);
            
        }
        DisplayRoundStatus();
        if (_gameWin) Console.WriteLine("You win!");
        else Console.WriteLine("You lose.");
    }

    private void DisplayRoundStatus()
    {
        Console.WriteLine($"You are in the room at (Row={_player.X}, Column={_player.Y}).");
        IRoom currentRoom = _worldManager.GetRoom(_player.X, _player.Y);
        if (currentRoom.Message != null) Console.WriteLine(currentRoom.Message);
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
                "enable fountain" => new EnableCommand(),
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
        string? text = Console.ReadLine();
        return text;
    }

    private void CheckWin()
    {
        FountainOfObjectsRoom fountainRoom = (FountainOfObjectsRoom)_worldManager.GetFountainRoom();
        if (fountainRoom.Enabled && _player.X == 0 && _player.Y == 0)
        {
            _gameOver = true;
            _gameWin = true;
        }
    }

    public Player GetPlayer() => _player;
    public WorldManager GetWorldManager() => _worldManager;
    public int GetWorldSize() => _worldManager.Size;
}

// ==================================================================
// |                          World Grid                            |
// ==================================================================
public class WorldManager
{
    public int Size { get; init; }
    private WorldSize _worldSize;
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

        return worldSize;
    }

    private static string AskPlayerForWorldSize()
    {
        string worldSizeChoice = null;
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

        return grid;
    }

    private void SetFountainRoom(IRoom[,] grid)
    {
        if (_worldSize == WorldSize.Small) grid[0, 2] = new FountainOfObjectsRoom(0, 2);
        else if (_worldSize == WorldSize.Medium) grid[1, 4] = new FountainOfObjectsRoom(1, 4);
        else if (_worldSize == WorldSize.Large) grid[4, 7] = new FountainOfObjectsRoom(4, 7);
    }

    public IRoom GetFountainRoom()
    {
        if (_worldSize == WorldSize.Small) return Grid[0, 2];
        if (_worldSize == WorldSize.Medium) return Grid[1, 4];
        return Grid[4, 7];
    }

    public IRoom GetRoom(int x, int y) => Grid[x, y];

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

        Grid[player.X, player.Y].PlayerInRoom = true;

        if (!Grid[player.X, player.Y].Discovered) Grid[player.X, player.Y].Discovered = true;
    }

    public void ResetBoard()
    {
        Grid = InitialiseGrid(_worldSize, Size);
    }
}

public enum WorldSize { Small, Medium, Large }

public interface IRoom
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool PlayerInRoom { get; set; }
    public bool Discovered { get; set; }
    public char RoomSymbol { get; set; }
    public ConsoleColor Color { get; set; }
    public string Message { get; set; }
}

public class GenericRoom : IRoom
{
    public int X { get; set; }
    public int Y { get; set; }
    public bool PlayerInRoom { get; set; }
    public bool Discovered { get; set; }
    public char RoomSymbol { get; set; }
    public ConsoleColor Color { get; set; }
    public string? Message { get; set; }

    public GenericRoom(int x, int y)
    {
        X = x;
        Y = y;
        RoomSymbol = ' ';
        Color = ConsoleColor.White;
        Message = null;
    }
}

public class FountainOfObjectsRoom : GenericRoom
{
    public bool Enabled { get; set; } = false;

    public FountainOfObjectsRoom(int X, int Y) : base(X, Y) 
    {
        RoomSymbol = '@';
        Color = ConsoleColor.Cyan;
        Message = "You hear water dripping in this room. The Fountain of Objects is here!";
    }
}

public class EntranceRoom : GenericRoom
{
    public EntranceRoom(int X, int Y) : base(X, Y)
    {
        RoomSymbol = '*';
        Color = ConsoleColor.Yellow;
        Message = "You see light coming from the cavern entrance.";
    }
}

public class Player
{
    public int X { get; set; }
    public int Y { get; set; }
    public char Symbol { get; private set; }

    public Player(char symbol)
    {
        X = 0;
        Y = 0;
        Symbol = symbol;
    }
}

public interface ICommand
{
    void Run(FountainGame game);
}

public class EnableCommand : ICommand
{
    public void Run(FountainGame game)
    {
        FountainOfObjectsRoom fountainRoom = (FountainOfObjectsRoom)game.GetWorldManager().GetFountainRoom();
        Player player = game.GetPlayer();
        if (fountainRoom.PlayerInRoom)
        {
            fountainRoom.Enabled = true;
            fountainRoom.Message = "You hear the rushing waters from the Fountain of Objects. It has been reactivated!";

            game.GetWorldManager().GetRoom(0, 0).Message = 
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
        if (player.X != 0) player.X--;
    }
}

public class EastCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.Y != worldSize - 1) player.Y++;
    }
}

public class SouthCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.X != worldSize - 1) player.X++;
    }
}

public class WestCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        int worldSize = game.GetWorldSize();
        if (player.Y != 0) player.Y--;
    }
}

class CommandManager
{
    private ICommand? Command;

    public void SetCommand(ICommand command) => Command = command;
    public void RunCommand(FountainGame game)
    {
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