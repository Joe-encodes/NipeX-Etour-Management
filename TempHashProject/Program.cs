using System;
using BCrypt.Net;

class Program
{
    static void Main()
    {
        string password = "testpassword";
        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
        Console.WriteLine(hashedPassword);
    }
}