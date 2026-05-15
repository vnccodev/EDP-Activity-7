using System;
using MySql.Data.MySqlClient;

public class DatabaseConnection
{
    // Default XAMPP credentials: root user, no password, localhost
    private readonly string connectionString = "Server=127.0.0.1;Database=invsales;Uid=root;Pwd=;";

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }

    public bool TestConnection()
    {
        using (MySqlConnection conn = GetConnection())
        {
            try
            {
                conn.Open();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database Connection Error: " + ex.Message);
                return false;
            }
        }
    }
}