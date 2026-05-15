using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

[Route("api/[controller]")]
[ApiController]
public class TransactionController : ControllerBase
{
    // ===================================================
    // 1. RELATIONAL JOINS READ & SEARCH
    // ===================================================
    [HttpGet("search")]
    public IActionResult SearchLedger([FromQuery] string term = "")
    {
        try
        {
            DataTable dt = new DataTable();
            DatabaseConnection db = new DatabaseConnection();
            
            using (MySqlConnection conn = db.GetConnection())
            {
                // Multi-Table SQL mapping fetching explicit relational details
                string sql = @"
                    SELECT 
                        o.order_id, 
                        CONCAT(c.first_name, ' ', c.last_name) AS customer_name,
                        p.product_name, 
                        od.quantity, 
                        pay.amount_paid, 
                        pay.payment_method, 
                        DATE_FORMAT(pay.payment_date, '%Y-%m-%d') AS payment_date
                    FROM payments pay
                    INNER JOIN orders o ON pay.order_id = o.order_id
                    INNER JOIN customers c ON o.customer_id = c.customer_id
                    INNER JOIN orderdetails od ON o.order_id = od.order_id
                    INNER JOIN products p ON od.product_id = p.product_id
                    WHERE c.first_name LIKE @term 
                       OR c.last_name LIKE @term 
                       OR p.product_name LIKE @term 
                       OR pay.payment_method LIKE @term
                    ORDER BY pay.payment_id DESC";

                MySqlCommand cmd = new MySqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@term", "%" + term + "%");
                
                MySqlDataAdapter adapter = new MySqlDataAdapter(cmd);
                conn.Open();
                adapter.Fill(dt);
            }

            var results = new List<Dictionary<string, object>>();
            foreach (DataRow row in dt.Rows)
            {
                var map = new Dictionary<string, object>();
                foreach (DataColumn col in dt.Columns)
                {
                    map.Add(col.ColumnName, row[col]);
                }
                results.Add(map);
            }

            return Ok(results);
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Relational JOIN fault: " + ex.Message });
        }
    }

    // ===================================================
    // 2. RETRIEVE MASTER SYSTEM INDEXES
    // ===================================================
    [HttpGet("indexes")]
    public IActionResult GetSystemIndexes()
    {
        try
        {
            var customers = new List<Dictionary<string, object>>();
            var products = new List<Dictionary<string, object>>();
            
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                // Load available customers array
                using (MySqlCommand cmd = new MySqlCommand("SELECT customer_id, first_name, last_name FROM customers ORDER BY first_name ASC", conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        customers.Add(new Dictionary<string, object> {
                            { "customer_id", reader.GetInt32("customer_id") },
                            { "first_name", reader.GetString("first_name") },
                            { "last_name", reader.GetString("last_name") }
                        });
                    }
                }

                // Load available inventory catalog
                using (MySqlCommand cmd = new MySqlCommand("SELECT product_id, product_name, price, stock_quantity FROM products ORDER BY product_name ASC", conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        products.Add(new Dictionary<string, object> {
                            { "product_id", reader.GetInt32("product_id") },
                            { "product_name", reader.GetString("product_name") },
                            { "price", reader.GetDecimal("price") },
                            { "stock_quantity", reader.GetInt32("stock_quantity") }
                        });
                    }
                }
            }

            return Ok(new { customers = customers, products = products });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Indexes query dropped: " + ex.Message });
        }
    }

    // ===================================================
    // 3. PROCESS MULTI-TABLE WRITE PIPELINE
    // ===================================================
    [HttpPost("process")]
    public IActionResult ExecuteOrderCheckout([FromBody] MultiTableCheckoutRequest req)
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                // 1. Verify item bounds and pricing structures
                string checkStockSql = "SELECT price, stock_quantity FROM products WHERE product_id = @pid";
                MySqlCommand checkCmd = new MySqlCommand(checkStockSql, conn);
                checkCmd.Parameters.AddWithValue("@pid", req.ProductId);

                decimal unitPrice = 0;
                int remainingStock = 0;

                using (MySqlDataReader reader = checkCmd.ExecuteReader())
                {
                    if (!reader.Read()) return NotFound(new { message = "Item missing from parent inventory records." });
                    unitPrice = reader.GetDecimal("price");
                    remainingStock = reader.GetInt32("stock_quantity");
                }

                if (remainingStock < req.Quantity)
                {
                    return BadRequest(new { message = "Insufficient dynamic stock allocations available." });
                }

                decimal totalPayable = unitPrice * req.Quantity;

                // 2. Commit formal stock balance decrements
                string decSql = "UPDATE products SET stock_quantity = stock_quantity - @qty WHERE product_id = @pid";
                MySqlCommand decCmd = new MySqlCommand(decSql, conn);
                decCmd.Parameters.AddWithValue("@qty", req.Quantity);
                decCmd.Parameters.AddWithValue("@pid", req.ProductId);
                decCmd.ExecuteNonQuery();

                // 3. Commit absolute master parent entry into `orders` table
                string insertOrderSql = "INSERT INTO orders (customer_id, order_date) VALUES (@cid, NOW()); SELECT LAST_INSERT_ID();";
                MySqlCommand orderCmd = new MySqlCommand(insertOrderSql, conn);
                orderCmd.Parameters.AddWithValue("@cid", req.CustomerId);
                int generatedOrderId = System.Convert.ToInt32(orderCmd.ExecuteScalar());

                // 4. Map bound relational line items into `orderdetails` table
                string insertDetailsSql = "INSERT INTO orderdetails (order_id, product_id, quantity, unit_price) VALUES (@oid, @pid, @qty, @price)";
                MySqlCommand detailsCmd = new MySqlCommand(insertDetailsSql, conn);
                detailsCmd.Parameters.AddWithValue("@oid", generatedOrderId);
                detailsCmd.Parameters.AddWithValue("@pid", req.ProductId);
                detailsCmd.Parameters.AddWithValue("@qty", req.Quantity);
                detailsCmd.Parameters.AddWithValue("@price", unitPrice);
                detailsCmd.ExecuteNonQuery();

                // 5. Finalize formal financial settlement ledger inside `payments` table
                string insertPaySql = "INSERT INTO payments (order_id, payment_date, amount_paid, payment_method) VALUES (@oid, CURDATE(), @amt, @method)";
                MySqlCommand payCmd = new MySqlCommand(insertPaySql, conn);
                payCmd.Parameters.AddWithValue("@oid", generatedOrderId);
                payCmd.Parameters.AddWithValue("@amt", totalPayable);
                payCmd.Parameters.AddWithValue("@method", req.PaymentMethod);
                payCmd.ExecuteNonQuery();

                return Ok(new { message = "Relational checkout pipeline executed securely across multi-table array." });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Pipeline write failure: " + ex.Message });
        }
    }
}

// Map structural request mapping models safely
public class MultiTableCheckoutRequest
{
    public int CustomerId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}