using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using BCrypt.Net;

namespace WpfApp2
{
    public static class DatabaseManager
    {
        private static readonly string DbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "firmas.db");
        private static readonly byte[] Key = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0,
                                                0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };
        private static readonly byte[] IV = { 0x01, 0x23, 0x45, 0x67, 0x89, 0xAB, 0xCD, 0xEF, 0x12, 0x34, 0x56, 0x78, 0x9A, 0xBC, 0xDE, 0xF0 };

        static DatabaseManager()
        {
            if (!File.Exists(DbPath))
                CrearBaseDeDatos();
        }

        private static void CrearBaseDeDatos()
        {
            // 1. Crea el archivo
            SQLiteConnection.CreateFile(DbPath);

            // 2. Abre conexión y crea tabla
            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();

                // Usa CREATE TABLE IF NOT EXISTS para evitar errores
                string sql = @"
            CREATE TABLE IF NOT EXISTS Usuarios (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Nombre TEXT NOT NULL,
                Rol TEXT NOT NULL,
                HashContrasena TEXT NOT NULL,
                FirmaEncriptada BLOB NOT NULL
            );";

                using (var cmd = new SQLiteCommand(sql, conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public static List<Usuario> ObtenerUsuarios()
        {
            // Si el archivo no existe, créalo
            if (!File.Exists(DbPath))
            {
                CrearBaseDeDatos();
            }

            var lista = new List<Usuario>();
            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT Id, Nombre, Rol FROM Usuarios";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(new Usuario
                        {
                            Id = Convert.ToInt32(reader["Id"]),
                            Nombre = reader["Nombre"].ToString(),
                            Rol = reader["Rol"].ToString()
                        });
                    }
                }
            }
            return lista;
        }

        public static byte[] ObtenerFirma(int usuarioId, string contrasena)
        {
            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT HashContrasena, FirmaEncriptada FROM Usuarios WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", usuarioId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        string hash = reader.GetString(0);
                        if (BCrypt.Net.BCrypt.Verify(contrasena, hash))
                        {
                            return Desencriptar((byte[])reader["FirmaEncriptada"]);
                        }
                    }
                }
            }
            return null;
        }

        public static void AgregarUsuario(string nombre, string rol, string contrasena, byte[] firmaPng)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(contrasena);
            var encriptado = Encriptar(firmaPng);

            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Usuarios (Nombre, Rol, HashContrasena, FirmaEncriptada) VALUES (@n, @r, @h, @f)";
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@r", rol);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@f", encriptado);
                cmd.ExecuteNonQuery();
            }
        }

        private static byte[] Encriptar(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var encryptor = aes.CreateEncryptor())
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }

        public static void ActualizarUsuario(int id, string nombre, string rol, string nuevaContrasena, byte[] nuevaFirma)
        {
            var hash = BCrypt.Net.BCrypt.HashPassword(nuevaContrasena);
            var encriptado = Encriptar(nuevaFirma);

            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = @"
            UPDATE Usuarios 
            SET Nombre = @n, Rol = @r, HashContrasena = @h, FirmaEncriptada = @f 
            WHERE Id = @id";
                cmd.Parameters.AddWithValue("@n", nombre);
                cmd.Parameters.AddWithValue("@r", rol);
                cmd.Parameters.AddWithValue("@h", hash);
                cmd.Parameters.AddWithValue("@f", encriptado);
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        public static void EliminarUsuario(int id)
        {
            using (var conn = new SQLiteConnection($"Data Source={DbPath}"))
            {
                conn.Open();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM Usuarios WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                cmd.ExecuteNonQuery();
            }
        }

        private static byte[] Desencriptar(byte[] data)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Key;
                aes.IV = IV;
                using (var decryptor = aes.CreateDecryptor())
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
            }
        }
    }
}