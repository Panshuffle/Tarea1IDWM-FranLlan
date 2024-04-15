using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using courses_dotnet_api.Src.DTOs.Account;
using courses_dotnet_api.Src.Interfaces;
using courses_dotnet_api.Src.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace courses_dotnet_api.Src.Data;

public class AccountRepository : IAccountRepository
{
    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;
    private readonly ITokenService _tokenService;

    public AccountRepository(DataContext dataContext, IMapper mapper, ITokenService tokenService)
    {
        _dataContext = dataContext;
        _mapper = mapper;
        _tokenService = tokenService;
    }

    public async Task AddAccountAsync(RegisterDto registerDto)
    {
        using var hmac = new HMACSHA512();

        User user =
            new()
            {
                Rut = registerDto.Rut,
                Name = registerDto.Name,
                Email = registerDto.Email,
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

        await _dataContext.Users.AddAsync(user);
    }

    public async Task<AccountDto?> GetAccountAsync(string email)
    {
        User? user = await _dataContext
            .Users.Where(student => student.Email == email)
            .FirstOrDefaultAsync();

        if (user == null)
        {
            return null;
        }

        AccountDto accountDto =
            new()
            {
                Rut = user.Rut,
                Name = user.Name,
                Email = user.Email,
                Token = _tokenService.CreateToken(user.Rut)
            };

        return accountDto;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return 0 < await _dataContext.SaveChangesAsync();
    }

    public async Task<bool> VerifyLogin(LoginDto loginDto)
    {

        User? user = await _dataContext.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

        if (user == null)
        {
            return false;
        }

        // Generar el HMAC de la contraseña ingresada con el salt almacenado en la base de datos
        byte[] hashedPassword = GenerateHMAC(loginDto.Password, user.PasswordSalt);

        // Convertir los arrays de bytes a cadenas de caracteres hexadecimal para compararlos
        string storedPasswordHash = BitConverter.ToString(user.PasswordHash).Replace("-", "").ToLower();
        string enteredPasswordHash = BitConverter.ToString(hashedPassword).Replace("-", "").ToLower();

        // Comparar las cadenas de caracteres hexadecimal
        return storedPasswordHash == enteredPasswordHash;

        //Seré honesto, usé chatGPT para saber cómo comprar la contraseña ingresada con la almacenada,
        //ya que no sabía como hacerlo y no entendía lo que busqué en internet, sin embargo, entendí cómo
        //funciona lo que está, y para no olvidar prefiero dejar comentado lo que hizo la IA.
    }

    // Función para hashear la contraseña con HMAC-SHA512 y un salt
    private byte[] GenerateHMAC(string password, byte[] salt)
    {
        using (var hmac = new HMACSHA512(salt))
        {
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        }
    }
}
