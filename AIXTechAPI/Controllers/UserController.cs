using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient; // CRITICAL: Required for raw MySQL parameter commands

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly UserManager _userManager = new UserManager();

    // ===================================================
    // 1. READ (SEARCH)
    // ===================================================
    [HttpGet("search")]
    public IActionResult SearchUsers([FromQuery] string term = "")
    {
        try {
            DataTable dt = _userManager.SearchAccounts(term);
            var users = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows) {
                var user = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns) user.Add(col.ColumnName, row[col]);
                users.Add(user);
            }
            return Ok(users);
        }
        catch (System.Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ===================================================
    // 2. CREATE (ADD)
    // ===================================================
    [HttpPost("add")]
    public IActionResult AddUser([FromBody] UserCreateRequest request)
    {
        try {
            bool success = _userManager.AddAccount(request.Username, request.Email, request.Password);
            if (success) return Ok(new { message = "User added successfully" });
            return BadRequest(new { message = "Failed to add user" });
        }
        catch (System.Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ===================================================
    // 3. UPDATE (EDIT)
    // ===================================================
    [HttpPut("update")]
    public IActionResult UpdateUser([FromBody] UserUpdateRequest request)
    {
        try {
            bool success = _userManager.UpdateFullAccount(request.UserId, request.Username, request.Email, request.Role, request.IsActive);
            if (success) return Ok(new { message = "User updated successfully" });
            return BadRequest(new { message = "Failed to update user" });
        }
        catch (System.Exception ex) { return BadRequest(new { message = ex.Message }); }
    }

    // ===================================================
    // 4. DELETE (PURGE)
    // ===================================================
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteUser(int id)
    {
        try
        {
            // Cleanly instantiates your local database connection class
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                
                // Parameter binding (@id) prevents SQL injection
                string query = "DELETE FROM users WHERE user_id = @id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", id);

                int rowsAffected = cmd.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    return Ok(new { message = "Record successfully purged from the system." });
                }
                
                return NotFound(new { message = "Target user ID not found in the database." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Database deletion fault: " + ex.Message });
        }
    }
}

// ===================================================
// DATA REQUEST MODELS
// ===================================================
public class UserCreateRequest 
{ 
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UserUpdateRequest
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}