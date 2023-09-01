using System.Data;
using AutoMapper;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Helpers;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class AuthCompleteController : ControllerBase
    {
        private readonly DataContextDapper _dapper;
        private readonly ReusableSQL _reusableSQL;
        private readonly AuthHelper _authHelper;
        private readonly IMapper _mapper;
        public AuthCompleteController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _reusableSQL = new ReusableSQL(config);
            _authHelper = new AuthHelper(config);
            _mapper = new Mapper(new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<UserForRegistrationDto, UserComplete>();
            }));
        }

        [AllowAnonymous]
        [HttpPost("Register")]
        public IActionResult Register(UserForRegistrationDto userForRegistration)
        {
            if (userForRegistration.Password == userForRegistration.PasswordConfirm)
            {
                string sqlCheckUserExists = "SELECT Email FROM TutorialAppSchema.Auth WHERE Email = '" + userForRegistration.Email + "'";

                IEnumerable<string> existingUsers = _dapper.LoadData<string>(sqlCheckUserExists);

                if (existingUsers.Count() == 0)
                {
                    UserForLoginDto userForSetPassword = new UserForLoginDto()
                    {
                        Email = userForRegistration.Email,
                        Password = userForRegistration.Password,
                    };

                    if (_authHelper.setPassword(userForSetPassword))
                    {
                        UserComplete userComplete = _mapper.Map<UserComplete>(userForRegistration);
                        userComplete.Active = true;

                        if (_reusableSQL.UpsertUser(userComplete))
                        {
                            return Ok();
                        }
                        throw new Exception("Failed to Add user.");
                    }
                    throw new Exception("Failed to Register user.");
                }
                throw new Exception("Email already registered.");
            }
            throw new Exception("Passwords do not match.");
        }

        [HttpPut("ResetPassword")]
        public IActionResult ResetPassword(UserForLoginDto userForSetPassword)
        {
            if (_authHelper.setPassword(userForSetPassword))
            {
                return Ok();
            }
            throw new Exception("Failed to update password.");
        }

        [AllowAnonymous]
        [HttpPost("Login")]
        public IActionResult Login(UserForLoginDto userForLogin)
        {
            DynamicParameters sqlParameters = new DynamicParameters();

            sqlParameters.Add("@EmailParam", userForLogin.Email, DbType.String);

            string sqlForHashAndSalt = "EXEC TutorialAppSchema.spLoginConfirmation_Get @Email = @EmailParam";

            UserForLoginConfirmationDto userForConfirmation = _dapper.LoadDataSingleWithParameter<UserForLoginConfirmationDto>(sqlForHashAndSalt, sqlParameters);

            byte[] passwordHash = _authHelper.GetPasswordHash(userForLogin.Password, userForConfirmation.PasswordSalt);

            for (int i = 0; i < passwordHash.Length; i++)
            {
                if (passwordHash[i] != userForConfirmation.PasswordHash[i])
                {
                    return StatusCode(401, "Incorrect password.");
                }
            }

            string userIdSql = "SELECT UserId FROM TutorialAppSchema.Users WHERE Email = '" + userForLogin.Email + "'";

            int userId = _dapper.LoadDataSingle<int>(userIdSql);

            return Ok(new Dictionary<string, string> {
                {"token", _authHelper.CreateToken(userId)}
            });
        }

        [HttpGet("RefreshToken")]
        public string RefreshToken()
        {
            string sqlGetUserId = "SELECT UserId FROM TutorialAppSchema.Users WHERE UserId = '" + this.User.FindFirst("userId")?.Value + "'";

            int userId = _dapper.LoadDataSingle<int>(sqlGetUserId);

            return _authHelper.CreateToken(userId);
        }
    }
}
