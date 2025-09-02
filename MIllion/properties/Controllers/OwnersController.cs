using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using Properties.Domain.Exceptions;
using properties.Api.Application.Commands.Owners.CreateOwner;
using properties.Api.Application.Queries.Owners.GetOwnerById;
using properties.Api.Application.Queries.Owners.GetAllOwners;
using properties.Api.Application.Commands.Owners.UpdateOwnerPhoto;
using properties.Api.Application.Common.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace properties.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]

    public class OwnersController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<OwnersController> _logger;

        public OwnersController(IMediator mediator, ILogger<OwnersController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OwnerDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var owner = await _mediator.Send(new GetOwnerByIdQuery { Id = id });
                return Ok(owner);
            }
            catch (OwnerNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Owner with ID {id} not found");
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting owner with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving owner data");
            }
        }

        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OwnerDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromForm] CreateOwnerCommand command)
        {
            try
            {
                var result = await _mediator.Send(command);
                if (!result.Success)
                {
                    if (result.Message?.Contains("already exists") == true)
                        return Conflict(new { message = result.Message });
                    return BadRequest(new { message = result.Message, errors = result.Errors });
                }
                var ownerDto = result.Data as OwnerDto;
                return CreatedAtAction(nameof(GetById), new { id = ownerDto.Id }, ownerDto);
            }
            catch (OwnerAlreadyExistsException ex)
            {
                _logger.LogWarning(ex, "Owner creation failed - already exists");
                return Conflict(ex.Message);
            }
            catch (InvalidOwnerOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid owner operation during creation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating owner");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error creating owner data");
            }
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            try
            {
                var query = new GetAllOwnersQuery { PageNumber = pageNumber, PageSize = pageSize };
                var result = await _mediator.Send(query);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all owners");
                return StatusCode(StatusCodes.Status500InternalServerError, new ResponseDto 
                { 
                    Success = false, 
                    Message = "Error retrieving owners data",
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{id}/photo")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdatePhoto(int id, IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file was provided");
                }
                if (!file.ContentType.StartsWith("image/"))
                {
                    return BadRequest("The file must be an image");
                }

                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();
                var base64String = $"data:{file.ContentType};base64,{Convert.ToBase64String(fileBytes)}";
                
                var command = new UpdateOwnerPhotoCommand
                {
                    IdOwner = id,
                    PhotoBase64 = base64String
                };

                var result = await _mediator.Send(command);
                return Ok(result);
            }
            catch (OwnerNotFoundException ex)
            {
                _logger.LogWarning(ex, $"Owner with ID {id} not found when updating photo");
                return NotFound(ex.Message);
            }
            catch (InvalidOwnerOperationException ex)
            {
                _logger.LogWarning(ex, $"Invalid operation when updating photo for owner ID {id}");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating photo for owner with ID {id}");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the owner's photo");
            }
        }
    }
}