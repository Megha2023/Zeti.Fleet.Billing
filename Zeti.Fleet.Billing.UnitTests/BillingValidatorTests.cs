using FluentValidation.TestHelper;
using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Validator;

namespace Zeti.Fleet.Billing.UnitTests
{
    [TestFixture]
    public class BillingValidatorTests
    {
        private BillingValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new BillingValidator();
        }

        [Test]
        public void Validate_ShouldPass_WhenAllFieldsAreValid()
        {
            // Arrange
            var request = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "vehicle1", "vehicle2" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_ShouldFail_WhenCustomerIsMissing()
        {
            // Arrange
            var request = new BillingRequest
            {
                Customer = "",
                Vehicles = new List<string> { "vehicle1", "vehicle2" },
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Customer)
                .WithErrorMessage("Customer name is required.");
        }

        [Test]
        public void Validate_ShouldFail_WhenVehiclesListIsEmpty()
        {
            // Arrange
            var request = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string>(),
                StartDate = DateTime.UtcNow.AddDays(-7),
                EndDate = DateTime.UtcNow
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Vehicles)
                .WithErrorMessage("Vehicles list is required.");
        }

        [Test]
        public void Validate_ShouldFail_WhenStartDateIsLaterThanEndDate()
        {
            // Arrange
            var request = new BillingRequest
            {
                Customer = "Bob's Taxis",
                Vehicles = new List<string> { "vehicle1", "vehicle2" },
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-7)
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .WithErrorMessage("Start date must be earlier than end date.");
        }

        [Test]
        public void Validate_ShouldFail_WhenAllFieldsAreInvalid()
        {
            // Arrange
            var request = new BillingRequest
            {
                Customer = "",
                Vehicles = new List<string>(),
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(-7)
            };

            // Act
            var result = _validator.TestValidate(request);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Customer)
                .WithErrorMessage("Customer name is required.");
            result.ShouldHaveValidationErrorFor(x => x.Vehicles)
                .WithErrorMessage("Vehicles list is required.");
            result.ShouldHaveValidationErrorFor(x => x.StartDate)
                .WithErrorMessage("Start date must be earlier than end date.");
        }
    }
}