using System;

namespace Fitbit.Api.Portable
{
    public interface ITokenStore
    {
        string Read();

        void Write(string bearerToken, string refreshToken, DateTime expiration); // Maybe a TimeSpan instead? 
    }
}