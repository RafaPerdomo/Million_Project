using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using properties.Api.Application.Queries.Owners.GetAllOwners;
using properties.Api.Application.Queries.Owners.GetOwnerById;
using properties.Api.Application.Common.DTOs;
using properties.Api.Application.Commands.Owners.UpdateOwnerPhoto;
using properties.Controllers;
using Properties.Domain.Exceptions;

namespace Properties.UnitTests.Controllers
{
    [TestFixture]
    public class OwnersControllerTests
    {
        private Mock<IMediator> _mediatorMock;
        private Mock<ILogger<OwnersController>> _loggerMock;
        private OwnersController _controller;

        [SetUp]
        public void Setup()
        {
            _mediatorMock = new Mock<IMediator>();
            _loggerMock = new Mock<ILogger<OwnersController>>();
            _controller = new OwnersController(_mediatorMock.Object, _loggerMock.Object);

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
        public async Task GetById_WithValidId_ReturnsOkResult()
        {
            var ownerId = 1;
            var expectedOwner = new OwnerDto { Id = ownerId, Name = "Test Owner" };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetOwnerByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedOwner);
            var result = await _controller.GetById(ownerId);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.IsInstanceOf<OwnerDto>(okResult.Value);
            Assert.AreEqual(ownerId, ((OwnerDto)okResult.Value).Id);
        }

        [Test]
        public async Task GetById_WithNonExistentId_ReturnsNotFound()
        {
            var ownerId = 999;
            _mediatorMock
                .Setup(m => m.Send(It.IsAny<GetOwnerByIdQuery>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new OwnerNotFoundException(ownerId));

            var result = await _controller.GetById(ownerId);

            Assert.IsInstanceOf<NotFoundObjectResult>(result);
            var notFoundResult = result as NotFoundObjectResult;
            Assert.IsNotNull(notFoundResult);
            Assert.AreEqual(StatusCodes.Status404NotFound, notFoundResult.StatusCode);
            Assert.IsNotNull(notFoundResult.Value);
        }

        [Test]
        public async Task GetAll_ReturnsListOfOwners()
        {
            var owners = new List<OwnerDto>
            {
                new() { Id = 1, Name = "Owner 1" },
                new() { Id = 2, Name = "Owner 2" }
            };
            var response = new ResponseDto
            {
                Success = true,
                Data = owners,
                Message = "Owners retrieved successfully"
            };
            _mediatorMock.Setup(m => m.Send(It.IsAny<GetAllOwnersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);

            var result = await _controller.GetAll();

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
            Assert.IsInstanceOf<ResponseDto>(okResult.Value);
            var responseDto = okResult.Value as ResponseDto;
            Assert.IsTrue(responseDto.Success);
            Assert.IsInstanceOf<List<OwnerDto>>(responseDto.Data);
            var returnedOwners = responseDto.Data as List<OwnerDto>;
            Assert.AreEqual(2, returnedOwners.Count);
        }

        [Test]
        public async Task GetAll_WithPagination_ReturnsPaginatedResults()
        {
            var pageNumber = 1;
            var pageSize = 10;
            var totalCount = 20;
            
            var owners = Enumerable.Range(1, pageSize).Select(i => new OwnerDto 
            { 
                Id = i, 
                Name = $"Owner {i}",
                Address = $"Address {i}",
                Photo = $"photo{i}.jpg"
            }).ToList();
            
            var response = new ResponseDto
            {
                Success = true,
                Data = new 
                {
                    Items = owners,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                },
                Message = "Owners retrieved successfully"
            };
            
            _mediatorMock.Setup(m => m.Send(It.Is<GetAllOwnersQuery>(q => 
                q.PageNumber == pageNumber && q.PageSize == pageSize), 
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(response);
                
            var result = await _controller.GetAll(pageNumber, pageSize);
            
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            var responseDto = okResult.Value as ResponseDto;
            
            Assert.IsTrue(responseDto.Success);
            dynamic responseData = responseDto.Data;
            Assert.IsNotNull(responseData);
            
            Assert.AreEqual(pageNumber, (int)responseData.PageNumber);
            Assert.AreEqual(pageSize, (int)responseData.PageSize);
            Assert.AreEqual(totalCount, (int)responseData.TotalCount);
            Assert.IsTrue(owners.Count == pageSize);
            Assert.AreEqual(2, (int)responseData.TotalPages);
        }
        [Test]
        public async Task UpdatePhoto_WithValidFile_ReturnsOk()
        {
            var ownerId = 1;
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            fileMock.Setup(f => f.Length).Returns(1024);
            _mediatorMock.Setup(m => m.Send(It.IsAny<UpdateOwnerPhotoCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResponseDto { Success = true, Message = "Photo updated successfully" });
            var result = await _controller.UpdatePhoto(ownerId, fileMock.Object);
            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(StatusCodes.Status200OK, okResult.StatusCode);
        }
    }
}