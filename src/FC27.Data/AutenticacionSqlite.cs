using System.Security.Cryptography;
using FC27.Core;
using Microsoft.EntityFrameworkCore;

namespace FC27.Data;

/// <summary>Alta con invitación e inicio de sesión de organizadores sin servicios externos.</summary>
public sealed class AutenticacionSqlite(TorneoDbContext db)
{
    private const int Iteraciones = 210_000;
    private const int MinimoContrasena = 8;

    /// <summary>Crea una invitación de un solo uso y devuelve su código.</summary>
    public async Task<string> CrearInvitacionAsync(CancellationToken ct)
    {
        var codigo = Convert.ToHexString(RandomNumberGenerator.GetBytes(5));
        db.Invitaciones.Add(new Invitacion(0, codigo, null));
        await db.SaveChangesAsync(ct);
        return codigo;
    }

    /// <summary>Registra un organizador consumiendo su invitación en una sola transacción.</summary>
    public async Task<Organizador> RegistrarAsync(string usuario, string contrasena, string codigoInvitacion, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(usuario);
        if (contrasena is null || contrasena.Length < MinimoContrasena)
            throw new ArgumentException($"La contraseña necesita al menos {MinimoContrasena} caracteres.", nameof(contrasena));
        var nombre = usuario.Trim().ToLowerInvariant();

        await using var tx = await db.Database.BeginTransactionAsync(ct);
        var codigo = codigoInvitacion?.Trim().ToUpperInvariant();
        var invitacion = await db.Invitaciones.SingleOrDefaultAsync(i => i.Codigo == codigo && i.UsadaPorId == null, ct)
            ?? throw new InvalidOperationException("La invitación no es válida o ya se usó.");
        var sal = RandomNumberGenerator.GetBytes(16);
        var organizador = new Organizador(0, nombre, Derivar(contrasena, sal), sal);
        db.Organizadores.Add(organizador);
        await db.SaveChangesAsync(ct);
        db.Entry(invitacion).CurrentValues.SetValues(invitacion with { UsadaPorId = organizador.Id });
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return organizador;
    }

    /// <summary>Devuelve el organizador si las credenciales son correctas, o null. No revela cuál falló.</summary>
    public async Task<Organizador?> EntrarAsync(string usuario, string contrasena, CancellationToken ct)
    {
        var nombre = (usuario ?? "").Trim().ToLowerInvariant();
        var organizador = await db.Organizadores.AsNoTracking().SingleOrDefaultAsync(o => o.Usuario == nombre, ct);
        // Se deriva siempre para que el tiempo no delate si el usuario existe.
        var sal = organizador?.Sal ?? new byte[16];
        var hash = Derivar(contrasena ?? "", sal);
        return organizador is not null && CryptographicOperations.FixedTimeEquals(hash, organizador.Hash) ? organizador : null;
    }

    private static byte[] Derivar(string contrasena, byte[] sal) =>
        Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
}
