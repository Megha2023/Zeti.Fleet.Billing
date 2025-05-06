using System.Net;
using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Zeti.Fleet.Billing;
using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Services;

namespace Zeti.Fleet.Billing.UnitTests
{
    [TestFixture]
    public class BillingFunctionTests
    {
        private Mock<ILogger<BillingFunction>> _loggerMock;
        private Mock<IBillingService> _billingServiceMock;
        private Mock<IValidator<BillingRequest>> _validatorMock;
        private Mock<BillFormatterFactory> _formatterFactoryMock;
        private BillingFunction _billingFunction;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<BillingFunction>>();
            _billingServiceMock = new Mock<IBillingService>();
            _validatorMock = new Mock<IValidator<BillingRequest>>();
            _formatterFactoryMock = new Mock<BillFormatterFactory>(new List<IBillFormatter>());

            _billingFunction = new BillingFunction(
                _loggerMock.Object,
                _billingServiceMock.Object,
                _validatorMock.Object,
                _formatterFactoryMock.Object
            );
        }

        [Test]
        public async Task RunAsync_ShouldReturnBadRequest_WhenRequestBodyIsEmpty()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            var emptyStream = new MemoryStream();
            request.Setup(r => r.Body).Returns(emptyStream);

            // Act
            var response = await _billingFunction.RunAsync(request.Object);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();

        }
        [Test]
        public async Task RunAsync_ShouldReturnOk_WhenRequestBodyIsValid()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            var validJson = JsonSerializer.Serialize(new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "vehicle1" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            });
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(validJson);
            writer.Flush();
            stream.Position = 0; 
            request.Setup(r => r.Body).Returns(stream);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<BillingRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult());

            _billingServiceMock
                .Setup(s => s.CalculateBillAsync(It.IsAny<BillingRequest>()))
                .ReturnsAsync(100.0m);

            // Act
            var response = await _billingFunction.RunAsync(request.Object);

            // Assert
            response.Should().BeOfType<OkObjectResult>();
            var okResult = response as OkObjectResult;
            okResult!.Value.Should().BeEquivalentTo(new { Customer = "Bob's Taxis", Amount = 100.0m });
        }
        [Test]
        public async Task RunAsync_ShouldReturnBadRequest_WhenRequiredFieldsAreMissing()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            var invalidJson = JsonSerializer.Serialize(new BillingRequest
            {
                Customer = null, // Missing required field
                Vehicles = null, // Missing required field
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            });
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(invalidJson);
            writer.Flush();
            stream.Position = 0;
            request.Setup(r => r.Body).Returns(stream);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<BillingRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
                {
                    new FluentValidation.Results.ValidationFailure("Customer", "Customer is required."),
                    new FluentValidation.Results.ValidationFailure("Vehicles", "Vehicles are required.")
                }));

            // Act
            var response = await _billingFunction.RunAsync(request.Object);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = response as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Customer is required.; Vehicles are required.");
        }
        [Test]
        public async Task RunAsync_ShouldReturnBadRequest_WhenDatesAreWrong()
        {
            // Arrange
            var request = new Mock<HttpRequest>();
            var invalidJson = JsonSerializer.Serialize(new BillingRequest
            {
                Customer = "customer1", 
                Vehicles = new List<string> { "vehicle1" },
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-7)
            });
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(invalidJson);
            writer.Flush();
            stream.Position = 0;
            request.Setup(r => r.Body).Returns(stream);

            _validatorMock
                .Setup(v => v.ValidateAsync(It.IsAny<BillingRequest>(), default))
                .ReturnsAsync(new FluentValidation.Results.ValidationResult(new[]
                {
                    new FluentValidation.Results.ValidationFailure("StartDate", "Start date must be earlier than end date.")
                }));

            // Act
            var response = await _billingFunction.RunAsync(request.Object);

            // Assert
            response.Should().BeOfType<BadRequestObjectResult>();
            var badRequestResult = response as BadRequestObjectResult;
            badRequestResult!.Value.Should().Be("Start date must be earlier than end date.");
        }

    }
}