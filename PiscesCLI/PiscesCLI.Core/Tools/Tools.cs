namespace EverbloomingLab.PiscesCLI.Core
{
    public static class Tools
    {
        public static bool GetConfirmation(string message)
        {
            Console.WriteLine("Please confirm infos below to continue... Enter 'y' to confirm, 'n' to deny.");
            Console.WriteLine(message);
            var input = Console.ReadLine();
            return input?.ToLower() == "y";
        }
    }
}