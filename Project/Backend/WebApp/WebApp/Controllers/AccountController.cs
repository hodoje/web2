﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using WebApp.Models;
using WebApp.Models.DomainModels;
using WebApp.Persistence.UnitOfWork;
using WebApp.Providers;
using WebApp.Results;
using System.Linq;
using AutoMapper;
using System.Drawing;
using System.IO;
using WebApp.BusinessComponents.NotificationHubs;
using WebApp.Models.Dtos;
using System.Threading;
using System.Data.Entity;
using WebApp.BusinessComponents;
using System.Text;
using WebApp.Models.Enumerations;

namespace WebApp.Controllers
{
    //[Authorize]
    [RoutePrefix("api/Account")]
    public class AccountController : ApiController
    {
        private const string LocalLoginProvider = "Local";
        private ApplicationUserManager _userManager;
		private IUnitOfWork unitOfWork;
        private IMapper mapper;
        private IEmailSender emailSender;
        private UserProfileConfirmationHub userProfileConfirmationHub;

        public AccountController(ApplicationUserManager userManager,
            ISecureDataFormat<AuthenticationTicket> accessTokenFormat,
			IUnitOfWork uow,
            IMapper imapper,
            IEmailSender eSender,
            UserProfileConfirmationHub hub)
        {
            UserManager = userManager;
            AccessTokenFormat = accessTokenFormat;
			unitOfWork = uow;
            mapper = imapper;
            emailSender = eSender;
            this.userProfileConfirmationHub = hub;
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? Request.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        public ISecureDataFormat<AuthenticationTicket> AccessTokenFormat { get; private set; }

        [AllowAnonymous]
        [Route("CheckIfEmailExists")]
        [HttpPost]
        public async Task<IHttpActionResult> CheckIfEmailExistsAsync(CheckEmailExistsDto check)
        {
            ApplicationUser user = await UserManager.FindByNameAsync(check.Email);
            if(user == null)
            {
                return Ok(new
                {
                    exists = false
                });
            }
            else
            {
                return Ok(new
                {
                    exists = true
                });
            }
        }

        // GET api/Account/
        [Route("GetUserPersonalData")]
        [HttpGet]
        public async Task<ApplicationUserDto> GetUserPersonalDataAsync()
        {
            string username = User.Identity.GetUserName();
            ApplicationUser user = await UserManager.FindByNameAsync(username);
            ApplicationUserDto userDto = mapper.Map<ApplicationUser, ApplicationUserDto>(user);
            return userDto;
        }

        [Route("GetUserDocument")]
        [HttpGet]
        public async Task<HttpResponseMessage> GetUserDocumentAsync()
        {
            string username = User.Identity.GetUserName();
            ApplicationUser user = await UserManager.FindByNameAsync(username);
			if (!String.IsNullOrWhiteSpace(user.DocumentImage))
			{
				Image img = GetUserImage(user.DocumentImage);
				using (MemoryStream ms = new MemoryStream())
				{
					img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

					HttpResponseMessage result = new HttpResponseMessage(System.Net.HttpStatusCode.OK);
					result.Content = new ByteArrayContent(ms.ToArray());
					result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

					return result;
				}
			}
			return new HttpResponseMessage(System.Net.HttpStatusCode.NotFound);
        }

        private Image GetUserImage(string userImagePath)
        {
            string imgPath = HttpContext.Current.Server.MapPath("~/Resources/" + userImagePath);
            return new Bitmap(imgPath);
        }

		[Route("ChangeUserData")]
		[HttpPost]
		public async Task<IHttpActionResult> ChangeUserData(RegisterBindingModel model)
		{
			string username = User.Identity.GetUserName();
			ApplicationUser user = await UserManager.FindByNameAsync(username);

			if (!String.IsNullOrWhiteSpace(model.Name))
			{
				user.Name = model.Name;
			}

			if (!String.IsNullOrWhiteSpace(model.LastName))
			{
				user.LastName = model.LastName;
			}

			if (!String.IsNullOrWhiteSpace(model.Address))
			{
				user.Address = model.Address;
			}

			if (model.Birthday != null)
			{
				user.Birthday = model.Birthday;
			}

			UserType requestedUserType = unitOfWork.UserTypeRepository.Find(ut => ut.Name == model.RequestedUserType).FirstOrDefault();
			if(requestedUserType != null)
			{
				user.UserType = requestedUserType;
			}

			try
			{
				UserManager.Update(user);
			}
			catch(Exception e)
			{
				return BadRequest();
			}

			return Ok();
		}

		[Route("ChangeUserDocument")]
		[HttpPost]
		public async Task<IHttpActionResult> ChangeUserDocumentAsync()
		{
			var httpRequest = HttpContext.Current.Request;

			string documentImageFileName = null;
			if (httpRequest.Files.Count > 0)
			{
				foreach (string file in httpRequest.Files)
				{
					var postedFile = httpRequest.Files[file];
					var filePath = HttpContext.Current.Server.MapPath("~/Resources/" + postedFile.FileName);
					postedFile.SaveAs(filePath);
					documentImageFileName = postedFile.FileName;
					break;
				}
			}
			else
			{
				return BadRequest();
			}

			try
			{
				if (!String.IsNullOrWhiteSpace(documentImageFileName))
				{
					string username = User.Identity.GetUserName();
					ApplicationUser user = await UserManager.FindByNameAsync(username);
					user.DocumentImage = documentImageFileName;
					UserManager.Update(user);

					return Ok();
				}
				else
				{
					return BadRequest();
				}
			}
			catch (Exception ex)
			{
				return BadRequest();
			}
		}

        // GET api/Account/UserInfo
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("UserInfo")]
        public UserInfoViewModel GetUserInfo()
        {
            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            return new UserInfoViewModel
            {
                Email = User.Identity.GetUserName(),
                HasRegistered = externalLogin == null,
                LoginProvider = externalLogin != null ? externalLogin.LoginProvider : null
            };
        }

        // POST api/Account/Logout
        [Route("Logout")]
        public IHttpActionResult Logout()
        {
            Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
            return Ok();
        }

        // GET api/Account/ManageInfo?returnUrl=%2F&generateState=true
        [Route("ManageInfo")]
        public async Task<ManageInfoViewModel> GetManageInfo(string returnUrl, bool generateState = false)
        {
            IdentityUser user = await UserManager.FindByIdAsync(User.Identity.GetUserId());

            if (user == null)
            {
                return null;
            }

            List<UserLoginInfoViewModel> logins = new List<UserLoginInfoViewModel>();

            foreach (IdentityUserLogin linkedAccount in user.Logins)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = linkedAccount.LoginProvider,
                    ProviderKey = linkedAccount.ProviderKey
                });
            }

            if (user.PasswordHash != null)
            {
                logins.Add(new UserLoginInfoViewModel
                {
                    LoginProvider = LocalLoginProvider,
                    ProviderKey = user.UserName,
                });
            }

            return new ManageInfoViewModel
            {
                LocalLoginProvider = LocalLoginProvider,
                Email = user.UserName,
                Logins = logins,
                ExternalLoginProviders = GetExternalLogins(returnUrl, generateState)
            };
        }

        // POST api/Account/ChangePassword
        [Route("ChangePassword")]
        public async Task<IHttpActionResult> ChangePassword(ChangePasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.ChangePasswordAsync(User.Identity.GetUserId(), model.OldPassword,
                model.NewPassword);
            
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/SetPassword
        [Route("SetPassword")]
        public async Task<IHttpActionResult> SetPassword(SetPasswordBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result = await UserManager.AddPasswordAsync(User.Identity.GetUserId(), model.NewPassword);

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/AddExternalLogin
        [Route("AddExternalLogin")]
        public async Task<IHttpActionResult> AddExternalLogin(AddExternalLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);

            AuthenticationTicket ticket = AccessTokenFormat.Unprotect(model.ExternalAccessToken);

            if (ticket == null || ticket.Identity == null || (ticket.Properties != null
                && ticket.Properties.ExpiresUtc.HasValue
                && ticket.Properties.ExpiresUtc.Value < DateTimeOffset.UtcNow))
            {
                return BadRequest("External login failure.");
            }

            ExternalLoginData externalData = ExternalLoginData.FromIdentity(ticket.Identity);

            if (externalData == null)
            {
                return BadRequest("The external login is already associated with an account.");
            }

            IdentityResult result = await UserManager.AddLoginAsync(User.Identity.GetUserId(),
                new UserLoginInfo(externalData.LoginProvider, externalData.ProviderKey));

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // POST api/Account/RemoveLogin
        [Route("RemoveLogin")]
        public async Task<IHttpActionResult> RemoveLogin(RemoveLoginBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            IdentityResult result;

            if (model.LoginProvider == LocalLoginProvider)
            {
                result = await UserManager.RemovePasswordAsync(User.Identity.GetUserId());
            }
            else
            {
                result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(),
                    new UserLoginInfo(model.LoginProvider, model.ProviderKey));
            }

            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogin
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalCookie)]
        [AllowAnonymous]
        [Route("ExternalLogin", Name = "ExternalLogin")]
        public async Task<IHttpActionResult> GetExternalLogin(string provider, string error = null)
        {
            if (error != null)
            {
                return Redirect(Url.Content("~/") + "#error=" + Uri.EscapeDataString(error));
            }

            if (!User.Identity.IsAuthenticated)
            {
                return new ChallengeResult(provider, this);
            }

            ExternalLoginData externalLogin = ExternalLoginData.FromIdentity(User.Identity as ClaimsIdentity);

            if (externalLogin == null)
            {
                return InternalServerError();
            }

            if (externalLogin.LoginProvider != provider)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                return new ChallengeResult(provider, this);
            }

            ApplicationUser user = await UserManager.FindAsync(new UserLoginInfo(externalLogin.LoginProvider,
                externalLogin.ProviderKey));

            bool hasRegistered = user != null;

            if (hasRegistered)
            {
                Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
                
                 ClaimsIdentity oAuthIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    OAuthDefaults.AuthenticationType);
                ClaimsIdentity cookieIdentity = await user.GenerateUserIdentityAsync(UserManager,
                    CookieAuthenticationDefaults.AuthenticationType);

                AuthenticationProperties properties = ApplicationOAuthProvider.CreateProperties(user.UserName);
                Authentication.SignIn(properties, oAuthIdentity, cookieIdentity);
            }
            else
            {
                IEnumerable<Claim> claims = externalLogin.GetClaims();
                ClaimsIdentity identity = new ClaimsIdentity(claims, OAuthDefaults.AuthenticationType);
                Authentication.SignIn(identity);
            }

            return Ok();
        }

        // GET api/Account/ExternalLogins?returnUrl=%2F&generateState=true
        [AllowAnonymous]
        [Route("ExternalLogins")]
        public IEnumerable<ExternalLoginViewModel> GetExternalLogins(string returnUrl, bool generateState = false)
        {
            IEnumerable<AuthenticationDescription> descriptions = Authentication.GetExternalAuthenticationTypes();
            List<ExternalLoginViewModel> logins = new List<ExternalLoginViewModel>();

            string state;

            if (generateState)
            {
                const int strengthInBits = 256;
                state = RandomOAuthStateGenerator.Generate(strengthInBits);
            }
            else
            {
                state = null;
            }

            foreach (AuthenticationDescription description in descriptions)
            {
                ExternalLoginViewModel login = new ExternalLoginViewModel
                {
                    Name = description.Caption,
                    Url = Url.Route("ExternalLogin", new
                    {
                        provider = description.AuthenticationType,
                        response_type = "token",
                        client_id = Startup.PublicClientId,
                        redirect_uri = new Uri(Request.RequestUri, returnUrl).AbsoluteUri,
                        state = state
                    }),
                    State = state
                };
                logins.Add(login);
            }

            return logins;
        }

        // POST api/Account/Register
        [AllowAnonymous]
        [Route("Register")]
        public async Task<IHttpActionResult> Register()
        {
			var httpRequest = HttpContext.Current.Request;

			RegisterBindingModel model = new RegisterBindingModel()
			{
				Email = httpRequest.Form["email"],
				Password = httpRequest.Form["password"],
				ConfirmPassword = httpRequest.Form["confirmPassword"],
				Name = httpRequest.Form["name"],
				LastName = httpRequest.Form["lastname"],
				Birthday = DateTime.Parse(httpRequest.Form["birthday"]),
				Address = httpRequest.Form["address"],
				RequestedUserType = httpRequest.Form["requestedUserType"]
			};

			this.Validate<RegisterBindingModel>(model);

			if (!ModelState.IsValid)
			{
				return BadRequest(ModelState);
			}

			string documentImageFileName = null;
			if (httpRequest.Files.Count > 0)
			{
				foreach (string file in httpRequest.Files)
				{
					var postedFile = httpRequest.Files[file];
					var filePath = HttpContext.Current.Server.MapPath("~/Resources/" + postedFile.FileName);
					postedFile.SaveAs(filePath);
					documentImageFileName = postedFile.FileName;
					break;
				}
			}

            UserType requestedUserType = unitOfWork.UserTypeRepository.Find(ut => ut.Name == model.RequestedUserType).FirstOrDefault();

            try
            {
                var user = new ApplicationUser()
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Name = model.Name,
                    LastName = model.LastName,
                    Birthday = model.Birthday,
                    Address = model.Address,
                    UserTypeId = requestedUserType.Id,
                    RegistrationStatus = RegistrationStatus.Processing,
                    DocumentImage = (String.IsNullOrWhiteSpace(documentImageFileName)) ? null : documentImageFileName,
                };

                IdentityResult result = await UserManager.CreateAsync(user, model.Password);
                UserManager.AddToRole(user.Id, "AppUser");

                // Registration confirmation email retry
                Task t = new Task(() =>
                {
                    int tries = 6;
                    var body = new StringBuilder();
                    body.Append("<html><head><body><a href=\"http://localhost:4200/confirmRegistration\">Confirm registration</a></body></head></html>");
                    while (!emailSender.SendMail("Registration confirmation", body.ToString(), user.Email))
                    {
                        tries--;
                        if (tries != 0)
                        {
                            Thread.Sleep(10000);
                        }
                        else
                        {
                            break;
                        }
                    }
                });

                try
                {
                    t.Start();
                }
                catch(Exception e)
                {
                    t.Start();
                }
                userProfileConfirmationHub.AddNewUser(user);

                if (!result.Succeeded)
                {
                    return GetErrorResult(result);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest();
            }
        }

        // GET api/Account/PendingUsers
        [Route("PendingUsers")]
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IHttpActionResult PendingUsers()
        {
            List<ApplicationUser> users = UserManager.Users.Include(x => x.UserType)
                .Where(user => user.RegistrationStatus == RegistrationStatus.Processing).ToList();
            List<ApplicationUserDto> userDtos = new List<ApplicationUserDto>(users.Count);

            foreach (var user in users)
            {
                userDtos.Add(user);
            }

            return Ok(userDtos);
        }

        // POST api/Account/ConfirmRegistration
        [Route("ConfirmRegistration")]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IHttpActionResult> ConfirmRegistration(RegisterConfirmationDto registerConfirmation)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindByName(registerConfirmation?.Email);
            try
            {
                if (user.RegistrationStatus == RegistrationStatus.Processing)
                {
                    user.RegistrationStatus = RegistrationStatus.Accepted;

                    IdentityResult result = await UserManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        return GetErrorResult(result);
                    }

                    userProfileConfirmationHub.ConfirmRegistration(user.Email);

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return InternalServerError();
            }
        }

        // POST api/Account/DeclineUser
        [Route("DeclineUser")]
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IHttpActionResult> DeclineUser(RegisterConfirmationDto registerConfirmation)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ApplicationUser user = UserManager.FindByName(registerConfirmation?.Email);
            try
            {
                if (user.RegistrationStatus == RegistrationStatus.Processing)
                {
                    user.RegistrationStatus = RegistrationStatus.Rejected;

                    IdentityResult result = await UserManager.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        return GetErrorResult(result);
                    }

                    userProfileConfirmationHub.DeclineRegistration(user.Email);

                    return Ok();
                }
                else
                {
                    return BadRequest();
                }
            }
            catch (Exception e)
            {
                return InternalServerError();
            }
        }

        // POST api/Account/RegisterExternal
        [OverrideAuthentication]
        [HostAuthentication(DefaultAuthenticationTypes.ExternalBearer)]
        [Route("RegisterExternal")]
        public async Task<IHttpActionResult> RegisterExternal(RegisterExternalBindingModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var info = await Authentication.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return InternalServerError();
            }

            var user = new ApplicationUser() { UserName = model.Email, Email = model.Email };

            IdentityResult result = await UserManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                return GetErrorResult(result);
            }

            result = await UserManager.AddLoginAsync(user.Id, info.Login);
            if (!result.Succeeded)
            {
                return GetErrorResult(result); 
            }
            return Ok();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        #region Helpers

        private IAuthenticationManager Authentication
        {
            get { return Request.GetOwinContext().Authentication; }
        }

        private IHttpActionResult GetErrorResult(IdentityResult result)
        {
            if (result == null)
            {
                return InternalServerError();
            }

            if (!result.Succeeded)
            {
                if (result.Errors != null)
                {
                    foreach (string error in result.Errors)
                    {
                        ModelState.AddModelError("", error);
                    }
                }

                if (ModelState.IsValid)
                {
                    // No ModelState errors are available to send, so just return an empty BadRequest.
                    return BadRequest();
                }

                return BadRequest(ModelState);
            }

            return null;
        }

        private class ExternalLoginData
        {
            public string LoginProvider { get; set; }
            public string ProviderKey { get; set; }
            public string UserName { get; set; }

            public IList<Claim> GetClaims()
            {
                IList<Claim> claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.NameIdentifier, ProviderKey, null, LoginProvider));

                if (UserName != null)
                {
                    claims.Add(new Claim(ClaimTypes.Name, UserName, null, LoginProvider));
                }

                return claims;
            }

            public static ExternalLoginData FromIdentity(ClaimsIdentity identity)
            {
                if (identity == null)
                {
                    return null;
                }

                Claim providerKeyClaim = identity.FindFirst(ClaimTypes.NameIdentifier);

                if (providerKeyClaim == null || String.IsNullOrEmpty(providerKeyClaim.Issuer)
                    || String.IsNullOrEmpty(providerKeyClaim.Value))
                {
                    return null;
                }

                if (providerKeyClaim.Issuer == ClaimsIdentity.DefaultIssuer)
                {
                    return null;
                }

                return new ExternalLoginData
                {
                    LoginProvider = providerKeyClaim.Issuer,
                    ProviderKey = providerKeyClaim.Value,
                    UserName = identity.FindFirstValue(ClaimTypes.Name)
                };
            }
        }

        private static class RandomOAuthStateGenerator
        {
            private static RandomNumberGenerator _random = new RNGCryptoServiceProvider();

            public static string Generate(int strengthInBits)
            {
                const int bitsPerByte = 8;

                if (strengthInBits % bitsPerByte != 0)
                {
                    throw new ArgumentException("strengthInBits must be evenly divisible by 8.", "strengthInBits");
                }

                int strengthInBytes = strengthInBits / bitsPerByte;

                byte[] data = new byte[strengthInBytes];
                _random.GetBytes(data);
                return HttpServerUtility.UrlTokenEncode(data);
            }
        }

        #endregion
    }
}
