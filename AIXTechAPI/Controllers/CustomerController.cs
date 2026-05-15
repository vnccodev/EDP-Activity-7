using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

[Route("api/[controller]")]
[ApiController]
public class CustomerController : ControllerBase
{
    // ===================================================
    // 1. READ & SEARCH DIRECTORY
    // ===================================================
    [HttpGet("search")]
    public IActionResult SearchCustomers([FromQuery] string term = "")
    {
        try
        {
            DataTable dt = new DataTable();
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                string sql = "SELECT customer_id, first_name, last_name, email, phone, address FROM customers WHERE first_name LIKE @term OR last_name LIKE @term OR email LIKE @term ORDER BY customer_id ASC";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                conn.Open();
                adapter.Fill(dt);
            }

            var clients = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var c = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    c.Add(col.ColumnName, row[col]);
                }
                clients.Add(c);
            }
            return Ok(clients);
        }
        catch (System.Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ===================================================
    // 2. CREATE CLIENT
    // ===================================================
    [HttpPost("add")]
    public IActionResult AddCustomer([FromBody] CustomerCreateRequest req)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string sql = "INSERT INTO customers (first_name, last_name, email, phone, address) VALUES (@first, @last, @email, @phone, @addr)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@first", req.FirstName);
                cmd.Parameters.AddWithValue("@last", req.LastName);
                cmd.Parameters.AddWithValue("@email", req.Email);
                cmd.Parameters.AddWithValue("@phone", req.Phone);
                cmd.Parameters.AddWithValue("@addr", req.Address);
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Client successfully committed to records directory." });
            }
        }
        catch (System.Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ===================================================
    // 3. UPDATE CLIENT
    // ===================================================
    [HttpPut("update")]
    public IActionResult UpdateCustomer([FromBody] CustomerUpdateRequest req)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE customers SET first_name=@first, last_name=@last, email=@email, phone=@phone, address=@addr WHERE customer_id=@id";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@first", req.FirstName);
                cmd.Parameters.AddWithValue("@last", req.LastName);
                cmd.Parameters.AddWithValue("@email", req.Email);
                cmd.Parameters.AddWithValue("@phone", req.Phone);
                cmd.Parameters.AddWithValue("@addr", req.Address);
                cmd.Parameters.AddWithValue("@id", req.CustomerId);
                
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) return Ok(new { message = "Client master parameters mapped cleanly." });
                return NotFound(new { message = "Target Customer ID unresolvable." });
            }
        }
        catch (System.Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    // ===================================================
    // 4. DELETE CLIENT
    // ===================================================
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteCustomer(int id)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM customers WHERE customer_id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) return Ok(new { message = "Client profile completely purged." });
                return NotFound(new { message = "Target ID not mapped." });
            }
        }
        catch (System.Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }
}

// Request Data Validation Object Models
public class CustomerCreateRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class CustomerUpdateRequest
{
    public int CustomerId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}