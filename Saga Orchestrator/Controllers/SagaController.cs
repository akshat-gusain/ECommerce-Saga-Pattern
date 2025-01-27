using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderService.Models;
namespace SagaOrchestrator.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SagaController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        public SagaController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        [HttpPost]
        public async Task<IActionResult> ProcessOrder([FromBody] Order order)
        {
            try
            {
                var sagaResult = new
                {
                    Step1 = new { Status = "", Message = "" },
                    Step2 = new { Status = "", Message = "" },
                    Step3 = new { Status = "", Message = "" },
                    FinalStatus = ""
                };

                // Step 1: Create Order
                var orderResponse = await CreateOrder(order);
                if (!orderResponse.IsSuccessStatusCode)
                {
                    var error = await orderResponse.Content.ReadAsStringAsync();
                    sagaResult = new
                    {
                        Step1 = new { Status = "Failed", Message = $"Order creation failed. Details: {error}" },
                        Step2 = new { Status = "Skipped", Message = "Inventory reservation not attempted." },
                        Step3 = new { Status = "Skipped", Message = "Payment processing not attempted." },
                        FinalStatus = "Failed"
                    };
                    return BadRequest(sagaResult);
                }

                var orderResult = await orderResponse.Content.ReadFromJsonAsync<Order>();
                order.OrderId = orderResult.OrderId; // Assume response contains OrderId
                sagaResult = sagaResult with { Step1 = new { Status = "Success", Message = $"Order created successfully with ID {order.OrderId}" } };

                // Step 2: Reserve Inventory
                var inventoryResponse = await ReserveInventory(order);
                if (!inventoryResponse.IsSuccessStatusCode)
                {
                    await CancelOrder(order.OrderId);
                    var error = await inventoryResponse.Content.ReadAsStringAsync();
                    sagaResult = sagaResult with
                    {
                        Step2 = new { Status = "Failed", Message = $"Inventory reservation failed. Details: {error}" },
                        Step3 = new { Status = "Skipped", Message = "Payment processing not attempted." },
                        FinalStatus = "Failed"
                    };
                    return BadRequest(sagaResult);
                }

                sagaResult = sagaResult with { Step2 = new { Status = "Success", Message = "Inventory reserved successfully." } };

                // Step 3: Process Payment
                var paymentResponse = await ProcessPayment(order);
                if (!paymentResponse.IsSuccessStatusCode)
                {
                    await ReleaseInventory(order.OrderId);
                    await CancelOrder(order.OrderId);
                    var error = await paymentResponse.Content.ReadAsStringAsync();
                    sagaResult = sagaResult with
                    {
                        Step3 = new { Status = "Failed", Message = $"Payment processing failed. Details: {error}" },
                        FinalStatus = "Failed"
                    };
                    return BadRequest(sagaResult);
                }

                sagaResult = sagaResult with { Step3 = new { Status = "Success", Message = "Payment processed successfully." } };

                // Saga completed successfully
                order.Status = "Completed";
                sagaResult = sagaResult with { FinalStatus = "Completed" };

                return Ok(new
                {
                    sagaResult.Step1,
                    sagaResult.Step2,
                    sagaResult.Step3,
                    FinalStatus = sagaResult.FinalStatus
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Step = "Saga",
                    Status = "Failed",
                    Error = $"An unexpected error occurred: {ex.Message}"
                });
            }
        }

        private async Task<HttpResponseMessage> CreateOrder(Order order)
        {
            var client = _httpClientFactory.CreateClient();
            return await client.PostAsJsonAsync("https://localhost:7165/api/orders", order);
        }
        private async Task<HttpResponseMessage> ReserveInventory(Order order)
        {
            var client = _httpClientFactory.CreateClient();
            return await client.PostAsJsonAsync("https://localhost:7155/api/inventory/reserve", new { order.OrderId});
        }
        private async Task<HttpResponseMessage> ProcessPayment(Order order)
        {
            var client = _httpClientFactory.CreateClient();

            var paymentRequest = new { order.OrderId, Amount = order.Quantity * 10 };
            Console.WriteLine($"Sending payment request: {JsonSerializer.Serialize(paymentRequest)}");

            var response = await client.PostAsJsonAsync("https://localhost:7211/api/payments", paymentRequest);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Payment API failed. Response: {response.StatusCode}, Content: {errorContent}");
            }

            return response;
        }
        private async Task<HttpResponseMessage> CancelOrder(int orderId)
        {
            var client = _httpClientFactory.CreateClient();
            return await client.DeleteAsync($"https://localhost:7165/api/orders/{orderId}");
        }
        private async Task<HttpResponseMessage> ReleaseInventory(int orderId)
        {
            var client = _httpClientFactory.CreateClient();
            return await client.PostAsJsonAsync("https://localhost:7155/api/inventory/release", new {orderId });
        }
    }
}

