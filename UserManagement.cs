using System;
using System.Data;
using MySql.Data.MySqlClient;

public class UserManager
{
    private readonly DatabaseConnection _db;

    public UserManager()
    {
        _db = new DatabaseConnection();
    }

    // 1. User Authentication (Login)
    public bool AuthenticateUser(string username, string password)
    {
        using (MySqlConnection conn = _db.GetConnection())
        {
            string query = "SELECT password_hash, is_active FROM users WHERE username = @user";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", username);

            conn.Open();
            using (MySqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    bool isActive = Convert.ToBoolean(reader["is_active"]);
                    if (!isActive) throw new Exception("Account is deactivated.");

                    string dbPassword = reader["password_hash"].ToString();
                    // Implement BCrypt.Verify(password, dbPassword) here
                    return password == dbPassword; 
                }
            }
        }
        return false;
    }

    // 2. User Management: Add Account (Registration)
    public bool AddAccount(string username, string email, string password)
    {
        using (MySqlConnection conn = _db.GetConnection())
        {
            string query = "INSERT INTO users (username, email, password_hash) VALUES (@user, @email, @pass)";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@user", username);
            cmd.Parameters.AddWithValue("@email", email);
            cmd.Parameters.AddWithValue("@pass", password); // Hash before inserting

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    // 3. User Management: Update Account Profile
    public bool UpdateAccount(int userId, string newEmail, string newUsername)
    {
        using (MySqlConnection conn = _db.GetConnection())
        {
            string query = "UPDATE users SET email = @email, username = @user WHERE user_id = @id";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@email", newEmail);
            cmd.Parameters.AddWithValue("@user", newUsername);
            cmd.Parameters.AddWithValue("@id", userId);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    // 4. User Management: Set Active / Inactive Account
    public bool SetAccountStatus(int userId, bool isActive)
    {
        using (MySqlConnection conn = _db.GetConnection())
        {
            string query = "UPDATE users SET is_active = @status WHERE user_id = @id";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@status", isActive);
            cmd.Parameters.AddWithValue("@id", userId);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }

    // 5. User Management: Account List / Search
    public DataTable SearchAccounts(string searchTerm = "")
    {
        DataTable dt = new DataTable();
        using (MySqlConnection conn = _db.GetConnection())
        {
            string query = "SELECT user_id, username, email, role, is_active, created_at FROM users WHERE username LIKE @search OR email LIKE @search";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@search", "%" + searchTerm + "%");

            MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
            conn.Open();
            adapter.Fill(dt);
        }
        return dt;
    }

    // 6. Password Recovery (Reset)
    public bool RecoverPassword(string email, string newPassword)
    {
        using (MySqlConnection conn = _db.GetConnection())
        {
            // First verify if email exists, then update
            string query = "UPDATE users SET password_hash = @newPass WHERE email = @email";
            MySqlCommand cmd = new MySqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@newPass", newPassword); // Hash this
            cmd.Parameters.AddWithValue("@email", email);

            conn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}