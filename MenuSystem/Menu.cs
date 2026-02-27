namespace MenuSystem;

public class Menu
{
    private string Title { get; set; } = default!;
    private Dictionary<string, MenuItem> MenuItems { get; set; } = new();

    private EMenuLevel Level { get; set; }

    public void AddMenuItem(string key, string value, Func<string> methodToRun)
    {
        if (MenuItems.ContainsKey(key))
        {
            throw new ArgumentException($"Menu item with key '{key}' already exists.");
        }
        
        MenuItems[key] = new MenuItem() {Key = key, Value = value, MethodToRun = methodToRun};
    }
    
    public Menu(string title, EMenuLevel level)
    {
        Title = title;
        Level = level;

        switch (level)
        {
            case EMenuLevel.Root:
                MenuItems["X"] = new MenuItem() {Key = "X", Value = "Exit"};
                break;
            case EMenuLevel.First:
                MenuItems["M"] = new MenuItem() {Key = "M", Value = "Return to Main Menu"};
                MenuItems["X"] = new MenuItem() {Key = "X", Value = "Exit"};
                break;
            case EMenuLevel.Deep:
                MenuItems["B"] = new MenuItem() {Key = "B", Value = "Back to previous Menu"};
                MenuItems["M"] = new MenuItem() {Key = "M", Value = "Return to Main Menu"};
                MenuItems["X"] = new MenuItem() {Key = "X", Value = "Exit"};
                break;
        }
    }
    
    public string Run()
    {
        Console.Clear();
        var menuRunning = true;
        var userChoice = "";
        
        do
        {
            DisplayMenu();
            Console.Write("Select an option: ");

            var input = Console.ReadLine();
            if (input == null)
            {
                Console.WriteLine("Invalid input. Please try again.");
                continue;
            }

            userChoice = input.Trim().ToUpper();
            
            if ( userChoice== "X" || userChoice == "M" || userChoice == "B")
            {
                menuRunning = false;
            }
            else
            {
                if (MenuItems.ContainsKey(userChoice))
                {
                    var returnValueFromMethodToRun = MenuItems[userChoice].MethodToRun?.Invoke();

                    if (returnValueFromMethodToRun == "X")
                    {
                        menuRunning = false;
                        userChoice = "X";
                    }
                    else if (returnValueFromMethodToRun == "M" && Level != EMenuLevel.Root)
                    {
                        menuRunning = false;
                        userChoice = "M";
                    }
                }
                else
                {
                    Console.WriteLine("Invalid option. Please try again.");
                }
            }

            Console.WriteLine();
        } while (menuRunning);

        return userChoice; 
    }

    private void DisplayMenu()
    {
        Console.Clear();
        Console.WriteLine(Title);
        Console.WriteLine("--------------------");
        if (Title == "Rules")
        {
            Console.WriteLine("Connect4 is a two-player game where you take turns dropping pieces into columns."); 
            Console.WriteLine("The goal is to connect four in a row - horizontally, vertically, or diagonally.");
            Console.WriteLine("This version can be cylindrical - lines can wrap around the board edges. The first to ");
            Console.WriteLine("connect four wins, or the game is a draw if the board fills.");
        }
        
        // makes sure exit, return to main and back are on the bottom of the menu
        var orderedItems = MenuItems.Values
            .OrderBy(item => (item.Key == "B" || item.Key == "M" || item.Key == "X") ? 1 : 0)
            .ThenBy(item => item.Key);

        foreach (var item in orderedItems)
        {
            Console.WriteLine(item);
        }
    }
}