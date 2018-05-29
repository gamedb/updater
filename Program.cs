using System;
using System.IO;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        private const string LastChangeFile = "last-changenumber.txt";

        private static void Main(string[] args)
        {
            //var steam = new Steam();

            if (File.Exists(LastChangeFile))
            {
                //steam.PreviousChangeNumber = uint.Parse(File.ReadAllText("last-changenumber.txt"));
                Console.WriteLine("file");
            }
            else
            {
                Console.WriteLine("no file");
            }

            File.WriteAllText(LastChangeFile, "123");

            //steam.Connect();

            //Console.WriteLine(steam.PreviousChangeNumber);
        }
    }
}
