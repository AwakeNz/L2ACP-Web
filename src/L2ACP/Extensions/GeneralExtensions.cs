﻿/*
 * Copyright (C) 2017  Nick Chapsas
 * This program is free software: you can redistribute it and/or modify it under
 * the terms of the GNU General Public License as published by the Free Software
 * Foundation, either version 2 of the License, or (at your option) any later
 * version.
 * 
 * L2ACP is distributed in the hope that it will be useful, but WITHOUT
 * ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
 * FOR A PARTICULAR PURPOSE. See the GNU General Public License for more
 * details.
 * 
 * You should have received a copy of the GNU General Public License along with
 * this program. If not, see <http://www.gnu.org/licenses/>.
 */
using System;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using L2ACP.Cryptography;
using L2ACP.Requests;
using L2ACP.Responses;
using L2ACP.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace L2ACP.Extensions
{
    public static class GeneralExtensions
    {
        private const string ApiUrl = "http://localhost:8000/api";
        public static string ToL2Password(this string str)
        {
            SHA1 shA1 = SHA1.Create();
            byte[] bytes = new ASCIIEncoding().GetBytes(str);
            str = Convert.ToBase64String(shA1.ComputeHash(bytes));
            return str;
        }

        public static string GetUsername(this HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                return context.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
            }
            return string.Empty;
        }

        public static async Task<T> SendPostRequest<T>(this L2Request req) where T : L2Response
        {
            var request = await new HttpClient().PostAsync(ApiUrl, new JsonContent(req));

            var result = AesCrypto.DecryptRijndael(await request.Content.ReadAsStringAsync(), Constants.Salt);

            var responseObject = JsonConvert.DeserializeObject<T>(result);
            return responseObject;
        }

        public static GetAccountInfoResponse GetAccountInfo(this HttpContext context)
        {
            return (GetAccountInfoResponse)context.Items["AccountInfo"];
        }

        public static void InjectInfoToContext(this HttpContext context, GetAccountInfoResponse accountInfo)
        {
            context.Items["AccountInfo"] = accountInfo;
        }

        public static GetAccountInfoResponse RetrieveAccountInfo(this HttpContext context)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                var username = context.User.Claims.FirstOrDefault(x => x.Type == "username").Value;
                var chars = context.RequestServices.GetRequiredService<IRequestService>().GetAccountInfo(username).Result;
                var allCharsResponse = chars as GetAccountInfoResponse;
                if (allCharsResponse?.ResponseCode == 200)
                {
                    return allCharsResponse;
                }
                return null;
            }
            return null;
        }

    }
}