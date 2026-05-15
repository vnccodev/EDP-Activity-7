using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient; // Required for raw database token queries

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly UserManager _userManager = new UserManager();

    // ===================================================
    // 1. EXISTING LOGIN ENDPOINT
    // ===================================================
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        try {
            bool success = _userManager.AuthenticateUser(request.Username, request.Password);
            if (success) return Ok(new { message = "Login successful" });
            return Unauthorized(new { message = "Invalid credentials or inactive account" });
        }
        catch (System.Exception ex) {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ===================================================
    // 2. FORGOT PASSWORD (GENERATE TOKEN)
    // ===================================================
    [HttpPost("forgot-password")]
    public IActionResult ForgotPassword([FromBody] ForgotRequest model)
    {
        try
        {
            // Connects directly via your existing backend database configuration
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                
                // 1. Verify the email exists in your system records
                string checkQuery = "SELECT user_id FROM users WHERE email = @email";
                MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn);
                checkCmd.Parameters.AddWithValue("@email", model.Email);
                
                object result = checkCmd.ExecuteScalar();
                if (result == null)
                {
                    return NotFound(new { message = "Email address not found in system records." });
                }

                // 2. Generate a secure token and a 15-minute expiration timestamp
                string token = System.Guid.NewGuid().ToString();
                string expiry = System.DateTime.Now.AddMinutes(15).ToString("yyyy-MM-dd HH:mm:ss");

                // 3. Commit the token to the user's database row
                string updateQuery = "UPDATE users SET reset_token = @token, reset_expiry = @expiry WHERE email = @email";
                MySqlCommand updateCmd = new MySqlCommand(updateQuery, conn);
                updateCmd.Parameters.AddWithValue("@token", token);
                updateCmd.Parameters.AddWithValue("@expiry", expiry);
                updateCmd.Parameters.AddWithValue("@email", model.Email);
                updateCmd.ExecuteNonQuery();

                // 4. Generate the local test link (defaults to standard VS Code Live Server port 5500)
                string resetUrl = $"http://localhost:5500/reset.html?token={token}";

                return Ok(new { 
                    message = "Recovery token generated successfully.",
                    resetLink = resetUrl 
                });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Token generation fault: " + ex.Message });
        }
    }

    // ===================================================
    // 3. RESET PASSWORD (EXECUTE UPDATE)
    // ===================================================
    [HttpPost("reset-password")]
    public IActionResult ResetPassword([FromBody] ResetRequest model)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                
                // 1. Validate the token exists and has not expired
                string validateQuery = "SELECT email FROM users WHERE reset_token = @token AND reset_expiry > NOW()";
                MySqlCommand validateCmd = new MySqlCommand(validateQuery, conn);
                validateCmd.Parameters.AddWithValue("@token", model.Token);
                
                object emailObj = validateCmd.ExecuteScalar();
                if (emailObj == null)
                {
                    return BadRequest(new { message = "Invalid or expired recovery token. Please request a new link." });
                }

                string targetEmail = emailObj.ToString();

                // 2. Commit the new password and instantly wipe the token so it cannot be reused
                string commitQuery = "UPDATE users SET password = @newPass, reset_token = NULL, reset_expiry = NULL WHERE email = @email";
                MySqlCommand commitCmd = new MySqlCommand(commitQuery, conn);
                commitCmd.Parameters.AddWithValue("@newPass", model.NewPassword);
                commitCmd.Parameters.AddWithValue("@email", targetEmail);
                commitCmd.ExecuteNonQuery();

                return Ok(new { message = "Password updated securely." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Database reset fault: " + ex.Message });
        }
    }
}

// ===================================================
// DATA REQUEST MODELS
// ===================================================
public class LoginRequest 
{ 
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class ForgotRequest 
{ 
    public string Email { get; set; } = string.Empty; 
}

public class ResetRequest 
{ 
    public string Token { get; set; } = string.Empty; 
    public string NewPassword { get; set; } = string.Empty; 
}