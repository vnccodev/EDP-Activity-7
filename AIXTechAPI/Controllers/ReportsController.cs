using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Collections.Generic;
using MySql.Data.MySqlClient;

[Route("api/[controller]")]
[ApiController]
public class ReportController : ControllerBase
{
    [HttpGet("summary")]
    public IActionResult GetAnalyticsSummary()
    {
        try
        {
            DatabaseConnection db = new DatabaseConnection();
            using (MySqlConnection conn = db.GetConnection())
            {
                conn.Open();

                // 1. CALCULATE MASTER METRICS SUMMARIES
                decimal totalRev = 0;
                int totalUnits = 0;
                int lowStockCount = 0;

                using (MySqlCommand cmd = new MySqlCommand("SELECT SUM(amount_paid) FROM payments", conn))
                {
                    object res = cmd.ExecuteScalar();
                    if (res != System.DBNull.Value && res != null) totalRev = System.Convert.ToDecimal(res);
                }

                using (MySqlCommand cmd = new MySqlCommand("SELECT SUM(quantity) FROM orderdetails", conn))
                {
                    object res = cmd.ExecuteScalar();
                    if (res != System.DBNull.Value && res != null) totalUnits = System.Convert.ToInt32(res);
                }

                using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM products WHERE stock_quantity <= 5", conn))
                {
                    lowStockCount = System.Convert.ToInt32(cmd.ExecuteScalar());
                }

                // 2. EXTRACT REVENUE BY METHOD GROUPINGS
                var revByMethod = new List<object>();
                string revSql = @"
                    SELECT payment_method, COUNT(*) as tx_count, SUM(amount_paid) as total_amount 
                    FROM payments 
                    GROUP BY payment_method 
                    ORDER BY total_amount DESC";

                using (MySqlCommand cmd = new MySqlCommand(revSql, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        revByMethod.Add(new {
                            method = reader.GetString("payment_method"),
                            txCount = reader.GetInt32("tx_count"),
                            totalAmount = reader.GetDecimal("total_amount")
                        });
                    }
                }

                // 3. EXTRACT LOW STOCK ITEMS (Deficiency threshold <= 5)
                var lowStockList = new List<object>();
                string stockSql = "SELECT product_id, product_name, category, stock_quantity FROM products WHERE stock_quantity <= 5 ORDER BY stock_quantity ASC";

                using (MySqlCommand cmd = new MySqlCommand(stockSql, conn))
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lowStockList.Add(new {
                            id = reader.GetInt32("product_id"),
                            name = reader.GetString("product_name"),
                            category = reader.GetString("category"),
                            stock = reader.GetInt32("stock_quantity")
                        });
                    }
                }

                // Package and transmit absolute runtime metric objects safely
                return Ok(new {
                    metrics = new {
                        totalRevenue = totalRev,
                        totalUnitsSold = totalUnits,
                        criticalStockCount = lowStockCount
                    },
                    revenueByMethod = revByMethod,
                    lowStockItems = lowStockList
                });
            }
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, new { message = "Analytics parsing runtime exception: " + ex.Message });
        }
    }
}