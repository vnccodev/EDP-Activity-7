using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient; // CRITICAL: Required for raw MySQL connectivity

[Route("api/[controller]")]
[ApiController]
public class OverviewController : ControllerBase
{
    [HttpGet("metrics")]
    public IActionResult GetMasterOverview()
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                // 1. EXTRACT AGGREGATE SYSTEM COUNTS
                int totalUsers = 0;
                int totalProducts = 0;
                decimal grossRevenue = 0;
                int criticalStock = 0;

                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM users", conn))
                {
                    totalUsers = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM products", conn))
                {
                    totalProducts = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                using (MySqlCommand cmd = new MySqlCommand("SELECT SUM(amount_paid) FROM payments", conn))
                {
                    object res = cmd.ExecuteScalar();
                    if (res != System.DBNull.Value && res != null) grossRevenue = System.Convert.ToDecimal(res);
                }

                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM products WHERE stock_quantity <= 5", conn))
                {
                    criticalStock = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2. EXTRACT RECENT SETTLEMENT FEED (Multi-Table Relational JOIN)
                var recentTransactions = new List<Dictionary<string, object>>();
                string feedSql = @"
                    SELECT 
                        pay.order_id, 
                        CONCAT(c.first_name, ' ', c.last_name) AS customer_name, 
                        pay.amount_paid, 
                        pay.payment_method, 
                        DATE_FORMAT(pay.payment_date, '%Y-%m-%d') AS payment_date
                    FROM payments pay
                    INNER JOIN orders o ON pay.order_id = o.order_id
                    INNER JOIN customers c ON o.customer_id = c.customer_id
                    ORDER BY pay.payment_id DESC 
                    LIMIT 5";

                using (MySqlCommand cmd = new MySqlCommand(feedSql, conn))
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);

                    foreach (DataRow row in dt.Rows)
                    {
                        var tx = new Dictionary<string, object>();
                        foreach (DataColumn col in dt.Columns)
                        {
                            tx.Add(col.ColumnName, row[col]);
                        }
                        recentTransactions.Add(tx);
                    }
                }

                // Transmit unified structural status dictionaries securely
                return Ok(new {
                    metrics = new {
                        totalUsers = totalUsers,
                        totalProducts = totalProducts,
                        grossRevenue = grossRevenue,
                        criticalStock = criticalStock
                    },
                    recentTransactions = recentTransactions
                });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Overview parsing handler fault: " + ex.Message });
        }
    }
}