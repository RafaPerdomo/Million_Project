using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using properties.Controllers.Examples;
using Swashbuckle.AspNetCore.Annotations;
using Swashbuckle.AspNetCore.Filters;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Properties.Domain.Exceptions;
using properties.Api.Application.Commands.Propertys.SellProperty;
using properties.Api.Application.Commands.Propertys.UpdateProperty;
using properties.Api.Application.Commands.Propertys.CreateProperty;
using properties.Api.Application.Commands.Propertys.CreatePropertyImages;
using properties.Api.Application.Commands.Propertys.DeletePropertyImage;
using properties.Api.Application.Queries.Propertys.ListProperties;
using properties.Api.Application.Queries.Propertys.GetPropertyImage;
using Microsoft.AspNetCore.Authorization;
using properties.Api.Application.Queries.Propertys.GetPropertyById;
using properties.Api.Application.Common.DTOs;
namespace properties.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    [Authorize]

    public class PropertiesController : ControllerBase
    {
        private readonly IMediator _mediator;
        public PropertiesController(IMediator mediator)
        {
            _mediator = mediator;
        }
        [HttpPut("{id}")]
        [SwaggerOperation(Summary = "Update an existing property")]
        [SwaggerResponse(StatusCodes.Status200OK, "Property updated successfully", typeof(ResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Error in the provided data")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Property not found")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Property with same code already exists")]
        [SwaggerRequestExample(typeof(UpdatePropertyCommand), typeof(UpdatePropertyCommandExample))]
        public async Task<IActionResult> UpdateProperty(
            [FromRoute] int id,
            [FromBody] UpdatePropertyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                command.Id = id;
                var result = await _mediator.Send(command, cancellationToken);
                return Ok(result);
            }
            catch (PropertyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (PropertyAlreadyExistsException ex)
            {
                return Conflict(ex.Message);
            }
            catch (InvalidPropertyOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the property");
            }
        }
        [HttpPost]
        [SwaggerOperation(Summary = "Create a new property")]
        [SwaggerResponse(StatusCodes.Status201Created, "Property created successfully", typeof(ResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Error in the provided data")]
        [SwaggerResponse(StatusCodes.Status409Conflict, "Property with same code already exists")]
        [SwaggerRequestExample(typeof(CreatePropertyCommand), typeof(CreatePropertyCommandExample))]
        public async Task<IActionResult> CreateProperty(
            [FromBody] CreatePropertyCommand command,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _mediator.Send(command, cancellationToken);
                return StatusCode(StatusCodes.Status201Created, result);
            }
            catch (PropertyAlreadyExistsException ex)
            {
                return Conflict(ex.Message);
            }
            catch (InvalidPropertyOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the property");
            }
        }
        [HttpGet("{id}")]
        [SwaggerOperation(Summary = "Get a property by ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Property retrieved successfully", typeof(ResponseDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Property not found")]
        public async Task<IActionResult> GetPropertyById(
            [FromRoute] int id,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var query = new GetPropertyByIdQuery { Id = id };
                var result = await _mediator.Send(query, cancellationToken);
                return Ok(result);
            }
            catch (PropertyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (InvalidPropertyOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the property");
            }
        }

        [HttpGet]
        [SwaggerOperation(Summary = "Get the list of properties")]
        [SwaggerResponse(StatusCodes.Status200OK, "Property list retrieved successfully", typeof(ResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Error in the provided data")]
        public async Task<IActionResult> GetProperties(
            [FromQuery] ListPropertiesQuery query,
            CancellationToken cancellationToken = default)
        {
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        [HttpGet("images/{id}")]
        [SwaggerOperation(Summary = "Get property image details by ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Image retrieved successfully", typeof(PropertyImageDetailDto))]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Image not found")]
        public async Task<IActionResult> GetPropertyImage(
            [FromRoute] int id,
            CancellationToken cancellationToken = default)
        {
            var query = new GetPropertyImageQuery { ImageId = id };
            var result = await _mediator.Send(query, cancellationToken);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        [HttpPost("{id}/images")]
        [Consumes("multipart/form-data")]
        [SwaggerOperation(Summary = "Upload multiple images for a property")]
        [SwaggerResponse(StatusCodes.Status200OK, "Images uploaded successfully", typeof(ResponseDto))]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Error in the provided data")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Property not found")]
        public async Task<IActionResult> AddPropertyImages(
            [FromRoute] int id,
            [FromForm] List<IFormFile> images,
            CancellationToken cancellationToken = default)
        {
            var command = new CreatePropertyImagesCommand
            {
                PropertyId = id,
                Images = images
            };
            var result = await _mediator.Send(command, cancellationToken);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }
            var response = new
            {
                Success = true,
                Message = result.Message,
                ImageIds = (result.Data as List<string>)?.Select((_, index) => index + 1).ToList()
            };
            return Ok(response);
        }
        [HttpDelete("images/{imageId}")]
        [SwaggerOperation(Summary = "Delete a property image by ID")]
        [SwaggerResponse(StatusCodes.Status200OK, "Image deleted successfully")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Image not found")]
        public async Task<IActionResult> DeletePropertyImage(
            [FromRoute] int imageId,
            CancellationToken cancellationToken = default)
        {
            var command = new DeletePropertyImageCommand { ImageId = imageId };
            var result = await _mediator.Send(command, cancellationToken);
            if (!result)
                return NotFound();
            return Ok();
        }
        [HttpPost("{id}/sell")]
        [SwaggerOperation(Summary = "Sell a property to a new owner")]
        [SwaggerRequestExample(typeof(SellPropertyCommand), typeof(SellPropertyCommandExample))]
        [SwaggerResponse(StatusCodes.Status200OK, "Sale completed successfully")]
        [SwaggerResponse(StatusCodes.Status400BadRequest, "Invalid request")]
        [SwaggerResponse(StatusCodes.Status404NotFound, "Property or owner not found")]
        public async Task<IActionResult> Sell(int id, [FromBody] SellPropertyCommand command, CancellationToken cancellationToken)
        {
            command.PropertyId = id;
            var result = await _mediator.Send(command, cancellationToken);
            return result.Success ? Ok(result) : BadRequest(result);
        }
    }
}