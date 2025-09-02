using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Properties.Domain.DTOs.Auth;
using Properties.Domain.Entities.Auth;
using Properties.Domain.Helpers;
using Properties.Domain.Interfaces;
using Properties.Domain.Interfaces.Services;
using Properties.Domain.Settings;

namespace Properties.Infrastructure.Services
{
    public class AuthService : IAuthService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(IOptions<JwtSettings> jwtSettings, IUnitOfWork unitOfWork)
        {
            _jwtSettings = jwtSettings.Value;
            _unitOfWork = unitOfWork;
        }

        public async Task<AuthResponse> AuthenticateAsync(AuthRequest request)
        {
            var user = await _unitOfWork.Users.GetByEmailOrUsernameAsync(request.EmailOrUsername);
            if (user == null || !PasswordHasher.Verify(request.Password, Convert.ToBase64String(user.PasswordHash) + ":" + Convert.ToBase64String(user.PasswordSalt) + ":10000:SHA256"))
                throw new UnauthorizedAccessException("Invalid credentials");

            if (!user.IsActive)
                throw new UnauthorizedAccessException("Account is deactivated");

            var token = GenerateJwtToken(user);
            var refreshToken = GenerateRefreshToken(user.Id);

            user.RefreshTokens.Add(refreshToken);
            user.LastLoginDate = DateTime.UtcNow;
            
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return new AuthResponse
            {
                Token = token,
                RefreshToken = refreshToken.Token,
                Expiration = refreshToken.Expires,
                User = MapToUserDto(user)
            };
        }

        public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(refreshToken);
            if (user == null)
                throw new SecurityTokenException("Invalid refresh token");

            var existingRefreshToken = user.RefreshTokens.Single(x => x.Token == refreshToken);
            if (!existingRefreshToken.IsActive)
                throw new SecurityTokenException("Refresh token expired");

            existingRefreshToken.Revoked = DateTime.UtcNow;

            var newToken = GenerateJwtToken(user);
            var newRefreshToken = GenerateRefreshToken(user.Id);
            user.RefreshTokens.Add(newRefreshToken);

            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();

            return new AuthResponse
            {
                Token = newToken,
                RefreshToken = newRefreshToken.Token,
                Expiration = newRefreshToken.Expires,
                User = MapToUserDto(user)
            };
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var user = await _unitOfWork.Users.GetByRefreshTokenAsync(token);
            if (user == null)
                return false;

            var refreshToken = user.RefreshTokens.Single(x => x.Token == token);
            if (!refreshToken.IsActive)
                return false;

            refreshToken.Revoked = DateTime.UtcNow;
            await _unitOfWork.Users.UpdateAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RegisterAsync(RegisterRequest request)
        {
            if (await _unitOfWork.Users.GetByEmailAsync(request.Email) != null)
            {
                throw new InvalidOperationException("Email is already registered");
            }

            if (await _unitOfWork.Users.GetByUsernameAsync(request.Username) != null)
            {
                throw new InvalidOperationException("Username is already taken");
            }

            var passwordHash = PasswordHasher.Hash(request.Password);
            var segments = passwordHash.Split(':');
            
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = Convert.FromBase64String(segments[0]),
                PasswordSalt = Convert.FromBase64String(segments[1]),
                FirstName = request.FirstName,
                LastName = request.LastName,
                IsActive = true
            };

            var defaultRole = await _unitOfWork.Roles.GetByNameAsync("User");
            if (defaultRole != null)
            {
                user.UserRoles.Add(new UserRole { Role = defaultRole });
            }

            await _unitOfWork.Users.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return true;
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null);
            if (roles != null)
            {
                claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.ExpireDays),
                Issuer = _jwtSettings.Issuer,
                Audience = _jwtSettings.Audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key), 
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static RefreshToken GenerateRefreshToken(int userId)
        {
            using var rngCryptoServiceProvider = RandomNumberGenerator.Create();
            var randomBytes = new byte[64];
            rngCryptoServiceProvider.GetBytes(randomBytes);
            
            return new RefreshToken
            {
                Token = Convert.ToBase64String(randomBytes),
                Expires = DateTime.UtcNow.AddDays(7), // Refresh token valid for 7 days
                CreatedAt = DateTime.UtcNow,
                UserId = userId
            };
        }

        private static UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Roles = user.UserRoles?.Select(ur => ur.Role?.Name).Where(r => r != null).ToList()
            };
        }
    }
}
