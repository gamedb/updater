using System;
using MySql.Data.MySqlClient;

namespace SteamUpdater
{
    public class MySQL
    {
        static void xx()
        {
            MySqlConnection con = new MySqlConnection("server=localhost;user id=root;database=projectmysql");
            con.Open();
            //MySqlCommand cmd = new MySqlCommand("INSERT INTO projectmysql (`name`, `address`, `phone`, `email`) VALUES ('" + txtname.Text + "','" + txtadd.Text + "','" + txtphone.Text + "','" + txtemail.Text + "')", con);
            //cmd.ExecuteNonQuery();
            Console.WriteLine("successful");
            con.Close();
        }
    }
}
