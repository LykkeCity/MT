using Flurl.Http;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace MarginTrading.Client
{
    static class MtUserHelper
    {
        public static async Task ApplicationInfo(string apiAddress)
        {
            //https://api-dev.lykkex.net/api/ApplicationInfo
            
            string address = $"{apiAddress}/ApplicationInfo";
            try
            {
                var result = await address.GetJsonAsync();
                if (result.Error != null)
                    throw new Exception(result.Error.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            

            


        }
        public static async Task EmailVerification(string email, string apiAddress)
        {
            //https://api-test.lykkex.net/api/EmailVerification
            string address = $"{apiAddress}/EmailVerification";
            string Email = email;
            try
            {
                var result = await address.PostJsonAsync(
                    new
                    {
                        Email
                    }).ReceiveJson();
                if (result.Error != null)
                    throw new Exception(result.Error.Message);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static async Task Registration(string email, string password, string apiAddress)
        {
            //https://api-dev.lykkex.net/api/Registration

            string address = $"{apiAddress}/Registration";

            string ClientInfo = "MT Test Bot";
            string ContactPhone = "";
            string Email = email;
            string FullName = "";
            string Hint = "MtBotHint";
            string Password = HasPass(password);            
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

        private static string HasPass(string password)
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
