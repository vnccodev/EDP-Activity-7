using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

[Route("api/[controller]")]
[ApiController]
public class ProductController : ControllerBase
{
    // ===================================================
    // 1. READ & SEARCH ENDPOINT (DataTable Pattern)
    // ===================================================
    [HttpGet("search")]
    public IActionResult SearchProducts([FromQuery] string term = "")
    {
        try
        {
            DataTable dt = new DataTable();
            DatabaseConnection db = new DatabaseConnection();
            
            using (MySqlConnection conn = db.GetConnection())
            {
                string query = "SELECT product_id, product_name, category, price, stock_quantity FROM products WHERE product_name LIKE @term OR category LIKE @term ORDER BY product_id ASC";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                conn.Open();
                adapter.Fill(dt); // Captures rows cleanly into local memory
            }

            var products = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var prod = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    prod.Add(col.ColumnName, row[col]);
                }
                products.Add(prod);
            }

            return Ok(products);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Database query fault: " + ex.Message });
        }
    }

    // ===================================================
    // 2. CREATE ENDPOINT
    // ===================================================
    [HttpPost("add")]
    public IActionResult AddProduct([FromBody] ProductCreateRequest req)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string sql = "INSERT INTO products (product_name, category, price, stock_quantity) VALUES (@name, @cat, @price, @stock)";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", req.ProductName);
                cmd.Parameters.AddWithValue("@cat", req.Category);
                cmd.Parameters.AddWithValue("@price", req.Price);
                cmd.Parameters.AddWithValue("@stock", req.StockQuantity);
                cmd.ExecuteNonQuery();
                return Ok(new { message = "Inventory item committed successfully." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Insertion fault: " + ex.Message });
        }
    }

    // ===================================================
    // 3. UPDATE ENDPOINT
    // ===================================================
    [HttpPut("update")]
    public IActionResult UpdateProduct([FromBody] ProductUpdateRequest req)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                string sql = "UPDATE products SET product_name=@name, category=@cat, price=@price, stock_quantity=@stock WHERE product_id=@id";
                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@name", req.ProductName);
                cmd.Parameters.AddWithValue("@cat", req.Category);
                cmd.Parameters.AddWithValue("@price", req.Price);
                cmd.Parameters.AddWithValue("@stock", req.StockQuantity);
                cmd.Parameters.AddWithValue("@id", req.ProductId);
                
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) return Ok(new { message = "Item profile updated cleanly." });
                return NotFound(new { message = "Target product ID unresolvable." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Update engine failure: " + ex.Message });
        }
    }

    // ===================================================
    // 4. DELETE ENDPOINT
    // ===================================================
    [HttpDelete("delete/{id}")]
    public IActionResult DeleteProduct(int id)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM products WHERE product_id=@id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                int rows = cmd.ExecuteNonQuery();
                if (rows > 0) return Ok(new { message = "Item successfully wiped." });
                return NotFound(new { message = "Target ID not mapped." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Deletion fault: " + ex.Message });
        }
    }
}

// Data Mapping Models
public class ProductCreateRequest 
{ 
    public string ProductName { get; set; } = string.Empty; 
    public string Category { get; set; } = string.Empty; 
    public decimal Price { get; set; } 
    public int StockQuantity { get; set; } 
}

public class ProductUpdateRequest 
{ 
    public int ProductId { get; set; } 
    public string ProductName { get; set; } = string.Empty; 
    public string Category { get; set; } = string.Empty; 
    public decimal Price { get; set; } 
    public int StockQuantity { get; set; } 
}