using System;
using System.IO;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        private static void Main(string[] args)
        {
            var steam = new Steam();

            if (File.Exists("last-changenumber.txt"))
            {
                steam.PreviousChangeNumber = uint.Parse(File.ReadAllText("last-changenumber.txt"));
            }
            
            //steam.Connect();

            Console.WriteLine(steam.PreviousChangeNumber);
        }
    }
}
