// Custom class to store the catch date of a fish in the player's inventory
public class CatchData
{
    private string location;
    private System.DateTime date;
    public string Location { get {return location;} }
    public System.DateTime Date { get {return date;} }
    // Start is called before the first frame update
    public CatchData(string loc)
    {
        location = loc;
        date = System.DateTime.Now;
    }

    public new string ToString()
    {
        return $"Caught at {location} at {date}";
    }
}
