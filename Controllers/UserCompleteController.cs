using System.Data;
using Dapper;
using DotnetAPI.Data;
using DotnetAPI.Helpers;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("[controller]")]
    public class UserCompleteController : ControllerBase
    {

        private readonly DataContextDapper _dapper;
        private readonly ReusableSQL _reusableSQL;
        public UserCompleteController(IConfiguration config)
        {
            _dapper = new DataContextDapper(config);
            _reusableSQL = new ReusableSQL(config);
        }

        // [HttpGet("TestConnection")]
        // public DateTime TestConnection()
        // {
        //     return _dapper.LoadDataSingle<DateTime>("SELECT GETDATE()");
        // }

        [HttpGet("GetUsers/{userId}/{Active}")]
        public IEnumerable<UserComplete> GetUsers(int userId, bool active)
        {
            string sql = "EXEC TutorialAppSchema.spUsers_Get";
            string parameters = "";

            DynamicParameters sqlParameters = new DynamicParameters();


            if (userId != 0)
            {
                parameters += ", @UserId=@UserIdParameter";
                sqlParameters.Add("@UserIdParameter", userId, DbType.Int32);
            }
            if (active)
            {
                parameters += ", @Active=@ActiveParameter";
                sqlParameters.Add("@ActiveParameter", active, DbType.Boolean);
            }

            if (parameters.Length > 0)
            {
                sql += parameters[1..];
            }
            IEnumerable<UserComplete> users = _dapper.LoadDataWithParameter<UserComplete>(sql, sqlParameters);
            return users;
        }

        [HttpPut("UpsertUser")]
        public IActionResult UpsertUser(UserComplete user)
        {
            if (_reusableSQL.UpsertUser(user))
            {
                return Ok();
            }

            throw new Exception("Failed to Upsert tUser.");
        }

        [HttpDelete("DeleteUser/{userId}")]
        public IActionResult DeleteUser(int userId)
        {
            string sql = "EXEC TutorialAppSchema.spUser_Delete @UserId = @UserIdParam";

            DynamicParameters sqlParameters = new DynamicParameters();
            sqlParameters.Add("@UserIdParam", userId, DbType.Int32);

            if (_dapper.ExecuteSqlWithParameter(sql, sqlParameters))
            {
                return Ok();
            }
            throw new Exception("Failed to Delete User.");
        }
    }

}

