using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
using System.Data;
using Microsoft.Data.SqlClient;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly string _connectionString;

        public OrdersController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("OrderServiceCS");
        }

        [HttpPost]
        public IActionResult CreateOrder([FromBody] Order order)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("CreateOrder", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            command.Parameters.AddWithValue("@ProductId", order.ProductId);
            command.Parameters.AddWithValue("@Quantity", order.Quantity);

            var statusParam = new SqlParameter("@Status", SqlDbType.NVarChar, 50)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(statusParam);

            var orderIdParam = new SqlParameter("@OrderId", SqlDbType.Int)
            {
                Direction = ParameterDirection.Output
            };
            command.Parameters.Add(orderIdParam);

            command.ExecuteNonQuery();

            // Check the status returned from the stored procedure
            string status = (string)statusParam.Value;
            if (status != "Created")
            {
                return BadRequest(status); // Return a 400 Bad Request with the status message
            }

            order.OrderId = (int)orderIdParam.Value;
            order.Status = status;

            return CreatedAtAction(nameof(GetOrderById), new { id = order.OrderId }, order);
        }



        [HttpGet("{id}")]
        public IActionResult GetOrderById(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("GetOrderById", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            using var reader = command.ExecuteReader();
            if (!reader.Read())
            {
                return NotFound();
            }

            var order = new Order
            {
                OrderId = reader.GetInt32(0),
                ProductId = reader.GetInt32(1),
                Quantity = reader.GetInt32(2),
                Status = reader.GetString(3)
            };

            return Ok(order);
        }

        [HttpGet]
        public IActionResult GetAllOrders()
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("GetAllOrders", connection)
            {
                CommandType = CommandType.StoredProcedure
            };

            using var reader = command.ExecuteReader();

            var orders = new List<Order>();
            while (reader.Read())
            {
                orders.Add(new Order
                {
                    OrderId = reader.GetInt32(0),
                    ProductId = reader.GetInt32(1),
                    Quantity = reader.GetInt32(2),
                    Status = reader.GetString(3)
                });
            }

            return Ok(orders);
        }

        [HttpDelete("{id}")]
        public IActionResult CancelOrder(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            var command = new SqlCommand("CancelOrder", connection)
            {
                CommandType = CommandType.StoredProcedure
            };
            command.Parameters.AddWithValue("@Id", id);

            try
            {
                var rowsAffected = command.ExecuteNonQuery();
                return NoContent(); // If rowsAffected > 0, return NoContent
            }
            catch (SqlException ex)
            {
                // Check if the error message indicates that the order was not found
                if (ex.Message.Contains("ID not found"))
                {
                    return NotFound(ex.Message); // Return NotFound with the error message
                }
                return BadRequest(ex.Message); // Handle other SQL exceptions
            }
        }
    }
}