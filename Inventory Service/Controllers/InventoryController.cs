using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using System.Data;
using Microsoft.Data.SqlClient;
using InventoryService.Models;
using System.Text.Json;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InventoryController : ControllerBase
    {
        private readonly string _connectionString;

        public InventoryController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("InventoryServiceCS");
        }



        [HttpPost("reserve")]
        public IActionResult ReserveInventory([FromBody] Inventory request)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("ReserveInventory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            //command.Parameters.AddWithValue("@ProductId", request.ProductId);
            command.Parameters.AddWithValue("@OrderId", request.OrderId); // Pass OrderId to the stored procedure

            try
            {
                var returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();

                int result = (int)returnParameter.Value;
                if (result == 1)
                {
                    return Ok("Inventory reserved successfully.");
                }
                else if (result == -1)
                {
                    return NotFound($"ID not found.");
                }
                else if (result == -2)
                {
                    return BadRequest("Insufficient stock.");
                }
                else
                {
                    return StatusCode(500, "An error occurred while reserving inventory.");
                }
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("release")]
        public IActionResult ReleaseInventory([FromBody] Inventory request)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("ReleaseInventory", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            //command.Parameters.AddWithValue("@ProductId", request.ProductId);
            command.Parameters.AddWithValue("@OrderId", request.OrderId); // Pass OrderId to the stored procedure

            try
            {
                // Execute the command and get the return value
                var returnParameter = command.Parameters.Add("@ReturnValue", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                command.ExecuteNonQuery();

                // Check the return value
                int result = (int)returnParameter.Value;
                if (result == 1)
                {
                    return Ok("Stock released successfully.");
                }
                else if (result == -1)
                {
                    return NotFound($"ID not found.");
                }
                else if (result == -2)
                {
                    return NotFound($"ID not found.");
                }
                else
                {
                    return StatusCode(500, "An error occurred while releasing inventory.");
                }
            }
            catch (SqlException ex)
            {
                // Log the exception message for debugging
                Console.WriteLine($"SQL Exception: {ex.Message}");
                return BadRequest(ex.Message);
            }
        }

        [HttpPost("add")]
        public IActionResult AddStock([FromBody] JsonElement request)
        {
            int productId = request.GetProperty("productId").GetInt32();
            int quantity = request.GetProperty("quantity").GetInt32();
            decimal price = request.GetProperty("price").GetDecimal(); // Add price property

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Ensure the SQL query is correct
            var command = new SqlCommand("AddStock", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@Price", price); // Add price parameter

            int rowsAffected = command.ExecuteNonQuery();

            if (rowsAffected == 0)
            {
                return NotFound($"ID not found.");
            }

            return Ok("Stock added successfully.");
        }

        [HttpGet]
        public IActionResult GetInventory()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("GetInventory", connection) { CommandType = CommandType.StoredProcedure };
            using var reader = command.ExecuteReader();

            var inventories = new List<object>(); // Use object to hold anonymous types
            while (reader.Read())
            {
                inventories.Add(new
                {
                    ProductId = reader.GetInt32(0),
                    Stock = reader.GetInt32(1),
                    Price = reader.GetDecimal(2)
                });
            }
            return Ok(inventories);
        }


        [HttpDelete("delete/{productId}")]
        public IActionResult DeleteStock(int productId)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("DeleteStock", connection) { CommandType = CommandType.StoredProcedure };
            command.Parameters.AddWithValue("@ProductId", productId);

            try
            {
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected == 0)
                {
                    return NotFound($"ID not found.");
                }

                return Ok("Stock deleted successfully.");
            }
            catch (SqlException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}