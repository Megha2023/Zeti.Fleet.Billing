using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Net.Http.Json;
using Google.Protobuf.WellKnownTypes;
using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Services;
using Azure;

namespace Zeti.Fleet.Billing.UnitTests
{
    [TestFixture]
    public class BillingServiceTests
    {
        private Mock<ILogger<BillingService>> _loggerMock;
        private Mock<IOptions<BillingOptions>> _optionsMock;
        private Mock<HttpMessageHandler> _httpMessageHandlerMock;
        private Mock<HttpClient> _httpClientMock;
        private BillingService _billingService;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<BillingService>>();
            _optionsMock = new Mock<IOptions<BillingOptions>>();
            _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

           
            _optionsMock.Setup(o => o.Value).Returns(new BillingOptions
            {
                CostPerMile = 0.5m,
                readingUrl = "https://example.com/api/odometer/"
            });
            _httpClientMock = new Mock<HttpClient>();
           
            _billingService = new BillingService(_httpClientMock.Object, _loggerMock.Object, _optionsMock.Object);
        }
        
        [Test]
        public async Task CalculateBillAsync_ShouldReturnCorrectBill_WhenOdometerReadingsAreValid()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "CBDH 789" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            var odometerResponsesStart = new List<OdometerResponse>
            {
                new OdometerResponse
                {
                    LicensePlate = "CBDH 789",
                    State = new VehicleState { OdometerInMeters = 16093.4m } // 10 miles
                }
            };
            var odometerResponsesEnd = new List<OdometerResponse>
            {
                new OdometerResponse
                {
                    LicensePlate = "CBDH 789",
                    State = new VehicleState { OdometerInMeters = 32186.8m } // 20 miles
                }
            };
            var mockHttpMessageHandler = new MockHttpMessageHandler(request =>
            {
                if (request.RequestUri.ToString() == $"{_optionsMock.Object.Value.readingUrl}{billingRequest.StartDate:o}")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(odometerResponsesStart)
                    };
                }

                if (request.RequestUri.ToString() == $"{_optionsMock.Object.Value.readingUrl}{billingRequest.EndDate:o}")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(odometerResponsesEnd)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var httpClient = new HttpClient(mockHttpMessageHandler);
            _billingService = new BillingService(httpClient, _loggerMock.Object, _optionsMock.Object);

            // Act
            var result = await _billingService.CalculateBillAsync(billingRequest);

            // Assert
            result.Should().Be(5.0m); // (20 - 10) miles * 0.5 cost per mile
        }
        [Test]
        public async Task CalculateBillAsync_ShouldReturnCorrectBill_ForMultipleVehicles()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "CBDH 789", "86532 AZE" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            var odometerResponsesStart = new List<OdometerResponse>
            {
                new OdometerResponse
                {
                    LicensePlate = "CBDH 789",
                    State = new VehicleState { OdometerInMeters = 16093.4m } // 10 miles
                },
                new OdometerResponse
                {
                    LicensePlate = "86532 AZE",
                    State = new VehicleState { OdometerInMeters = 32186.8m } // 20 miles
                }
            };

            var odometerResponsesEnd = new List<OdometerResponse>
            {
                new OdometerResponse
                {
                    LicensePlate = "CBDH 789",
                    State = new VehicleState { OdometerInMeters = 32186.8m } // 20 miles
                },
                new OdometerResponse
                {
                    LicensePlate = "86532 AZE",
                    State = new VehicleState { OdometerInMeters = 48280.2m } // 30 miles
                }
            };

            var mockHttpMessageHandler = new MockHttpMessageHandler(request =>
            {
                if (request.RequestUri.ToString() == $"{_optionsMock.Object.Value.readingUrl}{billingRequest.StartDate:o}")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(odometerResponsesStart)
                    };
                }

                if (request.RequestUri.ToString() == $"{_optionsMock.Object.Value.readingUrl}{billingRequest.EndDate:o}")
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = JsonContent.Create(odometerResponsesEnd)
                    };
                }

                return new HttpResponseMessage(HttpStatusCode.NotFound);
            });

            var httpClient = new HttpClient(mockHttpMessageHandler);
            _billingService = new BillingService(httpClient, _loggerMock.Object, _optionsMock.Object);

            // Act
            var result = await _billingService.CalculateBillAsync(billingRequest);

            // Assert
            result.Should().Be(10.0m); // (20 - 10) + (30 - 20) miles * 0.5 cost per mile
        }
        [Test]
        public async Task CalculateBillAsync_ShouldReturnZero_WhenVehicleNotFoundInOdometerData()
        {
            // Arrange
            var billingRequest = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "UNKNOWN_VEHICLE" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            var odometerResponses = new List<OdometerResponse>
            {
                new OdometerResponse
                {
                    LicensePlate = "CBDH 789",
                    State = new VehicleState { OdometerInMeters = 16093.4m } // 10 miles
                }
            };

            var mockHttpMessageHandler = new MockHttpMessageHandler(request =>
            {
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = JsonContent.Create(odometerResponses)
                };
            });

            var httpClient = new HttpClient(mockHttpMessageHandler);
            _billingService = new BillingService(httpClient, _loggerMock.Object, _optionsMock.Object);

            // Act
            var result = await _billingService.CalculateBillAsync(billingRequest);

            // Assert
            result.Should().Be(0.0m);
        }

    }
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _sendAsync;

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> sendAsync)
        {
            _sendAsync = sendAsync;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_sendAsync(request));
        }
    }
}

