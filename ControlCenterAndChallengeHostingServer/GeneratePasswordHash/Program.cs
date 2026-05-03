using System;
using System.Security.Cryptography;
using System.Text;
using ResourceShared.Utils;

namespace GeneratePasswordHash
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== FCTF Password Hash Generator (Passlib Format) ===\n");

            if (args.Length > 0)
            {
                // Generate hash for provided password
                string password = args[0];
                string hash = SHA256Helper.HashPasswordPythonStyle(password);
                Console.WriteLine($"Password: {password}");
                Console.WriteLine($"Hash: {hash}");
            }
            else
            {
                // Interactive mode
                Console.Write("Enter password to hash: ");
                string? password = Console.ReadLine();

                if (string.IsNullOrEmpty(password))
                {
                    Console.WriteLine("Error: Password cannot be empty");
                    return;
                }

                string hash = SHA256Helper.HashPasswordPythonStyle(password);
                Console.WriteLine($"\nPassword: {password}");
                Console.WriteLine($"Hash: {hash}");
                Console.WriteLine("\nCopy this hash to your SQL file!");
            }

            Console.WriteLine("\n=== Common Test Passwords ===");
            GenerateCommonPasswords();
        }

        static void GenerateCommonPasswords()
        {
            string[] passwords = { "password123", "admin123", "test123" };

            foreach (var pwd in passwords)
            {
                string hash = SHA256Helper.HashPasswordPythonStyle(pwd);
                Console.WriteLine($"{pwd,-15} => {hash}");
            }
        }
    }
}
