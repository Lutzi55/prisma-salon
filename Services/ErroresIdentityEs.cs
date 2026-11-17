using Microsoft.AspNetCore.Identity;

public class ErroresIdentityEs : IdentityErrorDescriber
{
    public override IdentityError DefaultError() => E(nameof(DefaultError), "Ocurrió un error inesperado.");
    public override IdentityError DuplicateUserName(string u) => E(nameof(DuplicateUserName), $"El correo '{u}' ya está registrado.");
    public override IdentityError DuplicateEmail(string e) => E(nameof(DuplicateEmail), $"El correo '{e}' ya está registrado.");
    public override IdentityError InvalidEmail(string? e) => E(nameof(InvalidEmail), "El correo no es válido.");
    public override IdentityError InvalidUserName(string? u) => E(nameof(InvalidUserName), "El nombre de usuario no es válido.");
    public override IdentityError PasswordMismatch() => E(nameof(PasswordMismatch), "La contraseña es incorrecta.");
    public override IdentityError PasswordTooShort(int l) => E(nameof(PasswordTooShort), $"La contraseña debe tener al menos {l} caracteres.");
    public override IdentityError PasswordRequiresDigit() => E(nameof(PasswordRequiresDigit), "La contraseña debe incluir al menos un número.");
    public override IdentityError PasswordRequiresLower() => E(nameof(PasswordRequiresLower), "La contraseña debe incluir al menos una minúscula.");
    public override IdentityError PasswordRequiresUpper() => E(nameof(PasswordRequiresUpper), "La contraseña debe incluir al menos una mayúscula.");

    private static IdentityError E(string code, string desc) => new() { Code = code, Description = desc };
}