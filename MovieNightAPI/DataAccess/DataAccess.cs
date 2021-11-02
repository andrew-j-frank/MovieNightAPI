using Dapper;
using Microsoft.Extensions.Configuration;
using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MovieNightAPI.DataAccess
{
    public class DataAccess : IDataAccess
    {
        private readonly IConfiguration _config;

        #region Constructor

        public DataAccess(IConfiguration config)
        {
            this._config = config;
        }

        #endregion

        #region Login

        public DataAccessResult Login(Login login)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var users = connection.Query<UserDB>($"select * from Users where username = @username", new { username = login.username }).ToList();
                    if (users.Count == 1)
                    {
                        var user = users.First();
                        if (user.password == GenerateSaltedHash(login.password, user.salt))
                        {
                            return new DataAccessResult()
                            {
                                returnObject = User.UserDBToUser(user)
                            };
                        }
                        else
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 404,
                                message = "username and password combination not found"
                            };
                        }
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "multiple users with this username"
                        };
                    }
                }
                catch (SqlException ex)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        #endregion

        #region SignUp

        public DataAccessResult SignUp(SignUp signUp)
        {
            var salt = GenerateSalt();
            var hashedPassword = GenerateSaltedHash(signUp.password, salt);
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into Users (username,password,salt,email) values (@username,@password,@salt,@email)", new { username = signUp.username, password = hashedPassword, salt = salt, email = signUp.email });
                    if (rows == 1)
                    {
                        var users = connection.Query<User>($"select * from Users where username = @username", new { username = signUp.username }).ToList();
                        if (users.Count == 1)
                        {
                            return new DataAccessResult()
                            {
                                returnObject = users.First()
                            };
                        }
                        else
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "multiple users with this username"
                            };
                        }
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "multiple rows changed. THIS SHOULD NEVER HAPPEN"
                        };
                    }
                }
                catch (SqlException ex)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        #endregion

        #region Login Signup Helpers

        private string GenerateSaltedHash(string password, string salt)
        {
            // Use SHA256 hashing algorith
            using (HashAlgorithm algorithm = new SHA256Managed())
            {
                // Combine the password and salt
                string passwordWithSalt = password + salt;
                // Get the bytes for the password and salt
                byte[] bytes = Encoding.ASCII.GetBytes(passwordWithSalt);
                // Get the hashed bytes
                byte[] hash = algorithm.ComputeHash(bytes);
                // Convert back to string
                return Encoding.ASCII.GetString(hash);
            }
        }

        private string GenerateSalt()
        {
            // Use a cryptographic random byte generator
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                // Create byte array to populate
                byte[] randomBytes = new byte[32];
                // Fill with random bytes
                rngCsp.GetBytes(randomBytes);
                // Convert to string
                return Encoding.ASCII.GetString(randomBytes);
            }
        }

        #endregion
    }
}
