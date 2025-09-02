using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using properties.Api.Application.Commands.Propertys.CreateProperty;
using properties.Api.Application.Commands.Propertys.CreatePropertyImages;
using properties.Api.Application.Commands.Propertys.DeletePropertyImage;
using properties.Api.Application.Commands.Propertys.SellProperty;
using properties.Api.Application.Commands.Propertys.UpdateProperty;
using properties.Api.Application.Queries.Propertys.GetPropertyImage;
using properties.Api.Application.Queries.Propertys.ListProperties;
using properties.Api.Application.Queries.Propertys.GetPropertyById;
using properties.Api.Controllers;
using properties.Api.Application.Common.DTOs;
using Properties.Domain.Exceptions;
using properties.Controllers;

namespace Properties.UnitTests.Controllers
{
    [TestFixture]
    public class PropertiesControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<PropertiesController>> _loggerMock;
        private PropertiesController _controller;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<PropertiesController>>();
            
            _controller = new PropertiesController(_mediatorMock.Object);
            
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("jti", Guid.NewGuid().ToString())
            }));
            
            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user }
            };
        }
        [Test]
        public async Task GetProperties_ReturnsListOfProperties()
        {
            var properties = new List<PropertyListDto>
            {
                new() { Id = 1, Name = "Property 1", CodeInternal = "P001" },
                new() { Id = 2, Name = "Property 2", CodeInternal = "P002" }
            };
            var response = new PropertiesListResponseDto
            {
                Properties = properties,
                TotalCount = 2,
                PageNumber = 1,
                PageSize = 10
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<ListPropertiesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
                
            var result = await _controller.GetProperties(new ListPropertiesQuery());
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            var responseDto = okResult.Value as PropertiesListResponseDto;
            Assert.IsNotNull(responseDto);
            Assert.IsInstanceOf<IEnumerable<PropertyListDto>>(responseDto.Properties);
            var returnedProperties = responseDto.Properties as List<PropertyListDto>;
            Assert.AreEqual(2, returnedProperties?.Count);
        }
        
        [Test]
        public async Task GetProperties_WithFilters_ReturnsFilteredResults()
        {
            var query = new ListPropertiesQuery 
            { 
                MinPrice = 100000,
                MaxPrice = 500000,
                Year = 2020,
                PageNumber = 1,
                PageSize = 10
            };
            
            var properties = new List<PropertyListDto>
            {
                new() { Id = 1, Name = "Filtered Property", Price = 250000, Year = 2020 }
            };
            
            var response = new PropertiesListResponseDto
            {
                Properties = properties,
                TotalCount = 1,
                PageNumber = query.PageNumber,
                PageSize = query.PageSize
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<ListPropertiesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
                
            var result = await _controller.GetProperties(query);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var responseDto = okResult.Value as PropertiesListResponseDto;
            Assert.AreEqual(1, responseDto.TotalCount);
            Assert.AreEqual(1, responseDto.Properties.Count());
        }
        
        [Test]
        public async Task GetPropertyById_WithValidId_ReturnsProperty()
        {
            var propertyId = 1;
            var property = new PropertyDto 
            { 
                Id = propertyId, 
                Name = "Test Property",
                Price = 300000,
                Year = 2020,
                CodeInternal = "P001"
            };
            
            var response = new ResponseDto 
            { 
                Success = true, 
                Data = property,
                Message = "Property retrieved successfully" 
            };
            
            _mediatorMock.Setup(m => m.Send(It.Is<GetPropertyByIdQuery>(q => q.Id == propertyId), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
                
            var result = await _controller.GetPropertyById(propertyId);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            
            var responseDto = okResult.Value as ResponseDto;
            Assert.IsNotNull(responseDto);
            Assert.IsTrue(responseDto.Success);
            
            var returnedProperty = responseDto.Data as PropertyDto;
            Assert.IsNotNull(returnedProperty);
            Assert.AreEqual(propertyId, returnedProperty.Id);
            Assert.AreEqual("Test Property", returnedProperty.Name);
            Assert.AreEqual(propertyId, returnedProperty.Id);
        }
        
        [Test]
        public async Task GetPropertyById_WithNonExistentId_ReturnsNotFound()
        {
            var propertyId = 999;
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetPropertyByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PropertyNotFoundException(propertyId));
                
            var result = await _controller.GetPropertyById(propertyId);
            
            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
        }
        
        [Test]
        public async Task CreateProperty_WithValidData_ReturnsCreated()
        {
            var command = new CreatePropertyCommand 
            { 
                Name = "New Property",
                Address = "123 Test St",
                Price = 250000,
                Year = 2023,
                CodeInternal = "NEW001"
            };
            
            var createdProperty = new PropertyDto 
            { 
                Id = 1, 
                Name = command.Name,
                Address = command.Address,
                Price = command.Price,
                Year = command.Year,
                CodeInternal = command.CodeInternal
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseDto { Success = true, Data = createdProperty, Message = "Property created successfully" });
                
            var result = await _controller.CreateProperty(command);
            
            Assert.IsInstanceOf<ObjectResult>(result);
            var objectResult = result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status201Created, objectResult.StatusCode);
            
            var response = objectResult.Value as ResponseDto;
            var responseData = response.Data as PropertyDto;
            Assert.IsTrue(response.Success);
            Assert.AreEqual(createdProperty.Id, responseData.Id);
            Assert.AreEqual(createdProperty.Name, responseData.Name);
        }
        
        [Test]
        public async Task CreateProperty_WithDuplicateCode_ReturnsConflict()
        {
            var command = new CreatePropertyCommand 
            { 
                Name = "Duplicate Property",
                CodeInternal = "DUP001"
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<CreatePropertyCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new PropertyAlreadyExistsException("Property with this code already exists"));
                
            var result = await _controller.CreateProperty(command);
            
            Assert.IsInstanceOf<ConflictObjectResult>(result);
            var conflictResult = result as ConflictObjectResult;
            Assert.IsNotNull(conflictResult);
            Assert.AreEqual(StatusCodes.Status409Conflict, conflictResult.StatusCode);
        }
        
        [Test]
        public async Task AddPropertyImages_WithValidData_ReturnsOk()
        {
            var propertyId = 1;
            var files = new List<IFormFile>();
            var fileMock = new Mock<IFormFile>();
            var content = "Fake image content";
            var fileName = "test.jpg";
            var ms = new MemoryStream();
            var writer = new StreamWriter(ms);
            writer.Write(content);
            writer.Flush();
            ms.Position = 0;
            
            fileMock.Setup(_ => _.OpenReadStream()).Returns(ms);
            fileMock.Setup(_ => _.FileName).Returns(fileName);
            fileMock.Setup(_ => _.Length).Returns(ms.Length);
            fileMock.Setup(_ => _.ContentType).Returns("image/jpeg");
            
            files.Add(fileMock.Object);
            
            var imageUrls = new List<string> { $"properties/{propertyId}/images/test.jpg" };
            _mediatorMock.Setup(m => m.Send(It.Is<CreatePropertyImagesCommand>(c => 
                    c.PropertyId == propertyId && 
                    c.Images != null && 
                    c.Images.Count == files.Count), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseDto { 
                    Success = true, 
                    Data = imageUrls, 
                    Message = "Images uploaded successfully" 
                });
                
            var result = await _controller.AddPropertyImages(propertyId, files);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            
            var response = okResult.Value.GetType().GetProperty("Success").GetValue(okResult.Value);
            var message = okResult.Value.GetType().GetProperty("Message").GetValue(okResult.Value) as string;
            var imageIds = okResult.Value.GetType().GetProperty("ImageIds").GetValue(okResult.Value) as List<int>;
            
            Assert.IsTrue((bool)response);
            Assert.AreEqual("Images uploaded successfully", message);
            Assert.IsNotNull(imageIds);
            Assert.AreEqual(1, imageIds.Count);
            Assert.AreEqual(1, imageIds[0]);
        }
        
        [Test]
        public async Task UpdateProperty_WithValidData_ReturnsOk()
        {
            var propertyId = 1;
            var command = new UpdatePropertyCommand 
            { 
                Name = "Updated Property",
                Price = 300000,
                Year = 2023
            };
            
            var updatedProperty = new PropertyDto 
            { 
                Id = propertyId,
                Name = command.Name,
                Price = command.Price ?? 0, // Handle nullable decimal
                Year = command.Year ?? 0    // Handle nullable int
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePropertyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseDto { Success = true, Data = updatedProperty, Message = "Property updated successfully" });
                
            var result = await _controller.UpdateProperty(propertyId, command);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var response = okResult.Value as ResponseDto;
            var responseData = response.Data as PropertyDto;
            
            Assert.IsTrue(response.Success);
            Assert.AreEqual(propertyId, responseData.Id);
            Assert.AreEqual(command.Name, responseData.Name);
            Assert.AreEqual(command.Price, responseData.Price);
        }
        
        [Test]
        public async Task SellProperty_WithValidData_ReturnsOk()
        {
            var propertyId = 1;
            var command = new SellPropertyCommand 
            { 
                PropertyId = propertyId,
                NewOwnerId = 2,
                SalePrice = 350000,
                TaxPercentage = 10.0m
            };
            
            var saleResult = new
            {
                PropertyId = propertyId,
                PreviousOwnerId = 1,
                NewOwnerId = command.NewOwnerId,
                SalePrice = command.SalePrice,
                TaxAmount = command.SalePrice * (command.TaxPercentage / 100),
                TotalAmount = command.SalePrice * (1 + command.TaxPercentage / 100)
            };
            
            var response = new ResponseDto
            {
                Success = true,
                Message = "Property sold successfully",
                Data = saleResult
            };
            
            _mediatorMock.Setup(m => m.Send(It.IsAny<SellPropertyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
                
            var result = await _controller.Sell(propertyId, command, CancellationToken.None);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var responseDto = okResult.Value as ResponseDto;
            dynamic responseData = responseDto.Data;
            
            Assert.IsTrue(responseDto.Success);
            Assert.AreEqual(propertyId, (int)responseData.PropertyId);
            Assert.AreEqual(2, (int)responseData.NewOwnerId);
            Assert.AreEqual(350000m, (decimal)responseData.SalePrice);
            Assert.AreEqual(35000m, (decimal)responseData.TaxAmount);
            Assert.AreEqual(385000m, (decimal)responseData.TotalAmount);
        }
        [Test]
        public async Task GetPropertyImage_WithValidId_ReturnsImage()
        {
            var imageId = 1;
            var imageDetail = new PropertyImageDetailDto { Id = imageId, File = "test.jpg" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetPropertyImageQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(imageDetail);
            var result = await _controller.GetPropertyImage(imageId);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.IsInstanceOf<PropertyImageDetailDto>(okResult.Value);
            Assert.AreEqual(imageId, ((PropertyImageDetailDto)okResult.Value).Id);
        }
        [Test]
        public async Task UpdateProperty_WithValidId_ReturnsOk()
        {
            var propertyId = 1;
            var command = new UpdatePropertyCommand { Id = propertyId, Name = "Updated Property" };
            var response = new ResponseDto 
            { 
                Success = true, 
                Message = "Property updated successfully" 
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdatePropertyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var result = await _controller.UpdateProperty(propertyId, command);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
        }
        [Test]
        public async Task DeletePropertyImage_WithValidId_ReturnsOk()
        {
            var imageId = 1;
            _mediatorMock.Setup(m => m.Send(It.IsAny<DeletePropertyImageCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            var result = await _controller.DeletePropertyImage(imageId);
            Assert.IsInstanceOf<OkResult>(result);
        }
        [Test]
        public async Task Sell_WithValidData_ReturnsOk()
        {
            var propertyId = 1;
            var command = new SellPropertyCommand { PropertyId = propertyId, NewOwnerId = 1, SalePrice = 100000, TaxPercentage = 10m };
            var response = new ResponseDto 
            { 
                Success = true, 
                Message = "Property sold successfully" 
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<SellPropertyCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
            var result = await _controller.Sell(propertyId, command, CancellationToken.None);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
        }
    }
}