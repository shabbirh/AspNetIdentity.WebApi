﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using AspNetIdentity.WebApi.Entities;
using AspNetIdentity.WebApi.Helpers;
using AspNetIdentity.WebApi.Repositories;
using Microsoft.Owin.Security.Infrastructure;

namespace AspNetIdentity.WebApi.Providers
{
    public class SimpleRefreshTokenProvider : IAuthenticationTokenProvider
    {
        public void Create(AuthenticationTokenCreateContext context)
        {
            throw new NotImplementedException();
        }

        public async Task CreateAsync(AuthenticationTokenCreateContext context)
        {
            try
            {
                var clientid = context.Ticket.Properties.Dictionary["as:client_id"];

                if (string.IsNullOrEmpty(clientid))
                {
                    return;
                }

                var refreshTokenId = Guid.NewGuid().ToString("n");

                using (AuthRepository _repo = new AuthRepository())
                {
                    var refreshTokenLifeTime = context.OwinContext.Get<string>("as:clientRefreshTokenLifeTime");

                    var token = new RefreshToken()
                    {
                        Id = HashingHelper.GetHash(refreshTokenId),
                        ClientId = clientid,
                        Subject = context.Ticket.Identity.Name,
                        IssuedUtc = DateTime.UtcNow,
                        ExpiresUtc = DateTime.UtcNow.AddMinutes(Convert.ToDouble(refreshTokenLifeTime))
                    };

                    context.Ticket.Properties.IssuedUtc = token.IssuedUtc;
                    context.Ticket.Properties.ExpiresUtc = token.ExpiresUtc;

                    token.ProtectedTicket = context.SerializeTicket();

                    var result = await _repo.AddRefreshToken(token);

                    if (result)
                    {
                        context.SetToken(refreshTokenId);
                    }

                }
            }
            catch (Exception)
            {
                return; 
            }
        }

        public void Receive(AuthenticationTokenReceiveContext context)
        {
            throw new NotImplementedException();
        }

        public async Task ReceiveAsync(AuthenticationTokenReceiveContext context)
        {
            var allowedOrigin = context.OwinContext.Get<string>("as:clientAllowedOrigin");
            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { allowedOrigin });

            string hashedTokenId = HashingHelper.GetHash(context.Token);

            using (AuthRepository _repo = new AuthRepository())
            {
                var refreshToken = await _repo.FindRefreshToken(hashedTokenId);

                if (refreshToken != null)
                {
                    //Get protectedTicket from refreshToken class
                    context.DeserializeTicket(refreshToken.ProtectedTicket);
                    var result = await _repo.RemoveRefreshToken(hashedTokenId);
                }
            }
        }
    }
}