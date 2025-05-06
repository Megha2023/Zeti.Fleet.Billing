using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Zeti.Fleet.Billing.Command;
using Zeti.Fleet.Billing.Model;
using Zeti.Fleet.Billing.Services;

namespace Zeti.Fleet.Billing;

public class BillingFunction
{
    private readonly ILogger<BillingFunction> _logger;
    private readonly IValidator<BillingRequest?> _validator;
    private readonly BillFormatterFactory _formatterFactory;
    private readonly IBillingService _billService;

    public BillingFunction(
        ILogger<BillingFunction> logger,
        IBillingService billService,
        IValidator<BillingRequest?> validator,
        BillFormatterFactory formatterFactory)
    {
        _logger = logger;
        _billService = billService;
        _validator = validator;
        _formatterFactory = formatterFactory;
    }

    [Function("bill-customer-05052025")]
    public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
    {

        string body;
        using (var reader = new StreamReader(req.Body))
        {
            body = await reader.ReadToEndAsync();
        }

        if (string.IsNullOrWhiteSpace(body))
        {
            _logger.LogWarning("Request body is empty.");
            return new BadRequestObjectResult("Request body is empty.");
        }

        BillingRequest? request;
        try
        {
            request = JsonSerializer.Deserialize<BillingRequest>(body, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true // Allows case-insensitive property matching
            });
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize request body.");
            return new BadRequestObjectResult("Invalid JSON format in request body.");
        }

        if (request == null)
        {
            _logger.LogWarning("Request body is null.");
            return new BadRequestObjectResult("Invalid request.");
        }

        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = string.Join("; ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Validation failed: {Errors}", errors);
            return new BadRequestObjectResult(errors);
        }

        var command = new BillingCommand(_billService, request);
        var billAmount = await command.ExecuteAsync();

        var acceptHeader =  "application/json";
        //var formatter = _formatterFactory.GetFormatter(acceptHeader);
        //var result = formatter.Format(request.Customer, billAmount);
        //tried to use formatter but it's returning response with escape characters , something to do with double json serialization
        var result = new { Customer = request.Customer, Amount = billAmount };
        return new OkObjectResult(result);
    }
}
