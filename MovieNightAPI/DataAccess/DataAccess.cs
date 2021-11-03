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
                    var rows = connection.Execute($"insert into users (username,password,salt,email) values (@username,@password,@salt,@email)", new { username = signUp.username, password = hashedPassword, salt = salt, email = signUp.email });
                    if (rows == 1)
                    {
                        var users = connection.Query<User>($"select * from users where username = @username", new { username = signUp.username }).ToList();
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

        #region Group

        public DataAccessResult CreateGroup(Group group)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var group_id = connection.QuerySingle<int>($"insert into groups (group_name,created_by) OUTPUT INSERTED.group_id values (@group_name,@created_by)", new { group_name = group.group_name, created_by = group.created_by });
                    group.group_id = group_id;
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

        public DataAccessResult JoinGroup(int group_id, int creator_id, string alias, Boolean is_admin = false)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into group_users (group_id,user_id,alias,is_admin) values (@group_id,@creator_id,@alias,@is_admin)", new { group_id = group_id, creator_id = creator_id, alias = alias, is_admin = is_admin });
                    if (rows == 1)
                    {
                        var group = connection.QuerySingle<Group>($"select * from group where group_id = @group_id", new { group_id = group_id });
                        group.alias = alias;
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

        public DataAccessResult ChangeAlias(int group_id, int user_id, string alias)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"update group_users set alias = @alias where group_id = @group_id and user_id = @user_id;", new { alias = alias, group_id = group_id, user_id = user_id });
                    if (rows == 1)
                    {
                        var group = connection.QuerySingle<Group>($"select * from group where group_id = @group_id", new { group_id = group_id });
                        group.alias = alias;
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

        public DataAccessResult GetGroups(int user_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<Group> groups = connection.Query<Group>($"selct g.* from users u inner join group_users gu on u.user_id = gu.user_id inner join groups g on gu.group_id = g.group_id where u.user_id = @user_id", new { user_id = user_id });
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

        #endregion

        #region Movies
        public DataAccessResult AddGroupMovie(GroupMovies group_movies)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into group_movies (group_id,tmdb_movie_id,added_by) values (@group_id,@tmdb_movie_id,@added_by)", new { group_id = group_movies.group_id, tmdb_movie_id = group_movies.tmdb_movie_id, added_by = group_movies.added_by });
                    if (rows == 1)
                    {
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

        public DataAccessResult RemoveMovie(int group_id, int tmdb_movie_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"delete from group_movies where group_id = @group_id and tmdb_movie_id = @tmdb_movie_id", new { group_id = group_id, tmdb_movie_id = tmdb_movie_id });
                    if (rows == 1)
                    {
                        return null;
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
                        // Likely, this movie already exists within this group
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        public DataAccessResult GetMovies(int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    // Should we check if there are no movies?
                    IEnumerable<GroupMovies> movies = connection.Query<GroupMovies>($"selct * from group_movies where group_id = @group_id", new { group_id = group_id });
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

        #endregion

        #region Ratings

        public DataAccessResult RateMovie(MovieRatings movie_ratings)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var rows = connection.Execute($"insert into group_movie_ratings (group_id,user_id,tmdb_movie_id,rating) values (@group_id,@user_id,@tmdb_movie_id,@rating)", new { group_id = movie_ratings.group_id, user_id = movie_ratings.user_id, tmdb_movie_id = movie_ratings.tmdb_movie_id, rating = movie_ratings.rating });
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
                        // Likely, this user/movie/group combination already exists
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        public DataAccessResult GetGroupRating(int tmdb_movie_id, int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var avg_rating = connection.QuerySingle<int>($"select avg(cast(rating as float)) from group_movie_ratings where tmdb_movie_id = @tmdb_movie_id and group_id = @group_id group by group_id ", new { tmdb_movie_id = tmdb_movie_id, group_id = group_id });
                    return new DataAccessResult()
                    {
                        returnObject = avg_rating
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
    }
}
