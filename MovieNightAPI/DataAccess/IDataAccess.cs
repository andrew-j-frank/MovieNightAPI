using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MovieNightAPI.DataAccess
{
    public interface IDataAccess
    {
        public DataAccessResult Login(Login login);

        public DataAccessResult SignUp(SignUp signUp);

        public DataAccessResult CreateGroup(Group group);

        public DataAccessResult JoinGroup(int group_id, int creator_id, string alias, Boolean is_admin = false);

        public DataAccessResult ChangeAlias(int group_id, int creator_id, string alias);

        public DataAccessResult GetGroups(int user_id);

        public DataAccessResult AddGroupMovie(GroupMovies group_movies);

        public DataAccessResult RemoveMovie(int group_id, int tmdb_movie_id);

        public DataAccessResult GetMovies(int group_id);

        public DataAccessResult RateMovie(MovieRatings movie_ratings);

        public DataAccessResult GetGroupRating(int tmdb_movie_id, int group_id);

    }
}
