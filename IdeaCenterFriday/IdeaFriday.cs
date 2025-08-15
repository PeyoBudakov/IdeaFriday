
using IdeaCenterFriday.Models;
using Newtonsoft.Json;
using RestSharp;
using RestSharp.Authenticators;
using System.Net;
using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace IdeaCenterFriday
{
    public class IdeaFriday
    {
        private RestClient client;
        private const string BASEURL = "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:84";
        private const string EMAIL = "peyopeyo@example.com";
        private const string PASSWORD = "peyopeyo";

        private static string lastIdeaId;

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken = GetJwtToken(EMAIL, PASSWORD);

            var options = new RestClientOptions(BASEURL)
            {
                Authenticator = new JwtAuthenticator(jwtToken)
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            RestClient authClient = new RestClient(BASEURL);
            var request = new RestRequest("/api/User/Authentication");
            request.AddJsonBody(new
            {
                email,
                password
            });

            var response = authClient.Execute(request, Method.Post);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();
                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Access Token is null or empty");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected response type {response.StatusCode} with data {response.Content}");
            }
        }

        [OneTimeTearDown]
        public void TearDown() { client.Dispose(); }



        [Test, Order(1)]
        public void CreateANewIdea_WithTheRequiredFields_ShouldSucceed()
        {
            var newIdea = new IdeaDTO
            {
                Title = "New Idea",
                Description = "New Idea Description",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(newIdea);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(responseData.Msg, Is.EqualTo("Successfully created!"));
        }

        [Test, Order(2)]
        public void GetAllIdeas_ShouldSucceed()
        {
            var request = new RestRequest("/api/Idea/All", Method.Get);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseDataArray = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);
            //Assert.That(responseDataArray, Is.Not.Empty);
            Assert.That(responseDataArray.Length, Is.GreaterThan(0));

            lastIdeaId= responseDataArray[responseDataArray.Length-1].ideaId;
            Console.WriteLine(lastIdeaId);
        }

        [Test, Order(3)]
        public void EditEheLastIdeaYouCreated_ShouldSucceed()
        {
            var editIdea = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "Edited Idea Description",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddJsonBody(editIdea);
            request.AddQueryParameter("ideaID",lastIdeaId);
            var response = this.client.Execute(request);


            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            var responseData = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(responseData.Msg, Is.EqualTo("Edited successfully"));
        }

        [Test, Order(4)]
        public void DeleteTheIdeaThatYouEdited_ShouldSucceed()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaID", lastIdeaId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The idea is deleted!"));
        }

        [Test, Order(5)]
        public void CreateAnIdea_WithoutTheRequiredFields_ShouldFail()
        {
            var newIdea = new IdeaDTO
            {
                Title = "",
                Description = "",
                Url = ""
            };

            var request = new RestRequest("/api/Idea/Create", Method.Post);
            request.AddJsonBody(newIdea);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Test, Order(6)]
        public void EditNonExistingIdea_ShouldFial()
        {
            var editIdea = new IdeaDTO
            {
                Title = "Edited Idea",
                Description = "Edited Idea Description",
                Url = ""
            };
            var request = new RestRequest("/api/Idea/Edit", Method.Put);
            request.AddJsonBody(editIdea);
            request.AddQueryParameter("ideaID", "666");
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));

        }

        [Test, Order(7)]
        public void DeleteNonExistingIdea_ShouldFail()
        {
            var request = new RestRequest("/api/Idea/Delete", Method.Delete);
            request.AddQueryParameter("ideaID", "666");
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such idea!"));
        }


    }
}