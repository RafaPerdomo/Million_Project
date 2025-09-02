using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using Moq;
using NUnit.Framework;
using properties.Api.Controllers;
using Properties.Domain.DTOs.Auth;
using Properties.Domain.Interfaces.Services;
using ValidationException = FluentValidation.ValidationException;

namespace Properties.UnitTests.Controllers
{
    [TestFixture]
    public class AuthControllerTests
    {
        private Mock<IAuthService> _authServiceMock;
        private Mock<ILogger<AuthController>> _loggerMock;
        private AuthController _controller;

        [SetUp]
        public void Setup()
        {
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();
            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        [Test]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            var request = new AuthRequest { EmailOrUsername = "test@example.com", Password = "P@ssw0rd" };
            var expectedResponse = new AuthResponse { Token = "test-token", RefreshToken = "refresh-token" };
            
            _authServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<AuthRequest>()))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.Login(request);

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(expectedResponse, okResult.Value);
        }

        [Test]
        public async Task Login_WithInvalidCredentials_ReturnsUnauthorized()
        {
            var request = new AuthRequest { EmailOrUsername = "wrong@example.com", Password = "wrongpass" };
            
            _authServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<AuthRequest>()))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

            var result = await _controller.Login(request);

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult);
            Assert.AreEqual(401, unauthorizedResult.StatusCode);
        }

        [Test]
        public void AuthRequest_Validation_ShouldPass_WithMinimalValidInput()
        {
            var request = new AuthRequest { EmailOrUsername = "user", Password = "123456" };
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(request, context, results, true);

            Assert.IsTrue(isValid);
            Assert.IsEmpty(results);
        }
        
        [Test]
        public void AuthRequest_Validation_ShouldFail_WhenPasswordIsTooShort()
        {
            var request = new AuthRequest { EmailOrUsername = "user", Password = "12345" };
            var context = new ValidationContext(request);
            var results = new List<ValidationResult>();

            var isValid = Validator.TryValidateObject(request, context, results, true);

            Assert.IsFalse(isValid);
            Assert.That(results, Has.Some.Matches<ValidationResult>(
                r => r.ErrorMessage.Contains("minimum length of 6")));
        }

        [Test]
        public async Task Login_WithLockedAccount_ReturnsUnauthorized()
        {
            var request = new AuthRequest { EmailOrUsername = "locked@example.com", Password = "P@ssw0rd" };
            
            _authServiceMock
                .Setup(x => x.AuthenticateAsync(It.IsAny<AuthRequest>()))
                .ThrowsAsync(new UnauthorizedAccessException("Account is locked"));

            var result = await _controller.Login(request);

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult);
            Assert.AreEqual(401, unauthorizedResult.StatusCode);
        }

        [Test]
        public async Task RefreshToken_WithExpiredToken_ReturnsNewTokens()
        {
            var request = new RefreshTokenRequest 
            { 
                Token = "expired-token",
                RefreshToken = "valid-refresh-token"
            };
            
            var expectedResponse = new AuthResponse 
            { 
                Token = "new-token", 
                RefreshToken = "new-refresh-token" 
            };
            
            _authServiceMock
                .Setup(x => x.RefreshTokenAsync(request.Token, request.RefreshToken))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.RefreshToken(request);

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.IsInstanceOf<AuthResponse>(okResult.Value);
            var response = okResult.Value as AuthResponse;
            Assert.AreEqual(expectedResponse.Token, response.Token);
            Assert.AreEqual(expectedResponse.RefreshToken, response.RefreshToken);
        }

        [Test]
        public async Task RevokeToken_WithValidToken_ReturnsOk()
        {
            var request = new RevokeTokenRequest { Token = "valid-token" };
            _authServiceMock.Setup(x => x.RevokeTokenAsync(request.Token)).ReturnsAsync(true);

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

            var result = await _controller.RevokeToken(request);

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [Test]
        public async Task Register_WithValidData_ReturnsOk()
        {
            var request = new RegisterRequest 
            { 
                Email = "new@example.com", 
                Username = "newuser",
                Password = "P@ssw0rd" 
            };
            
            _authServiceMock
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ReturnsAsync(true);

            var result = await _controller.Register(request);

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
        }

        [Test]
        public async Task Register_WithExistingEmail_ReturnsBadRequest()
        {
            var request = new RegisterRequest 
            { 
                Email = "existing@example.com", 
                Username = "existinguser",
                Password = "P@ssw0rd" 
            };
            
            _authServiceMock
                .Setup(x => x.RegisterAsync(It.IsAny<RegisterRequest>()))
                .ThrowsAsync(new InvalidOperationException("Email already exists"));

            var result = await _controller.Register(request);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }

        [Test]
        public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
        {
            var request = new RefreshTokenRequest 
            { 
                Token = "expired-token",
                RefreshToken = "valid-refresh-token" 
            };
            
            var expectedResponse = new AuthResponse 
            { 
                Token = "new-token", 
                RefreshToken = "new-refresh-token" 
            };
            
            _authServiceMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(expectedResponse);

            var result = await _controller.RefreshToken(request);

            Assert.IsInstanceOf<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.IsNotNull(okResult);
            Assert.AreEqual(200, okResult.StatusCode);
            Assert.AreEqual(expectedResponse, okResult.Value);
        }

        [Test]
        public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
        {
            var request = new RefreshTokenRequest 
            { 
                Token = "invalid-token",
                RefreshToken = "invalid-refresh-token" 
            };
            
            _authServiceMock
                .Setup(x => x.RefreshTokenAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new SecurityTokenException("Invalid token"));

            var result = await _controller.RefreshToken(request);

            Assert.IsInstanceOf<UnauthorizedObjectResult>(result);
            var unauthorizedResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(unauthorizedResult);
            Assert.AreEqual(401, unauthorizedResult.StatusCode);
        }

        [Test]
        public async Task RevokeToken_WithInvalidToken_ReturnsBadRequest()
        {
            var request = new RevokeTokenRequest { Token = "invalid-token" };
            
            _authServiceMock
                .Setup(x => x.RevokeTokenAsync(It.IsAny<string>()))
                .ReturnsAsync(false);

            var result = await _controller.RevokeToken(request);

            Assert.IsInstanceOf<BadRequestObjectResult>(result);
            var badRequestResult = result as BadRequestObjectResult;
            Assert.IsNotNull(badRequestResult);
            Assert.AreEqual(400, badRequestResult.StatusCode);
        }
    }
}
