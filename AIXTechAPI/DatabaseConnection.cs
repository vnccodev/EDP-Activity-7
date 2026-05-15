using System;
using MySql.Data.MySqlClient;

public class DatabaseConnection
{
    private readonly string connectionString = "Server=127.0.0.1;Database=invsales;Uid=root;Pwd=;";

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(connectionString);
    }
}