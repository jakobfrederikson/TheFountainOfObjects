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

            // Run command for current room, if there is an enemy
            IRoom currentRoom = _worldManager.GetCurrentRoom();
            if (currentRoom.Enemy != null && currentRoom.Enemy.Alive == true)
            {
                _commandManager.SetCommand(currentRoom.Enemy.Command);
                _commandManager.RunCommand(this);
            }            

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

    private string PlayerArrowsMessage() =>
        $"You have {_player.Arrows}/5 arrows.";

    private void SetMessages()
    {
        // Player location and arrows
        _messageManager.AddMessage(PlayerLocationMessage());
        _messageManager.AddMessage(PlayerArrowsMessage());

        // Current room message
        IRoom currentRoom = _worldManager.GetRoom(_player.Row, _player.Column);
        if (currentRoom.InRoomMessage != null)
            _messageManager.AddMessage(currentRoom.InRoomMessage, currentRoom.Color);

        // Any adjacent room messages and room enemy messages
        List<IRoom> adjacentRooms = _worldManager.GetAdjacentRooms(_player);
        foreach (IRoom room in adjacentRooms)
        {
            if (room.AdjacentMessage != null) _messageManager.AddMessage(room.AdjacentMessage);
            if (room.Enemy != null && room.Enemy.Alive == true) _messageManager.AddMessage(room.Enemy.AdjacentMessage);
        }
    }

    private ICommand GetCommand()
    {
        ICommand command = null!;

        while (command == null || command.GetType() == typeof(DefaultCommand))
        {
            string? commandChoice = AskPlayerWhatToDo();

            if (commandChoice == null) command = new DefaultCommand();
            else commandChoice = commandChoice.ToLower();

            if (commandChoice == "move north" || commandChoice == "mn") command = new NorthCommand();
            else if (commandChoice == "move east" || commandChoice == "me") command = new EastCommand();
            else if (commandChoice == "move south" || commandChoice == "ms") command = new SouthCommand();
            else if (commandChoice == "move west" || commandChoice == "mw") command = new WestCommand();
            else if (commandChoice == "shoot north" || commandChoice == "sn") command = new ShootNorthCommand();
            else if (commandChoice == "shoot east" || commandChoice == "se") command = new ShootEastCommand();
            else if (commandChoice == "shoot south" || commandChoice == "ss") command = new ShootSouthCommand();
            else if (commandChoice == "shoot west" || commandChoice == "sw") command = new ShootWestCommand();
            else if (commandChoice == "enable fountain" || commandChoice == "ef") command = new EnableFountainCommand();
            else if (commandChoice == "help") command = new HelpCommand();

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
        Grid = InitialiseGrid(Size);
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

    private IRoom[,] InitialiseGrid(int size)
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

        SetSpecialRooms(grid);
        SetEnemies(grid);

        return grid;
    }

    private void SetSpecialRooms(IRoom[,] grid)
    {
        // Entrace will always be at 0, 0
        SetSpecialRoom<EntranceRoom>(grid, new int[1, 2] { { 0, 0 } });

        if (_worldSize == WorldSize.Small) SetSmallSpecialRooms(grid);
        else if (_worldSize == WorldSize.Medium) SetMediumSpecialRooms(grid);
        else if (_worldSize == WorldSize.Large) SetLargeSpecialRooms(grid);
    }

    private void SetSmallSpecialRooms(IRoom[,] grid)
    {
        SetSpecialRoom<EntranceRoom>(grid, new int[1, 2] { { 0, 0 } });
        SetSpecialRoom<FountainOfObjectsRoom>(grid, new int[1, 2] { { 0, 2 } });
        
    }

    private void SetMediumSpecialRooms(IRoom[,] grid)
    {
        SetSpecialRoom<EntranceRoom>(grid, new int[1, 2] { { 0, 0 } });
        SetSpecialRoom<FountainOfObjectsRoom>(grid, new int[1, 2] { { 3, 4 } });
    }

    private void SetLargeSpecialRooms(IRoom[,] grid)
    {
        SetSpecialRoom<EntranceRoom>(grid, new int[1, 2] { { 0, 0 } });
        SetSpecialRoom<FountainOfObjectsRoom>(grid, new int[1, 2] { { 4, 5 } });        
    }

    // Sets a certain room at the coordinates given.
    private void SetSpecialRoom<T>(IRoom[,] grid, int[,] coordinates)
        where T : IRoom, new()
    {
        for (int i = 0; i < coordinates.GetLength(0); i++)
        {
            IRoom room = new T();
            room.Row = coordinates[i, 0];
            room.Column = coordinates[i, 1];
            grid[room.Row, room.Column] = room;
        }
    }

    private void SetEnemies(IRoom[,] grid)
    {
        if (_worldSize == WorldSize.Small) SetSmallEnemies(grid);
        else if (_worldSize == WorldSize.Medium) SetMediumEnemies(grid);
        else if (_worldSize == WorldSize.Large) SetLargeEnemies(grid);
    }

    private void SetSmallEnemies(IRoom[,] grid)
    {
        SetEnemy<PitEnemy>(grid, new int[1, 2] { { 0, 1 } });
    }

    private void SetMediumEnemies(IRoom[,] grid)
    {
        SetEnemy<PitEnemy>(grid, new int[2, 2] { { 1, 3 }, { 4, 5 } });
        SetEnemy<MaelstromEnemy>(grid, new int[1, 2] { { 3, 3 } });
        SetEnemy<AmarokEnemy>(grid, new int[2, 2] { { 2, 3 }, { 4, 3 } });
    }

    private void SetLargeEnemies(IRoom[,] grid)
    {
        SetEnemy<PitEnemy>(grid, new int[4, 2] { { 1, 3 }, { 2, 5 }, { 1, 2 }, { 2, 7 } });
        SetEnemy<MaelstromEnemy>(grid, new int[2, 2] { { 5, 6 }, { 7, 7 } });
        SetEnemy<AmarokEnemy>(grid, new int[3, 2] { { 3, 3 }, { 5, 5 }, { 6, 7 } });
    }

    private void SetEnemy<T>(IRoom[,] grid, int[,] coordinates)
        where T : IEnemy, new()
    {
        for (int i = 0; i < coordinates.GetLength(0); i++)
        {
            IEnemy enemy = new T();
            enemy.Row = coordinates[i, 0];
            enemy.Column = coordinates[i, 1];

            grid[enemy.Row, enemy.Column].Enemy = enemy;
        }
    }

    public IRoom GetFountainRoom()
    {
        if (_worldSize == WorldSize.Small) return Grid[0, 2];
        if (_worldSize == WorldSize.Medium) return Grid[3, 4];
        return Grid[4, 5];
    }

    public IRoom GetRoom(int Row, int Column) => Grid[Row, Column];
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
                char cellText = ' ';
                ConsoleColor cellColour = ConsoleColor.Gray;

                IRoom currentRoom = Grid[i, j];

                if (currentRoom.PlayerInRoom)
                {
                    cellText = playerSymbol;
                    cellColour = ConsoleColor.Magenta;

                }
                else if (currentRoom.Enemy != null && currentRoom.Enemy.Alive == false)
                {
                    cellText = currentRoom.Enemy.Symbol;
                    
                    // Don't use enemyColour here, the enemy is dead.
                    //cellColour = currentRoom.Enemy.Colour;
                }
                else if (currentRoom.Discovered)
                {
                    cellText = currentRoom.Symbol;
                    cellColour = currentRoom.Colour;
                }

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
        Grid = InitialiseGrid(Size);
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
    public IEnemy? Enemy { get; set; }
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
    public IEnemy? Enemy { get; set; }

    public GenericRoom () 
    {
        Row = 0;
        Column = 0;
        RoomSymbol = ' ';
        Color = ConsoleColor.White;
        InRoomMessage = null;
        AdjacentMessage = null;
        Enemy = null;
    }

    public GenericRoom(int row, int column)
    {
        Row = row;
        Column = column;
        RoomSymbol = ' ';
        Color = ConsoleColor.White;
        InRoomMessage = null;
        AdjacentMessage = null;
        Enemy = null;
    }
}

public class FountainOfObjectsRoom : GenericRoom
{
    public bool Enabled { get; set; } = false;

    public FountainOfObjectsRoom() 
    {
        RoomSymbol = '^';
        Color = ConsoleColor.Blue;
        InRoomMessage = "You hear water dripping in this room. The Fountain of Objects is here!";
    }
}

public class EntranceRoom : GenericRoom
{
    public EntranceRoom() 
    {
        RoomSymbol = 'O';
        Color = ConsoleColor.Yellow;
        InRoomMessage = "You see light coming from the cavern entrance.";
    }
}

public interface IEnemy
{
    public int Row { get; set; }
    public int Column { get; set; }
    public char Symbol { get; set; }
    public bool Alive { get; set; }
    public ICommand? Command { get; set; }
    public ConsoleColor Colour { get; set; }
    public string? InRoomMessage { get; set; }
    public string? AdjacentMessage { get; set; }
}

public class BaseEnemy : IEnemy
{
    public int Row { get; set; }
    public int Column { get; set; }
    public char Symbol { get; set; }
    public bool Alive { get; set; }
    public ICommand? Command { get; set; }
    public ConsoleColor Colour { get; set; }
    public string? InRoomMessage { get; set; }
    public string? AdjacentMessage { get; set; }

    public BaseEnemy()
    {
        Row = 0;
        Column = 0;
        Symbol = ' ';
        Alive = true;
        Command = null;
        Colour = ConsoleColor.Gray;
        InRoomMessage = null;
        AdjacentMessage = null;
    }
}

public class PitEnemy : BaseEnemy
{
    public PitEnemy()
    {
        Symbol = '_';
        Command = new KillPlayerCommand<PitEnemy>();
        Colour = ConsoleColor.Red;
        InRoomMessage = "You fell into a pit and die.";
        AdjacentMessage = "You feel a draft. There is a pit in a nearby room.";
    }
}

public class MaelstromEnemy : BaseEnemy
{
    public MaelstromEnemy()
    {
        Symbol = '@';
        Command = new MaelstromCommand();        
        Colour = ConsoleColor.Red;
        InRoomMessage = "You walked into a maelstrom, and have been swept elsewhere.";
        AdjacentMessage = "You hear the growling and groaning of a maelstrom nearby.";
    }
}

public class AmarokEnemy : BaseEnemy
{
    public AmarokEnemy()
    {
        Command = new KillPlayerCommand<AmarokEnemy>();
        Symbol = '!';
        Colour = ConsoleColor.Red;
        InRoomMessage = "You walked into a group of giant, rotting Amarok wolves and died.";
        AdjacentMessage = "You can smell the rotten stench of an amarok in a nearby room.";
    }
}

public class Player
{
    public int Row { get; set; }
    public int Column { get; set; }
    public char Symbol { get; private set; }
    public bool Alive { get; set; }
    public int Arrows { get; private set; }

    public Player(char symbol)
    {
        Row = 0;
        Column = 0;
        Symbol = symbol;
        Alive = true;
        Arrows = 5;
    }

    public bool Shoot()
    {
        if (Arrows > 0)
        {
            Arrows--;
            return true;
        }
        return false;
    }
}

public interface ICommand
{
    public void Run(FountainGame game);
}

public class DefaultCommand : ICommand
{
    public void Run(FountainGame game) { }
}

public class MaelstromCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        WorldManager worldManager = game.GetWorldManager();
        IRoom currentRoom = worldManager.GetCurrentRoom();

        if (currentRoom.Enemy == null)
            return;

        IEnemy maelstrom = currentRoom.Enemy;

        if (maelstrom.GetType() != typeof(MaelstromEnemy))
            return;
        
        int worldSize = game.GetWorldSize();

        // Move player 1 place north, and two spaces east.
        player.Row -= 1;
        player.Column += 2;

        if (player.Row < 0) player.Row = worldSize + player.Row;
        if (player.Column > worldSize - 1) player.Column = 0 + (player.Column - worldSize);

        // Move maelstrom 1 space south, and two spaces west.
        maelstrom.Row += 1;
        maelstrom.Column -= 2;
        if (maelstrom.Row > worldSize - 1) maelstrom.Row = 0 + (maelstrom.Row - worldSize);
        if (maelstrom.Column < 0) maelstrom.Column = worldSize + maelstrom.Column;

        // Maelstrom has moved coords, so clear the enemy in current room.
        worldManager.GetCurrentRoom().Enemy = null;

        // Now, set maelstrom in new coords.
        worldManager.GetRoom(maelstrom.Row, maelstrom.Column).Enemy = maelstrom;

        game.GetMessageManager().AddMessage(maelstrom.InRoomMessage, maelstrom.Colour);
        worldManager.Update(player);
    }
}

public class KillPlayerCommand<T> : ICommand
    where T : IEnemy
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        IRoom currentRoom = game.GetWorldManager().GetCurrentRoom();

        if (currentRoom.Enemy == null) return;

        IEnemy enemy = currentRoom.Enemy;

        // If current room has enemy
        if (enemy.GetType() != typeof(T))
            return;

        player.Alive = false;
        game.GetMessageManager().AddMessage(enemy.InRoomMessage, enemy.Colour);
    }
}

public class HelpCommand : ICommand
{
    public void Run(FountainGame game)
    {
        ColourConsole.WriteWithColour("help: ", ConsoleColor.Cyan);
        Console.WriteLine("Displays the help menu.");

        ColourConsole.WriteWithColour("move (north, east, south, west) or m(n, e, s, w): ", ConsoleColor.Cyan);
        Console.WriteLine("Move the player along the grid.");

        ColourConsole.WriteWithColour("shoot (north, east, south, west) or s(n, e, s, w): ", ConsoleColor.Cyan);
        Console.WriteLine("Shoot into a room.");

        ColourConsole.WriteWithColour("enable fountain or ef: ", ConsoleColor.Cyan);
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

public class ShootNorthCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        MessageManager messageManager = game.GetMessageManager();

        if (!player.Shoot())
        {
            messageManager.AddMessage("You can no longer shoot.");
            return;
        }

        if (player.Row - 1 < 0)
        {
            messageManager.AddMessage("You shot an arrow into a wall and hit nothing.");
            return;
        }
        
        IRoom northRoom = game.GetWorldManager().GetRoom(player.Row - 1, player.Column);
        if (northRoom.Enemy == null || northRoom.Enemy.Alive == false)
        {
            messageManager.AddMessage("You shot into a room, but nothing happened.");
            return;
        }

        northRoom.Enemy.Alive = false;
        messageManager.AddMessage("You shot into a room and killed an enemy.", ConsoleColor.Green);
    }
}

public class ShootEastCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        MessageManager messageManager = game.GetMessageManager();

        if (!player.Shoot())
        {
            messageManager.AddMessage("You can no longer shoot.");
            return;
        }

        if (player.Column + 1 > game.GetWorldSize() - 1)
        {
            messageManager.AddMessage("You shot an arrow into a wall and hit nothing.");
            return;
        }

        IRoom eastRoom = game.GetWorldManager().GetRoom(player.Row, player.Column + 1);
        if (eastRoom.Enemy == null || eastRoom.Enemy.Alive == false)
        {
            messageManager.AddMessage("You shot into a room, and there was nothing there.");
            return;
        }

        eastRoom.Enemy.Alive = false;
        messageManager.AddMessage("You shot into a room and killed an enemy.", ConsoleColor.Green);
    }
}

public class ShootSouthCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        MessageManager messageManager = game.GetMessageManager();

        if (!player.Shoot())
        {
            messageManager.AddMessage("You can no longer shoot.");
            return;
        }

        if (player.Row + 1 > game.GetWorldSize() - 1)
        {
            messageManager.AddMessage("You shot an arrow into a wall and hit nothing.");
            return;
        }

        IRoom southRoom = game.GetWorldManager().GetRoom(player.Row + 1, player.Column);
        if (southRoom.Enemy == null || southRoom.Enemy.Alive == false)
        {
            messageManager.AddMessage("You shot into a room, and there was nothing there.");
            return;
        }

        southRoom.Enemy.Alive = false;
        messageManager.AddMessage("You shot into a room and killed an enemy.", ConsoleColor.Green);
    }
}

public class ShootWestCommand : ICommand
{
    public void Run(FountainGame game)
    {
        Player player = game.GetPlayer();
        MessageManager messageManager = game.GetMessageManager();

        if (!player.Shoot())
        {
            messageManager.AddMessage("You can no longer shoot.");
            return;
        }

        if (player.Column - 1 < 0)
        {
            messageManager.AddMessage("You shot an arrow into a wall and hit nothing.");
            return;
        }

        IRoom westRoom = game.GetWorldManager().GetRoom(player.Row, player.Column - 1);
        if (westRoom.Enemy == null || westRoom.Enemy.Alive == false)
        {
            messageManager.AddMessage("You shot into a room, and there was nothing there.");
            return;
        }

        westRoom.Enemy.Alive = false;
        messageManager.AddMessage("You shot into a room and killed an enemy.", ConsoleColor.Green);
    }
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