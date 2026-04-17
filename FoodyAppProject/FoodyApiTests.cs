using System;
using System.Net;
using System.Text.Json;
using NUnit.Framework.Constraints;
using RestSharp;
using RestSharp.Authenticators;
using FoodyAppProject.Models;

namespace FoodyAppProject
{
    [TestFixture]
    public class Tests
    {
        private RestClient client;
        private static string lastCreatedFoodId;

        private const string BaseUrl = "http://144.91.123.158:81";
        private const string LoginUsername = "VessNikolova";
        private const string LoginPassword = "123456";

        [OneTimeSetUp]
        public void Setup()
        {
            // Always get fresh token (avoids expiration issues)
            string jwtToken = GetJwtToken(LoginUsername, LoginPassword);

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string username, string password)
        {
            var tempClient = new RestClient(BaseUrl);

            var request = new RestRequest("/api/User/Authentication", Method.Post);

            // FIX: correct property names
            request.AddJsonBody(new { username, password });

            var response = tempClient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);

                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Token not found in the response.");
                }

                return token;
            }
            else
            {
                throw new InvalidOperationException(
                    $"Failed to authenticate. Status code: {response.StatusCode}, Response: {response.Content}");
            }
        }

        

        [Order(1)]
        [Test]
        public void CreateNewFood_WithRequiredFields_ShouldReturnSuccess()
        {
            //Creates new object of type Food
            FoodDTO foodData = new FoodDTO
            {
                Name = "Tiramisu",
                Description = "This is an italian dessert.",
                Url = "",
            };
            //Creates ab HTTP request for creating the food
            var request = new RestRequest("/api/Food/Create", Method.Post);
            //Attaches the Food as a JSON request body
            request.AddJsonBody(foodData);

            //Sends the request to the server
            var response = this.client.Execute(request);
            //Takes the response body as JSON string and converts it into a C# object(API Response DTO)
            ApiResponseDTO createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created), "Expected status code 201 Created.");
            Assert.That(response.Content.Contains("foodId"));

            lastCreatedFoodId = createResponse.FoodId;
                
        }

        [Order(2)]
        [Test]
        public void EditTitle_WithExistingFood_ShouldReturnSuccess()
        {


            var request = new RestRequest($"/api/Food/Edit/{lastCreatedFoodId}", Method.Patch);
            request.AddBody(new[]
            {
                new
                {
                    path = "/name",
                    op = "replace",
                    value = "Edited Food"
                }
            });

            var response = this.client.Execute(request);
            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(editResponse.Msg, Is.EqualTo("Successfully edited"));

        }

        [Order(3)]
        [Test]
        public void GetAllFoods_ShouldReturnSuccess()
        {

            var request = new RestRequest("/api/Food/All", Method.Get);
            var response = this.client.Execute(request);

            //Get all items in array
            var responseItem = JsonSerializer.Deserialize<List<FoodDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), "Expected status code 200 OK.");
            Assert.That(responseItem, Is.Not.Null);
            Assert.That(responseItem, Is.Not.Empty);
            Assert.That(responseItem.Count, Is.GreaterThanOrEqualTo(1));

        }

        [Order(4)]
        [Test]
        public void DeleteExistingFood_ShouldReturnSuccess()
        {
            RestRequest request = new RestRequest($"/api/Food/Delete/{lastCreatedFoodId}", Method.Delete);
            RestResponse response = client.Execute(request);
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            ApiResponseDTO readyResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(readyResponse.Msg, Is.EqualTo("Deleted successfully!"));
        }

        [Order(5)]
        [Test]
        public void CreateNewFood_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var food = new FoodDTO
            {
                Name = "",
                Description = "",
                Url = "",
            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = this.client.Execute(request);
            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), "Expected status code 400 Bad Request.");
           
        }

        [Order(6)]
        [Test]
        public void EditNonExistingFood_ShouldReturnNotFound()
        {
            var nonExitingFoodId = "99999999";
            var editedFood = new FoodDTO
            {
                Name = "Edited Food",
                Description = "This is an edited description.",
                Url = "",
            };

            var request = new RestRequest($"/api/Idea/Edit/{nonExitingFoodId}", Method.Put);
           
            request.AddJsonBody(editedFood);

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found.");

        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingFood_ShouldReturnNotFound()
        {
            var nonExitingFoodId = "99999999";

            var request = new RestRequest($"/api/Food/Delete/{nonExitingFoodId}", Method.Delete);
          

            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), "Expected status code 404 Not Found.");
         
        }

        [OneTimeTearDown]
        public void TearDown()
        {
            this.client?.Dispose();
        }
    }
}
