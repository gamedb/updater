using System;
using System.IO;
using System.Threading.Tasks;

namespace SteamProxy
{
    internal static class ChangeFetcher
    {
        public const string LastChangeFile = "last-changenumber.txt";

        private static void Main(string[] args)
        {
            var steam = new Steam();

            if (File.Exists(LastChangeFile))
            {
                //steam.PreviousChangeNumber = uint.Parse(File.ReadAllText("last-changenumber.txt"));
                Console.WriteLine("file");
            }
            else
            {
                Console.WriteLine("no file");
            }

            steam.Start();

            Console.WriteLine(steam.PreviousChangeNumber);
        }
    }
}
