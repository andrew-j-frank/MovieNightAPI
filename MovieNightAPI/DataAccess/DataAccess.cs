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
using System.Security.Claims;
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
                            var token = GenerateToken(user.username);
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
                            var token = GenerateToken(user.username);
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

        private string GenerateToken(string username)
        {
            // reference: https://stackoverflow.com/a/63446357

            var clims = new[]
            {
                new Claim("username", username),
            };
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["AuthSettings:Key"]));
            var token = new JwtSecurityToken(
                issuer: _config["AuthSettings:Issuer"],
                audience: _config["AuthSettings:Audience"],
                claims: clims,
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
                    var group_id = connection.QuerySingle<int>($"insert into groups (group_name,created_by,max_user_movies) OUTPUT INSERTED.group_id values (@group_name,@created_by,-1)", new { group_name = group.group_name, created_by = group.created_by });
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
                    if (max_movies != -1 && num_movies >= max_movies)
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
                    IEnumerable<GroupMovieRating> movies = connection.Query<GroupMovieRating>($"select gm.group_id, gm.tmdb_movie_id, avg(cast(rating as float)) as avg_user_rating from group_movies gm inner join group_movie_ratings gmr on gm.group_id = gmr.group_id and gm.tmdb_movie_id = gmr.tmdb_movie_id where gm.group_id = @group_id group by gm.group_id, gm.tmdb_movie_id", new { group_id = group_id });
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

        // user
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
                        error = true,
                        statusCode = 500,
                        // TODO: Change message for final version 
                        message = ex.Message
                    };
                }
            }
        }

        // user
        public DataAccessResult DeleteUserGroup(int user_id, int group_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    UserDB user = connection.QuerySingle<UserDB>($"select * from users where user_id = @user_id", new { user_id = user_id });
                    connection.Execute($"delete from group_users where user_id = @user_id and group_id = @group_id", new { user_id = user_id, group_id = group_id });
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
                    var rows = connection.Execute($"update group_movie_ratings set rating = @rating where group_id = @group_id and user_id = @user_id and tmdb_movie_id = @tmdb_movie_id;", new { rating = ratings.rating, group_id = group_id, user_id = user_id, tmdb_movie_id = ratings.tmdb_movie_id });
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

        #endregion

        #region Group and User

        // user
        // group
        public DataAccessResult JoinGroup(int group_id, int creator_id, string alias, bool is_admin = false)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    int exists = connection.QuerySingle<int>($"select count(*) from groups where group_id = @group_id", new { group_id = group_id });
                    if (exists <= 0)
                    {
                        return new DataAccessResult()
                        {
                            error = true,
                            statusCode = 404,
                            message = "Group does not exist"
                        };
                    }
                    var rows = connection.Execute($"insert into group_users (group_id,user_id,alias,is_admin) values (@group_id,@creator_id,@alias,@is_admin)", new { group_id = group_id, creator_id = creator_id, alias = alias, is_admin = is_admin });
                    if (rows == 1)
                    {
                        var group = connection.QuerySingle<GroupJoin>($"select * from groups where group_id = @group_id", new { group_id = group_id });
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

        #endregion

        #region Event

        // event
        public DataAccessResult CreateEvent(GroupEvent group_event)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    var event_id = connection.QuerySingle<int>($"insert into events (group_id, start_time, location, genre, tmdb_movie_id, organized_by, voting_mode) OUTPUT INSERTED.event_id values (@group_id, @start_time, @location, @genre, @tmdb_movie_id, @organized_by, @voting_mode)", new { group_id = group_event.group_id, start_time = group_event.start_time, location = group_event.location, genre = group_event.genre, tmdb_movie_id = group_event.tmdb_movie_id, organized_by = group_event.organized_by, voting_mode = group_event.voting_mode });
                    group_event.event_id = event_id;
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
        public DataAccessResult GetEvent(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    GroupEvent group_event = connection.QuerySingle<GroupEvent>($"select * from events where event_id = @event_id", new { event_id = event_id });
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
                    for (int i = 0; i < movie_ids.movie_ids.Count; i++)
                    {
                        var rows = connection.Execute($"insert into event_movies (event_id,tmdb_movie_id) values (@event_id,@tmdb_movie_id)", new { event_id = event_id, tmdb_movie_id = movie_ids.movie_ids[i] });
                        if  (rows != 1)
                        {
                            return new DataAccessResult()
                            {
                                error = true,
                                statusCode = 500,
                                message = "Movie could not be added to event."
                            };
                        }
                    }

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
        public DataAccessResult GetMoviesEvent(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<int> movies = connection.Query<int>($"select tmdb_movie_id from event_movies where event_id = @event_id", new { event_id = event_id });
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
        public DataAccessResult GetEventRating(int event_id)
        {
            using (SqlConnection connection = new SqlConnection(_config.GetConnectionString("SQLServer")))
            {
                try
                {
                    IEnumerable<EventMovieRatings> ratings = connection.Query<EventMovieRatings>($"select * from event_movie_ratings where event_id = @event_id", new { event_id = event_id });
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

        #endregion

    }
}
