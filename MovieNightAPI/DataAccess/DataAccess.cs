using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
                            var token = GenerateToken(user);
                            return new DataAccessResult()
                            {
                                returnObject = LoginSignUpUser.UserDBToUser(user, token)
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
                    var rows = connection.Execute($"insert into users (username,password,salt,email) values (@username,@password,@salt,@email)", new { username = signUp.username, password = hashedPassword, salt = salt, email = signUp.email });
                    if (rows == 1)
                    {
                        var users = connection.Query<UserDB>($"select * from users where username = @username", new { username = signUp.username }).ToList();
                        if (users.Count == 1)
                        {
                            var user = users.First();
                            var token = GenerateToken(user);
                            return new DataAccessResult()
                            {
                                returnObject = LoginSignUpUser.UserDBToUser(user, token)
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

        private string GenerateToken(UserDB user)
        {
            // reference: https://stackoverflow.com/a/63446357

            var claims = new[]
            {
                new Claim("user_id", user.user_id.ToString()),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AuthSettings:Key"]));
            var token = new JwtSecurityToken(
                issuer: _config["AuthSettings:Issuer"],
                audience: _config["AuthSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(30),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
                );
            string tokenString = new JwtSecurityTokenHandler().WriteToken(token);
            return tokenString;
        }

        #endregion

        #region Group

        // group
        public DataAccessResult CreateGroup(GroupJoin group)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    string group_code = "";
                    int num_groups = connection.QuerySingle<int>($"select count(*) from groups");
                    if (num_groups == 0)
                    {
                        // Since this is the first group, simply generate a group code
                        group_code = GenerateGroupCode();
                    }
                    else
                    {
                        // Get list of all group codes
                        IEnumerable<string> group_codes = connection.Query<string>($"select group_code from groups");

                        // Generate group code until group code is unique
                        group_code = GenerateGroupCode();
                        while (group_codes.Contains(group_code))
                        {
                            group_code = GenerateGroupCode();
                        }
                    }

                    // Create group and get the generated group_id
                    var group_id = connection.QuerySingle<int>($"insert into groups (group_code,group_name,created_by,max_user_movies) OUTPUT INSERTED.group_id values (@group_code,@group_name,@created_by,5)", new { group_code = group_code, group_name = group.group_name, created_by = group.created_by });
                    group.group_id = group_id;
                    group.group_code = group_code;
                    return new DataAccessResult()
                    {
                        returnObject = group
                    };
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

        // group
        public DataAccessResult JoinGroupCreator(GroupJoin group)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    int exists = connection.QuerySingle<int>($"select count(*) from groups where group_code = @group_code", new { group_code = group.group_code });
                    if (exists <= 0)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 404,
                            message = "Group does not exist"
                        };
                    }
                    int group_id = connection.QuerySingle<int>($"select group_id from groups where group_code = @group_code", new { group_code = group.group_code });
                    var rows = connection.Execute($"insert into group_users (group_id,user_id,alias,is_admin) values (@group_id,@user_id,@alias,@is_admin)", new { group_id = group_id, user_id = group.created_by, alias = group.alias, is_admin = true });
                    if (rows == 1)
                    {
                        return new DataAccessResult()
                        {
                            returnObject = group
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "User could not be added to group. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        // group
        public DataAccessResult AddGroupMovie(GroupMovies group_movies)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get number of movies added by user
                    int num_movies = connection.QuerySingle<int>($"select count(*) from group_movies where group_id = @group_id and added_by = @added_by", new { group_id = group_movies.group_id, added_by = group_movies.added_by });
                    // Get max number of movies per user in this group
                    int max_movies = connection.QuerySingle<int>($"select max_user_movies from groups where group_id = @group_id", new { group_id = group_movies.group_id });

                    // Check if user has added their maximum number of movies
                    if (num_movies >= max_movies)
                    {
                        // The user has exceeded their limit for movies, this movie will not be added to the group
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The user has already submitted their maximum number of movies for this group."
                        };
                    }

                    // Check if this movie has already been added to the group
                    int exists = connection.QuerySingle<int>($"select count(*) from group_movies where group_id = @group_id and tmdb_movie_id = @tmdb_movie_id", new { group_id = group_movies.group_id, tmdb_movie_id = group_movies.tmdb_movie_id });
                    if (exists > 0)
                    {
                        // This movie has already been added to the group
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The given movie has already been added to the group."
                        };
                    }

                    var rows = connection.Execute($"insert into group_movies (group_id,tmdb_movie_id,added_by) values (@group_id,@tmdb_movie_id,@added_by)", new { group_id = group_movies.group_id, tmdb_movie_id = group_movies.tmdb_movie_id, added_by = group_movies.added_by });
                    if (rows == 1)
                    {
                        // Now, add a rating of 5 for the movie for each individual in the group
                        // Get a list of all users in the group
                        IEnumerable<int> users = connection.Query<int>($"select user_id from group_users where group_id = @group_id", new { group_id = group_movies.group_id });

                        // For each user in the group, add a rating of 5
                        foreach (int user_id in users)
                        {
                            // Check if the rating exists for this user
                            int exists2 = connection.QuerySingle<int>($"select count(*) from group_movie_ratings where user_id = @user_id and group_id = @group_id and tmdb_movie_id = @tmdb_movie_id", new { user_id = user_id, group_id = group_movies.group_id, tmdb_movie_id = group_movies.tmdb_movie_id });
                            if (exists2 <= 0)
                            {
                                rows = connection.Execute($"insert into group_movie_ratings (group_id,user_id,tmdb_movie_id,rating) values (@group_id,@user_id,@tmdb_movie_id,5)", new { group_id = group_movies.group_id, user_id = user_id, tmdb_movie_id = group_movies.tmdb_movie_id });
                                if (rows != 1)
                                {
                                    return new DataAccessResult()
                                    {
                                        error = true,
                                        statusCode = 500,
                                        message = "multiple rows changed. THIS SHOULD NEVER HAPPEN"
                                    };
                                }
                            }
                        }
                        
                        return new DataAccessResult()
                        {
                            returnObject = group_movies
                        };

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
                        // Likely, this movie already exists within this group
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        // group
        public DataAccessResult RemoveMovie(int group_id, int tmdb_movie_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    GroupMovies movie = connection.QuerySingle<GroupMovies>($"select * from group_movies where group_id = @group_id and tmdb_movie_id = @tmdb_movie_id", new { group_id = group_id, tmdb_movie_id = tmdb_movie_id });
                    var rows = connection.Execute($"delete from group_movies where group_id = @group_id and tmdb_movie_id = @tmdb_movie_id", new { group_id = group_id, tmdb_movie_id = tmdb_movie_id });
                    if (rows == 1)
                    {
                        return new DataAccessResult()
                        {
                            returnObject = movie
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The specified movie was not in the group's queue."
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
                catch (InvalidOperationException ex)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message + ". Ensure that the given movie was in the group's queue."
                    };
                }
            }
        }

        // group
        public DataAccessResult GetMovies(int group_id, int user_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<GroupMovieRating> movies = connection.Query<GroupMovieRating>($"select gm.added_by, gm.group_id, gm.tmdb_movie_id, avg(cast(rating as float)) as avg_user_rating from group_movies gm inner join group_movie_ratings gmr on gm.group_id = gmr.group_id and gm.tmdb_movie_id = gmr.tmdb_movie_id where gm.group_id = @group_id group by gm.group_id, gm.tmdb_movie_id, gm.added_by", new { group_id = group_id });
                    foreach (var movie in movies)
                    {
                        int rating = connection.QuerySingle<int>($"select rating from group_movie_ratings where group_id = @group_id and user_id = @user_id and tmdb_movie_id = @tmdb_movie_id", new { group_id = group_id, user_id = user_id, tmdb_movie_id = movie.tmdb_movie_id });
                        movie.user_rating = rating;
                    }
                    return new DataAccessResult()
                    {
                        returnObject = movies
                    };

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

        // group
        public DataAccessResult GetUsers(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<GroupUserDB> groups = connection.Query<GroupUserDB>($"select * from group_users where group_id = @group_id", new { group_id = group_id });
                    List<GroupUser> users = new List<GroupUser>();
                    foreach (GroupUserDB group in groups)
                    {
                        if (group.alias == null)
                        {
                            group.alias = connection.QuerySingle<string>($"select username from users where user_id = @user_id", new { user_id = group.user_id });
                        }
                        users.Add(GroupUser.GroupUserDBToGroupUser(group));
                    }
                    return new DataAccessResult()
                    {
                        returnObject = users
                    };
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

        // group
        public DataAccessResult DeleteGroup(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    Group group = connection.QuerySingle<Group>($"select * from groups where group_id = @group_id", new { group_id = group_id });
                    connection.Execute($"delete from group_users where group_id = @group_id", new { group_id = group_id });
                    connection.Execute($"delete from events where group_id = @group_id", new { group_id = group_id });
                    connection.Execute($"delete from group_movies where group_id = @group_id", new { group_id = group_id });
                    connection.Execute($"delete from group_movie_ratings where group_id = @group_id", new { group_id = group_id });
                    connection.Execute($"delete from groups where group_id = @group_id", new { group_id = group_id });
                    return new DataAccessResult()
                    {
                        returnObject = group
                    };

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
                catch (InvalidOperationException ex)
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

        // group
        public DataAccessResult GetGroup(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    Group group = connection.QuerySingle<Group>($"select * from groups where group_id = @group_id", new { group_id = group_id });
                    return new DataAccessResult()
                    {
                        returnObject = group
                    };

                }
                catch (Exception ex)
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

        // group
        public DataAccessResult ChangeMaxMovies(int group_id, int max_user_movies)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                if (max_user_movies < 0)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        message = "Max movies cannot be a negative value."
                    };
                }

                try
                {
                    var rows = connection.Execute($"update groups set max_user_movies = @max_user_movies where group_id = @group_id;", new { max_user_movies = max_user_movies, group_id = group_id });
                    if (rows == 1)
                    {
                        var group = connection.QuerySingle<Group>($"select * from groups where group_id = @group_id", new { group_id = group_id });
                        return new DataAccessResult()
                        {
                            returnObject = group
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "is_admin could not be updated. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        // group
        public DataAccessResult ChangeGroupName(int group_id, string group_name)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"update groups set group_name = @group_name where group_id = @group_id;", new { group_name = group_name, group_id = group_id });
                    if (rows == 1)
                    {
                        var group = connection.QuerySingle<Group>($"select * from groups where group_id = @group_id", new { group_id = group_id });
                        return new DataAccessResult()
                        {
                            returnObject = group
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "is_admin could not be updated. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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
        // group
        public DataAccessResult GetEvents(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<GroupEvent> events = connection.Query<GroupEvent>($"select * from events where group_id = @group_id", new { group_id = group_id });
                    return new DataAccessResult()
                    {
                        returnObject = events
                    };
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

        // group
        public DataAccessResult GenerateNewCode(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get list of all group codes
                    IEnumerable<string> group_codes = connection.Query<string>($"select group_code from groups");

                    // Generate group code until group code is unique
                    string group_code = GenerateGroupCode();
                    while (group_codes.Contains(group_code))
                    {
                        group_code = GenerateGroupCode();
                    }

                    var rows = connection.Execute($"update groups set group_code = @group_code where group_id = @group_id;", new { group_code = group_code, group_id = group_id });
                    if (rows == 1)
                    {
                        Group group = connection.QuerySingle<Group>($"select * from groups where group_id = @group_id", new { group_id = group_id });
                        return new DataAccessResult()
                        {
                            returnObject = group
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "Group code could not be updated."
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

        private static string GenerateGroupCode(int length = 6)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var randomString = new string(Enumerable.Repeat(chars, length)
                                                    .Select(s => s[random.Next(s.Length)]).ToArray());
            return randomString;
        }

        #endregion

        #region User

        // user
        public DataAccessResult ChangeAlias(int group_id, int user_id, string alias)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"update group_users set alias = @alias where group_id = @group_id and user_id = @user_id;", new { alias = alias, group_id = group_id, user_id = user_id });
                    if (rows == 1)
                    {
                        var groupUser = connection.QuerySingle<GroupUserDB>($"select * from group_users where group_id = @group_id and user_id = @user_id", new { group_id = group_id, user_id = user_id });
                        return new DataAccessResult()
                        {
                            returnObject = GroupUser.GroupUserDBToGroupUser(groupUser)
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "Alias could not be updated. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        public DataAccessResult ChangePassword(int user_id, string password)
        {
            var salt = GenerateSalt();
            var hashedPassword = GenerateSaltedHash(password, salt);

            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    connection.Execute($"update users set password = @password, salt = @salt where user_id = @user_id;", new { password = hashedPassword, salt = salt, user_id = user_id });
                    UserDB user = connection.QuerySingle<UserDB>($"select * from users where user_id = @user_id", new { user_id = user_id });
                    return new DataAccessResult()
                    {
                        returnObject = User.UserDBToUser(user)
                    };
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

        // user
        public DataAccessResult GetGroups(int user_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<Group> groups = connection.Query<Group>($"select g.* from users u inner join group_users gu on u.user_id = gu.user_id inner join groups g on gu.group_id = g.group_id where u.user_id = @user_id", new { user_id = user_id });
                    return new DataAccessResult()
                    {
                        returnObject = groups
                    };

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
        /*
        // user
        public DataAccessResult RateMovie(MovieRatings movie_ratings)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into group_movie_ratings (group_id,user_id,tmdb_movie_id,rating) values (@group_id,@user_id,@tmdb_movie_id,@rating)", new { group_id = movie_ratings.group_id, user_id = movie_ratings.user_id, tmdb_movie_id = movie_ratings.tmdb_movie_id, rating = movie_ratings.user_rating });
                    if (rows == 1)
                    {
                        return new DataAccessResult()
                        {
                            returnObject = movie_ratings
                        };

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
        */
        // user
        public DataAccessResult DeleteUserGroup(int user_id, int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    UserDB user = connection.QuerySingle<UserDB>($"select * from users where user_id = @user_id", new { user_id = user_id });
                    connection.Execute($"delete from group_users where user_id = @user_id and group_id = @group_id", new { user_id = user_id, group_id = group_id });
                    connection.Execute($"delete from group_movie_ratings where user_id = @user_id and group_id = @group_id", new { user_id = user_id, group_id = group_id });
                    return new DataAccessResult()
                    {
                        returnObject = User.UserDBToUser(user)
                    };

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

        // user
        public DataAccessResult ChangeAdmin(int group_id, int user_id, bool is_admin)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"update group_users set is_admin = @is_admin where group_id = @group_id and user_id = @user_id;", new { is_admin = is_admin, group_id = group_id, user_id = user_id });
                    if (rows == 1)
                    {
                        var groupUser = connection.QuerySingle<GroupUserDB>($"select * from group_users where group_id = @group_id and user_id = @user_id", new { group_id = group_id, user_id = user_id });
                        return new DataAccessResult()
                        {
                            returnObject = GroupUser.GroupUserDBToGroupUser(groupUser)
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "is_admin could not be updated. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        // user
        public DataAccessResult UpdateRateMovie(int user_id, int group_id, MovieRatings ratings)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    if (ratings.user_rating > 10 | ratings.user_rating < 0)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "Rating was out of the valid range of 0 - 10"
                        };
                    }
                    var rows = connection.Execute($"update group_movie_ratings set rating = @rating where group_id = @group_id and user_id = @user_id and tmdb_movie_id = @tmdb_movie_id;", new { rating = ratings.user_rating, group_id = group_id, user_id = user_id, tmdb_movie_id = ratings.tmdb_movie_id });
                    if (rows == 1)
                    {
                        ratings.user_id = user_id;
                        ratings.group_id = group_id;
                        return new DataAccessResult()
                        {
                            returnObject = ratings
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "rating could not be updated. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        // user
        public DataAccessResult JoinGroupUser(GroupJoinUser groupUser, int user_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    int exists = connection.QuerySingle<int>($"select count(*) from groups where group_code = @group_code", new { group_code = groupUser.group_code });
                    if (exists <= 0)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 404,
                            message = "Group does not exist"
                        };
                    }
                    int group_id = connection.QuerySingle<int>($"select group_id from groups where group_code = @group_code", new { group_code = groupUser.group_code });
                    var rows = connection.Execute($"insert into group_users (group_id,user_id,alias,is_admin) values (@group_id,@user_id,@alias,@is_admin)", new { group_id = group_id, user_id = user_id, alias = groupUser.alias, is_admin = groupUser.is_admin });
                    if (rows == 1)
                    {
                        GroupJoinUserResult result = new GroupJoinUserResult();
                        result.group_id = group_id;
                        result.group_code = groupUser.group_code;
                        result.user_id = user_id;
                        result.alias = groupUser.alias;
                        result.is_admin = groupUser.is_admin;

                        // Add ratings from this user for all movies in the queue
                        // Get a list of all movies in the queue
                        IEnumerable<int> movies = connection.Query<int>($"select tmdb_movie_id from group_movies where group_id = @group_id", new { group_id = group_id });
                        foreach (int movie in movies)
                        {
                            rows = connection.Execute($"insert into group_movie_ratings (group_id,user_id,tmdb_movie_id,rating) values (@group_id,@user_id,@tmdb_movie_id,5)", new { group_id = group_id, user_id = user_id, tmdb_movie_id = movie });
                            if (rows != 1)
                            {
                                return new DataAccessResult()
                                {
                                    error = true,
                                    statusCode = 500,
                                    message = "multiple rows changed. THIS SHOULD NEVER HAPPEN"
                                };
                            }
                        }


                        return new DataAccessResult()
                        {
                            returnObject = result
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "User could not be added to group. A SqlException should have been thrown. THIS SHOULD NEVER HAPPEN"
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

        // user
        public DataAccessResult ForgotPassword(string username)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

            var random = new Random();
            var newPassword = new string(Enumerable.Repeat(chars, 8).Select(s => s[random.Next(s.Length)]).ToArray());

            var salt = GenerateSalt();
            var hashedPassword = GenerateSaltedHash(newPassword, salt);

            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    string email = connection.QuerySingle<string>($"update users set password = @password, salt = @salt output inserted.email where username = @username;", new { password = hashedPassword, salt = salt, username = username, });
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(_config["MailSettings:email"], _config["MailSettings:password"]),
                        EnableSsl = true,
                    };

                    var emailBody = @$"Hello {username},

You recently requested a password reset.

Your new password: {newPassword}

After logging in, please change your password in the app.

Thanks,
Movie Night Team";

                    try
                    {
                        smtpClient.Send(_config["MailSettings:email"], email, "Movie Night Password Reset", emailBody);
                    }
                    catch (Exception ex)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            // TODO: Change message for final version 
                            message = ex.Message
                        };
                    }

                    return new DataAccessResult()
                    {
                        returnObject = new { message = "A reset password email has been sent to the user" }
                    };
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

        #region Event

        // event
        public DataAccessResult CreateEvent(GroupEvent group_event)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Add the event to the events table
                    var event_id = connection.QuerySingle<int>($"insert into events (group_id, start_time, location, tmdb_movie_id, organized_by, voting_mode) OUTPUT INSERTED.event_id values (@group_id, @start_time, @location, @tmdb_movie_id, @organized_by, @voting_mode)", new { group_id = group_event.group_id, start_time = group_event.start_time, location = group_event.location, tmdb_movie_id = group_event.tmdb_movie_id, organized_by = group_event.organized_by, voting_mode = group_event.voting_mode });
                    group_event.event_id = event_id;

                    // Add genres to event_genres
                    for (int i = 0; i < group_event.genres.Length; i++)
                    {
                        var rows = connection.Execute($"insert into event_genres (event_id, genre) values (@event_id,@genre)", new { event_id = group_event.event_id, genre = group_event.genres[i] });
                        if (rows != 1)
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "Genre could not be added for event."
                            };

                        }
                    }

                    // Add streaming services to event_services
                    for (int i = 0; i < group_event.genres.Length; i++)
                    {
                        var rows = connection.Execute($"insert into event_services (event_id,service) values (@event_id,@service)", new { event_id = group_event.event_id, service = group_event.services[i] });
                        if (rows != 1)
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "Streaming service could not be added for event."
                            };

                        }
                    }

                    // Return the newly created event
                    return new DataAccessResult()
                    {
                        returnObject = group_event
                    };
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
                catch (NullReferenceException)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        message = "Either the Services or Genres list was empty."
                    };
                }
            }
        }

        // event
        public DataAccessResult GetEvent(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    GroupEvent group_event = connection.QuerySingle<GroupEvent>($"select * from events where event_id = @event_id", new { event_id = event_id });
                    int[] services = connection.Query<int>($"select service from event_services where event_id = @event_id", new { event_id = event_id }).ToArray<int>();
                    int[] genres = connection.Query<int>($"select genre from event_genres where event_id = @event_id", new { event_id = event_id }).ToArray<int>();
                    group_event.genres = genres;
                    group_event.services = services;

                    return new DataAccessResult()
                    {
                        returnObject = group_event
                    };

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


        // event
        public DataAccessResult JoinEvent(RSVP rsvp)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into rsvp (user_id,event_id,is_coming) values (@user_id,@event_id,@is_coming)", new { user_id = rsvp.user_id, event_id = rsvp.event_id, is_coming = rsvp.is_coming });
                    if (rows == 1)
                    {
                        int num_movies = connection.QuerySingle<int>("select count(*) from event_movies where event_id = @event_id", new { event_id = rsvp.event_id });
                        // Check if there are movies already added to this event
                        if (num_movies > 0)
                        {
                            IEnumerable<EventMovies> movies = connection.Query<EventMovies>($"select * from event_movies where event_id = @event_id", new { event_id = rsvp.event_id });
                            // Iterate over all movies
                            for (int i = 0; i < movies.Count(); i++)
                            {
                                EventMovieRatings event_movie_ratings = new EventMovieRatings();
                                event_movie_ratings.event_id = rsvp.event_id;
                                event_movie_ratings.user_id = rsvp.user_id;
                                event_movie_ratings.tmdb_movie_id = movies.ElementAt(i).tmdb_movie_id;
                                event_movie_ratings.rating = 2;

                                // Attempt to rate the movie a 2 for this user
                                DataAccessResult add_rating = RateMovieEvent(event_movie_ratings);
                                if (add_rating.error)
                                {
                                    return new DataAccessResult()
                                    {
                                        error = true,
                                        statusCode = 500,
                                        // TODO: Change message for final version 
                                        message = add_rating.message
                                    };
                                }
                            }
                        }
      
                        IEnumerable<RSVP> all_rsvp = connection.Query<RSVP>($"select * from rsvp where event_id = @event_id", new { event_id = rsvp.event_id });
                        return new DataAccessResult()
                        {
                            returnObject = all_rsvp
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "User could not be RSVPd."
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

        // event
        public DataAccessResult GetRSVPs(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<RSVP> all_rsvp = connection.Query<RSVP>($"select * from rsvp where event_id = @event_id", new { event_id = event_id });
                    return new DataAccessResult()
                    {
                        returnObject = all_rsvp
                    };
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

        // event
        public DataAccessResult ChangeRSVP(int event_id, int user_id, IsComing is_coming)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"update rsvp set is_coming = @is_coming where user_id = @user_id and event_id = @event_id", new { is_coming = is_coming.is_coming, user_id = user_id, event_id = event_id });
                    if (rows == 1)
                    {
                        int num_movies = connection.QuerySingle<int>("select count(*) from event_movies where event_id = @event_id", new { event_id = event_id });
                        // Check if there are movies already added to this event
                        if (num_movies > 0)
                        {
                            // Remove all ratings for this user
                            rows = connection.Execute($"delete from event_movie_ratings where event_id = @event_id and user_id = @user_id", new { event_id = event_id, user_id = user_id });
                        }

                        IEnumerable<RSVP> all_rsvp = connection.Query<RSVP>($"select * from rsvp where event_id = @event_id", new { event_id = event_id });
                        return new DataAccessResult()
                        {
                            returnObject = all_rsvp
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "RSVP status could not be changed."
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

        // event
        public DataAccessResult AddMovieEvent(int event_id, MovieIDList movie_ids)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get list of all users who have RSVP'd to the event
                    IEnumerable<int> users = connection.Query<int>($"select user_id from rsvp where event_id = @event_id and is_coming = 1", new { event_id = event_id });

                    // Iterate over all movies to add
                    for (int i = 0; i < movie_ids.tmdb_movie_ids.Length; i++)
                    {
                        // Add the movie to the event
                        var rows = connection.Execute($"insert into event_movies (event_id,tmdb_movie_id) values (@event_id,@tmdb_movie_id)", new { event_id = event_id, tmdb_movie_id = movie_ids.tmdb_movie_ids[i] });
                        if  (rows != 1)
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "Movie could not be added to event."
                            };
                        }

                        // Rate this movie a 2 for all users
                        for (int j = 0; j < users.Count(); j++)
                        {
                            EventMovieRatings event_movie_ratings = new EventMovieRatings();
                            event_movie_ratings.event_id = event_id;
                            event_movie_ratings.user_id = users.ElementAt(j);
                            event_movie_ratings.tmdb_movie_id = movie_ids.tmdb_movie_ids[i];
                            event_movie_ratings.rating = 2;

                            DataAccessResult add_rating = RateMovieEvent(event_movie_ratings);
                            if (add_rating.error)
                            {
                                return new DataAccessResult()
                                {
                                    error = true,
                                    statusCode = 500,
                                    // TODO: Change message for final version 
                                    message = add_rating.message
                                };
                            }
                        }

                    }
                    
                    // Get a list of all movies in the event to return
                    IEnumerable<EventMovies> all_movies = connection.Query<EventMovies>($"select * from event_movies where event_id = @event_id", new { event_id = event_id });
                    return new DataAccessResult()
                    {
                        returnObject = all_movies
                    };
                    
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

        // event
        public DataAccessResult GetMoviesEventUser(int event_id, int user_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<EventMovieRatings> user_ratings = connection.Query<EventMovieRatings>($"select tmdb_movie_id, rating from event_movie_ratings where event_id = @event_id and user_id = @user_id", new { event_id = event_id, user_id = user_id });
                    return new DataAccessResult()
                    {
                        returnObject = user_ratings
                    };
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

        // event
        public DataAccessResult RateMovieEvent(EventMovieRatings event_movie_ratings)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Check if the movie and event_id combination is in event_movie
                    int exists = connection.QuerySingle<int>($"select count(*) from event_movies where event_id = @event_id and tmdb_movie_id = @tmdb_movie_id", new { event_id = event_movie_ratings.event_id, tmdb_movie_id = event_movie_ratings.tmdb_movie_id });
                    if (exists == 0)
                    {
                        // This movie has not been added to this event!
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The movie that was rated has not been added to this event."
                        };
                    }

                    var rows = connection.Execute($"insert into event_movie_ratings (event_id,user_id,tmdb_movie_id,rating) values (@event_id,@user_id,@tmdb_movie_id,@rating)", new { event_id = event_movie_ratings.event_id, user_id = event_movie_ratings.user_id, tmdb_movie_id = event_movie_ratings.tmdb_movie_id, rating = event_movie_ratings.rating });
                    if (rows == 1)
                    {

                        IEnumerable<EventMovieRatings> ratings = connection.Query<EventMovieRatings>($"select * from event_movie_ratings where event_id = @event_id", new { event_id = event_movie_ratings.event_id });
                        return new DataAccessResult()
                        {
                            returnObject = ratings
                        };

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

        // event
        public DataAccessResult UpdateEventMovieRating(EventMovieRatings event_movie_ratings)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Check if the movie and event_id combination is in event_movie
                    int exists = connection.QuerySingle<int>($"select count(*) from event_movies where event_id = @event_id and tmdb_movie_id = @tmdb_movie_id", new { event_id = event_movie_ratings.event_id, tmdb_movie_id = event_movie_ratings.tmdb_movie_id });
                    if (exists == 0)
                    {
                        // This movie has not been added to this event!
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The movie that was rated has not been added to this event."
                        };
                    }

                    var rows = connection.Execute($"update event_movie_ratings set rating = @rating where tmdb_movie_id = @tmdb_movie_id and user_id = @user_id and event_id = @event_id", new { event_id = event_movie_ratings.event_id, user_id = event_movie_ratings.user_id, tmdb_movie_id = event_movie_ratings.tmdb_movie_id, rating = event_movie_ratings.rating });
                    if (rows == 1)
                    {

                        IEnumerable<EventMovieRatings> ratings = connection.Query<EventMovieRatings>($"select * from event_movie_ratings where event_id = @event_id", new { event_id = event_movie_ratings.event_id });
                        return new DataAccessResult()
                        {
                            returnObject = ratings
                        };

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

        // event
        public DataAccessResult GetEventRating(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<EventMovieAverageRatings> ratings = connection.Query<EventMovieAverageRatings>($"select tmdb_movie_id, avg(cast(rating as float)) as avg_rating from event_movie_ratings where event_id = @event_id group by tmdb_movie_id", new { event_id = event_id });
                    return new DataAccessResult()
                    {
                        returnObject = ratings
                    };
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

        // event
        public DataAccessResult ChangeEventVotingMode (int event_id, VotingMode voting_mode)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get the group_id for the event
                    int group_id = connection.QuerySingle<int>($"select group_id from events where event_id = @event_id", new { event_id = event_id });
                    // See if there are any more events in that group that are in voting
                    int exists = connection.QuerySingle<int>($"select count(*) from events where group_id = @group_id and voting_mode = 1", new { group_id = group_id });
                    // If no event is in voting or we are setting an event to not voting then update the event
                    if(exists <= 0 || voting_mode.voting_mode == 0)
                    {
                        // Add the movie to the event
                        var rows = connection.Execute($"update events set voting_mode = @voting_mode where event_id = @event_id", new { voting_mode = voting_mode.voting_mode, event_id = event_id });
                        if (rows != 1)
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "Voting mode couls not be changed."
                            };
                        }

                        GroupEvent ret_event = connection.QuerySingle<GroupEvent>($"select * from events where event_id = @event_id", new { event_id = event_id });
                        return new DataAccessResult()
                        {
                            returnObject = ret_event
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "Voting is already enabled for another event in this group."
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

        public DataAccessResult ChangeEventMovie(int event_id, int tmdb_movie_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Add the movie to the event
                    var rows = connection.Execute($"update events set tmdb_movie_id = @tmdb_movie_id where event_id = @event_id", new { tmdb_movie_id = tmdb_movie_id, event_id = event_id });
                    if (rows != 1)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "Event movie mode could not be changed."
                        };
                    }

                    GroupEvent ret_event = connection.QuerySingle<GroupEvent>($"select * from events where event_id = @event_id", new { event_id = event_id });
                    return new DataAccessResult()
                    {
                        returnObject = ret_event
                    };
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

        // event
        public DataAccessResult RemoveEvent (int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get the event to return it after deletion
                    GroupEvent ret_event = connection.QuerySingle<GroupEvent>($"select * from events where event_id = @event_id", new { event_id = event_id });

                    // Delete all user ratings for the event
                    var rows = connection.Execute($"delete from event_movie_ratings where event_id = @event_id", new { event_id = event_id });

                    // Delete all movies for this event
                    rows = connection.Execute($"delete from event_movies where event_id = @event_id", new { event_id = event_id });

                    // Delete all RSVP's
                    rows = connection.Execute($"delete from rsvp where event_id = @event_id", new { event_id = event_id });

                    // Delete all services
                    rows = connection.Execute($"delete from event_services where event_id = @event_id", new { event_id = event_id });

                    // Delete all genres
                    rows = connection.Execute($"delete from event_genres where event_id = @event_id", new { event_id = event_id });

                    // Delete this event
                    rows = connection.Execute($"delete from events where event_id = @event_id", new { event_id = event_id });
                    if (rows == 1)
                    {
                        return new DataAccessResult()
                        {
                            returnObject = ret_event
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The specified event does not exist."
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
                catch (InvalidOperationException ex)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message + ". Ensure that the given event exists."
                    };
                }
            }
        }

        public DataAccessResult RemoveEventMovies(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Get a list of all movies in the event to return
                    IEnumerable<EventMovies> all_movies = connection.Query<EventMovies>($"select * from event_movies where event_id = @event_id", new { event_id = event_id });

                    // Delete all user ratings for the event
                    var rows = connection.Execute($"delete from event_movie_ratings where event_id = @event_id", new { event_id = event_id });

                    // Delete all movies for this event
                    rows = connection.Execute($"delete from event_movies where event_id = @event_id", new { event_id = event_id });

                    if (rows == 1)
                    {
                        return new DataAccessResult()
                        {
                            returnObject = all_movies
                        };
                    }
                    else
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 500,
                            message = "The specified event does not exist."
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
                catch (InvalidOperationException ex)
                {
                    return new DataAccessResult()
                    {
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message + ". Ensure that the given event exists."
                    };
                }
            }
        }

        #endregion

        #region Authorization Checks

        public bool CheckClaims(ClaimsIdentity identity, int user_id, int group_id, bool adminOnly, bool adminAllowed)
        {
            if (identity != null)
            {
                IEnumerable<Claim> claims = identity.Claims;
                var user_id_claim = identity.FindFirst("user_id").Value;

                IEnumerable<GroupClaim> groups;
                using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
                {
                    groups = connection.Query<GroupClaim>($"select gu.group_id, gu.is_admin from group_users gu inner join groups g on gu.group_id = g.group_id where gu.user_id = @user_id", new { user_id = user_id });
                }

                var group_claims = groups.Select(o => o.group_id).ToList();
                var admin_group_claims = groups.Where(o => o.is_admin).Select(o => o.group_id).ToList();

                // Only user_id given
                if (group_id == -1)
                {
                    if (user_id.ToString() == user_id_claim)
                    {
                        return true;
                    }
                    return false;
                }
                // Only group_id given
                else if(user_id == -1)
                {
                    if (adminOnly)
                    {
                        if (admin_group_claims.Contains(group_id))
                        {
                            return true;
                        }
                    }
                    else if (group_claims.Contains(group_id))
                    {
                        return true;
                    }
                    return false;
                }
                // Both user_id and group_id given
                else
                {
                    if (adminOnly)
                    {
                        if (admin_group_claims.Contains(group_id))
                        {
                            return true;
                        }
                    }
                    else if (adminAllowed)
                    {
                        if (admin_group_claims.Contains(group_id) || (group_claims.Contains(group_id) && user_id.ToString() == user_id_claim))
                        {
                            return true;
                        }
                    }
                    else if (group_claims.Contains(group_id) && user_id.ToString() == user_id_claim)
                    {
                        return true;
                    }
                    return false;
                }
            }
            return false;
        }

        #endregion

    }
}
