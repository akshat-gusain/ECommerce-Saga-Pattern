using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PaymentService.Models;
using System.Collections.Generic;
using System.Data;
using System.Text.Json;

namespace PaymentService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly string _connectionString;

        public PaymentsController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("PaymentServiceCS");
        }

        // POST: api/payments
        [HttpPost]
        public IActionResult ProcessPayment([FromBody] JsonElement request)
        {
            if (!request.TryGetProperty("orderId", out var orderIdElement))
            {
                return BadRequest("Invalid request format.");
            }

            int orderId = orderIdElement.GetInt32();
            string status;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("ProcessPayment", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@OrderId", orderId);

                var statusParam = new SqlParameter("@Status", SqlDbType.NVarChar, 50)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(statusParam);

                command.ExecuteNonQuery();
                status = (string)statusParam.Value;
            }

            // Check the status returned from the stored procedure
            if (status == "Order not found")
            {
                return NotFound("The specified order ID does not exist.");
            }
            else if (status == "Product not found in inventory")
            {
                return NotFound("The product associated with the order is not found in inventory.");
            }

            return Ok(new { orderId, status });
        }

        // GET: api/payments/{id}
        [HttpGet("{id}")]
        public IActionResult GetPaymentById(int id)
        {
            Payment payment = null;

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("GetPaymentById", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@Id", id);

                using (var reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        payment = new Payment
                        {
                            PaymentId = reader.GetInt32(0),
                            OrderId = reader.GetInt32(1),
                            Price = reader.GetDecimal(2), // Price of the product
                            Quantity = reader.GetInt32(3), // Quantity ordered
                            Amount = reader.GetDecimal(4), // Total amount calculated
                            Status = reader.GetString(5)
                        };
                    }
                }
            }

            if (payment == null)
            {
                return NotFound();
            }

            return Ok(payment);
        }

        // GET: api/payments
        [HttpGet]
        public IActionResult GetAllPayments()
        {
            List<Payment> payments = new List<Payment>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("GetAllPayments", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        payments.Add(new Payment
                        {
                            PaymentId = reader.GetInt32(0),
                            OrderId = reader.GetInt32(1),
                            Price = reader.GetDecimal(2), // Price of the product
                            Quantity = reader.GetInt32(3), // Quantity ordered
                            Amount = reader.GetDecimal(4), // Total amount calculated
                            Status = reader.GetString(5)
                        });
                    }
                }
            }

            return Ok(payments);
        }

        // DELETE: api/payments/{paymentId}
        [HttpDelete("{paymentId}")]
        public IActionResult DeletePayment(int paymentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var command = new SqlCommand("DeletePayment", connection)
                {
                    CommandType = CommandType.StoredProcedure
                };
                command.Parameters.AddWithValue("@PaymentId", paymentId);

                // Add an output parameter to capture the deletion status
                var isDeletedParam = new SqlParameter("@IsDeleted", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(isDeletedParam);

                // Execute the command
                command.ExecuteNonQuery();

                // Check the output parameter to determine if the payment was deleted
                bool isDeleted = (bool)isDeletedParam.Value;

                if (!isDeleted)
                {
                    return NotFound($"Payment ID not found.");
                }
            }

            return NoContent(); // Return 204 No Content on successful deletion
        }
    }
}