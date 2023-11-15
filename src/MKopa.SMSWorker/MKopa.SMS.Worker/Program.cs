namespace MKopa.SMS.Worker;

public class Program
{
    //Base main method for the executable for the rest of the team to implement
    public static async Task<int> Main(string[] args)
    {
        //Stop throwing warnings on build
        await Task.Delay(TimeSpan.FromSeconds(0));
        throw new NotImplementedException("Executable has not been implemented yet!");
    }
}


