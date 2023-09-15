﻿using Microsoft.AspNetCore.Mvc;
using DatabaseConfigClassLibrary.DTO;
using DatabaseConfigClassLibrary;
using Microsoft.AspNetCore.Authorization;
using CustomerDetailsManagementApp.Services;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace CustomerDetailsManagementApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [ApiVersion("1.0")]
    public class UserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly LoginService _loginService;
        private readonly EditUserService _editUserService;
        private readonly GetDistanceService _getDistanceService;
        private readonly SearchUserService _searchUserService;
        private readonly GetCustomerListByZipCodeService _getCustomerListService;
        private readonly GetAllCustomerListService _getAllCustomerListService;

        public UserController(
            ApplicationDbContext context,
            IConfiguration configuration,
            LoginService loginService,
            EditUserService editUserService,
            GetDistanceService getDistanceService,
            SearchUserService searchUserService,
            GetCustomerListByZipCodeService getCustomerListService,
            GetAllCustomerListService getAllCustomerListService
        )
        {
            _context = context;
            _configuration = configuration;
            _loginService = loginService;
            _editUserService = editUserService;
            _getDistanceService = getDistanceService;
            _searchUserService = searchUserService;
            _getCustomerListService = getCustomerListService;
            _getAllCustomerListService = getAllCustomerListService;
        }

        [HttpPost]
        [MapToApiVersion("1.0")]
        [Route("Login")]
        [Route("v{version:apiVersion}/Login")]
        public IActionResult Login([FromBody] LoginDTO loginDTO)
        {
            var (token, role) = _loginService.AuthenticateUser(
                loginDTO.Username,
                loginDTO.Password
            );

            if (token != null)
            {
                return Ok(new { access_token = token });
            }

            return Unauthorized("Invalid username or password");
        }

        // PUT api/User/EditUser/{Id}
        [Authorize(Policy = "UserOrAdminPolicy")]
        [HttpPut]
        [MapToApiVersion("1.0")]
        [Route("EditUser/{Id}")]
        [Route("v{version:apiVersion}/EditUser/{Id}")]
        public async Task<IActionResult> EditUser(string _id, [FromBody] UserUpdateDTO userUpdate)
        {
            var (success, message) = await _editUserService.EditUserAsync(_id, userUpdate);

            if (success)
            {
                return Ok(message);
            }

            return BadRequest(message);
        }

        //GET api/User/GetDistance/Id?Latitude=value&Longitude=value
        [Authorize(Policy = "UserOrAdminPolicy")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [Route("GetDistance/{Id}")]
        [Route("v{version:apiVersion}/GetDistance/{Id}")]
        public IActionResult GetDistance(string _id, double latitude, double longitude)
        {
            try
            {
                var user = _context.UserDatas.FirstOrDefault(u => u.Id == _id);

                if (user == null)
                {
                    return NotFound();
                }

                if (user.Latitude.HasValue && user.Longitude.HasValue)
                {
                    double userLatitude = user.Latitude.Value;
                    double userLongitude = user.Longitude.Value;

                    double distanceInKilometers = _getDistanceService.CalculateDistance(
                        userLatitude,
                        userLongitude,
                        latitude,
                        longitude
                    );

                    return Ok(distanceInKilometers);
                }
                else
                {
                    return BadRequest("User's Latitude or Longitude is missing.");
                }
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        //GET api/User/SearchUser?searchText=text to search
        [Authorize(Policy = "UserOrAdminPolicy")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [Route("SearchUser")]
        [Route("v{version:apiVersion}/SearchUser")]
        public IActionResult SearchUser(string searchText)
        {
            try
            {
                var matchedUsers = _searchUserService.SearchUsers(searchText);
                return Ok(matchedUsers);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        //GET api/User/GetCustomerListByZipCode
        [Authorize(Policy = "UserOrAdminPolicy")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [Route("GetCustomerListByZipCode")]
        [Route("v{version:apiVersion}/GetCustomerListByZipCode")]
        public IActionResult GetCustomerListByZipCode()
        {
            try
            {
                var result = _getCustomerListService.GetCustomersByZipCode();
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        //GET api/User/GetAllCustomerList
        [Authorize(Policy = "AdminPolicy")]
        [HttpGet]
        [MapToApiVersion("1.0")]
        [Route("GetAllCustomerList")]
        [Route("v{version:apiVersion}/GetAllCustomerList")]
        public IActionResult GetAllCustomerList()
        {
            try
            {
                var customerList = _getAllCustomerListService.GetAllCustomersAndAddresses();
                return Ok(customerList);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }
    }
}
