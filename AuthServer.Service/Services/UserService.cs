using AuthServer.Core.Dtos;
using AuthServer.Core.Models;
using AuthServer.Core.Services;
using AuthServer.Service.DtoMappers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using SharedLibrary.Dtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServer.Service.Services
{
    public class UserService : IUserService
    {
        private readonly UserManager<UserApp> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserService(UserManager<UserApp> userManager, RoleManager<IdentityRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<Response<UserAppDto>> CreateUserAsync(CreateUserDto createUserDto)
        {
            var user = new UserApp { Email = createUserDto.Email, UserName = createUserDto.UserName };
            var result = await _userManager.CreateAsync(user, createUserDto.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(x => x.Description).ToList();
                return Response<UserAppDto>.Fail(new ErrorDto(errors, true), 400);
            }
            return Response<UserAppDto>.Success(ObjectMapper.Mapper.Map<UserAppDto>(user), 200);
        }
        public async Task<Response<UserAppDto>> GetUserByNameAsync(string UserName)
        {
            var user = await _userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                return Response<UserAppDto>.Fail("Username not found.", 404, true);
            }
            return Response<UserAppDto>.Success(ObjectMapper.Mapper.Map<UserAppDto>(user), 200);
        }
        public async Task<Response<NoContent>> CreateUserRoles(CreateRoleDto createRoleDto)
        {

            var user = await _userManager.FindByEmailAsync(createRoleDto.Email); // You can use Id or Username to find record instead of email.
            if (user is null)
            {
                return Response<NoContent>.Fail("User not found!", 404, true);
            }

            if (!await _roleManager.RoleExistsAsync(createRoleDto.Role))
            {
                await _roleManager.CreateAsync(new IdentityRole { Name = createRoleDto.Role });
            }
            await _userManager.AddToRoleAsync(user, createRoleDto.Role);
            return Response<NoContent>.Success(StatusCodes.Status201Created);
        }
    }
}
