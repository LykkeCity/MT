﻿using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Client.Bot
{
    internal static class MtUserHelper
    {
        public static async Task ApplicationInfo(string apiAddress)
        {
            var address = $"{apiAddress}/ApplicationInfo";

            var result = await address.GetJsonAsync();
            if (result.Error != null)
                throw new Exception(result.Error.Message);
        }
        public static async Task EmailVerification(string email, string apiAddress)
        {
            var address = $"{apiAddress}/EmailVerification";
            var Email = email;

            var result = await address.PostJsonAsync(
                new
                {
                    Email
                }).ReceiveJson();
            if (result.Error != null)
                throw new Exception(result.Error.Message);
        }
        public static async Task Registration(string email, string password, string apiAddress)
        {
            var address = $"{apiAddress}/Registration";

            var ClientInfo = "MT Test Bot";
            var ContactPhone = "";
            var Email = email;
            var FullName = "";
            var Hint = "MtBotHint";
            var Password = HashPass(password);            
            var result = await address.PostJsonAsync(
                new
                {
                    ClientInfo,
                    ContactPhone,
                    Email,
                    FullName,
                    Hint,
                    Password
                }).ReceiveJson();
            if (result.Error != null)
                throw new Exception(result.Error.Message);

        }

        private static string HashPass(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                // Send text to hash.
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));

                // Get the hashed string.
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();

                // Print the string. 
                Console.WriteLine(hash);
                return hash;
            }
        }
    }
}
