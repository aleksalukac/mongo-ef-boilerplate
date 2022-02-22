using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebApi.Entities;
using WebApi.Helpers;
using WebApi.Models.Accounts;
using WebApi.Services;

namespace WebApi.Controllers
{
    public class Entity
    {
        public ObjectId _id { get; set; }
        public string Name { get; set; }
        public int Number { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class AccountsController : BaseController
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly IMongoDBContext _context;

        public AccountsController(
            IAccountService accountService,
            IMapper mapper,
            IMongoDBContext context)
        {
            _accountService = accountService;
            _mapper = mapper;
            _context = context;
        }

        [HttpGet("mongo")]
        public async Task<ActionResult<string>> Mongo()
        {
            var collection = _context.GetCollection<Entity>("entities");
            var result =
                collection.AsQueryable<Entity>()
                .Where(x => x.Number > 3)
                .Select(c => c.Name)
                .ToList();

            return JsonConvert.SerializeObject(result);
        }

        [HttpPost("authenticate")]
        public async Task<ActionResult<AuthenticateResponse>> Authenticate(AuthenticateRequest model)
        {

            var response = await _accountService.Authenticate(model, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [HttpPost("refresh-token")]
        public ActionResult<AuthenticateResponse> RefreshToken()
        {
            var refreshToken = Request.Cookies["refreshToken"];
            var response = _accountService.RefreshToken(refreshToken, ipAddress());
            setTokenCookie(response.RefreshToken);
            return Ok(response);
        }

        [Authorize]
        [HttpPost("revoke-token")]
        public IActionResult RevokeToken(RevokeTokenRequest model)
        {
            // accept token from request body or cookie
            var token = model.Token ?? Request.Cookies["refreshToken"];

            if (string.IsNullOrEmpty(token))
                return BadRequest(new { message = "Token is required" });

            // users can revoke their own tokens and admins can revoke any tokens
            if (!Account.OwnsToken(token) && Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            _accountService.RevokeToken(token, ipAddress());
            return Ok(new { message = "Token revoked" });
        }

        [HttpPost("register")]
        public IActionResult Register(RegisterRequest model)
        {
            _accountService.Register(model, Request.Headers["origin"]);
            return Ok(new { message = "Registration successful, please check your email for verification instructions" });
        }

        [HttpPost("verify-email")]
        public IActionResult VerifyEmail(VerifyEmailRequest model)
        {
            _accountService.VerifyEmail(model.Token);
            return Ok(new { message = "Verification successful, you can now login" });
        }

        [HttpPost("forgot-password")]
        public IActionResult ForgotPassword(ForgotPasswordRequest model)
        {
            _accountService.ForgotPassword(model, Request.Headers["origin"]);
            return Ok(new { message = "Please check your email for password reset instructions" });
        }

        [HttpPost("validate-reset-token")]
        public IActionResult ValidateResetToken(ValidateResetTokenRequest model)
        {
            _accountService.ValidateResetToken(model);
            return Ok(new { message = "Token is valid" });
        }

        [HttpPost("reset-password")]
        public IActionResult ResetPassword(ResetPasswordRequest model)
        {
            _accountService.ResetPassword(model);
            return Ok(new { message = "Password reset successful, you can now login" });
        }

        [Authorize(Role.Admin)]
        [HttpGet]
        public ActionResult<IEnumerable<AccountResponse>> GetAll()
        {
            var accounts = _accountService.GetAll();
            return Ok(accounts);
        }

        [Authorize]
        [HttpGet("{id:int}")]
        public ActionResult<AccountResponse> GetById(ObjectId id)
        {
            // users can get their own account and admins can get any account
            if (id != Account._id & Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            //var account = _accountService.GetById(id);
            var account = new Account();
            return Ok(account);
        }

        [Authorize(Role.Admin)]
        [HttpPost("create")]
        public ActionResult<AccountResponse> Create(CreateRequest model)
        {
            var account = _accountService.Create(model);
            return Ok(account);
        }

        [HttpGet("GetAllAcc")]
        public List<Account> GetAllAcc()
        {
            return _context.GetQueryable<Account>().ToList();
        }

        [Authorize]
        [HttpPut("{id}")]
        public ActionResult<AccountResponse> Update(string _id, UpdateRequest model)
        {
            ObjectId id = ObjectId.Parse(_id);
            // users can update their own account and admins can update any account
            if (id != Account._id && Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            // only admins can update role
            if (Account.Role != Role.Admin)
                model.Role = null;

            //var account = _accountService.Update(id, model);
            var account = new Account();
            return Ok(account);
        }

        [Authorize]
        [HttpDelete("{id}")]
        public IActionResult Delete(string _id)
        {
            var id = ObjectId.Parse(_id);
            // users can delete their own account and admins can delete any account
            if (id != Account._id && Account.Role != Role.Admin)
                return Unauthorized(new { message = "Unauthorized" });

            //_accountService.Delete(id);
            return Ok(new { message = "Account deleted successfully" });
        }

        // helper methods

        private void setTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Expires = DateTime.UtcNow.AddDays(7)
            };
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

        private string ipAddress()
        {
            if (Request.Headers.ContainsKey("X-Forwarded-For"))
                return Request.Headers["X-Forwarded-For"];
            else
                return HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }
    }
}
