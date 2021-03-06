using MovieNightAPI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace MovieNightAPI.DataAccess
{
    public interface IDataAccess
    {
        public DataAccessResult Login(Login login);

        public DataAccessResult SignUp(SignUp signUp);

        public DataAccessResult CreateGroup(GroupJoin group);

        public DataAccessResult JoinGroupCreator(GroupJoin group);
        public DataAccessResult JoinGroupUser(GroupJoinUser groupUser, int user_id);

        public DataAccessResult ChangeAlias(int group_id, int creator_id, string alias);

        public DataAccessResult GetGroups(int user_id);

        public DataAccessResult AddGroupMovie(GroupMovies group_movies);

        public DataAccessResult RemoveMovie(int group_id, int tmdb_movie_id);

        public DataAccessResult GetMovies(int group_id, int user_id);

        //public DataAccessResult RateMovie(MovieRatings movie_ratings);

        public DataAccessResult CreateEvent(GroupEvent group_event);

        public DataAccessResult JoinEvent(RSVP rsvp);

        public DataAccessResult GetRSVPs(int event_id);

        public DataAccessResult ChangeRSVP(int event_id, int user_id, IsComing is_coming);

        public DataAccessResult AddMovieEvent(int event_id, MovieIDList movie_ids);

        public DataAccessResult GetMoviesEventUser(int event_id, int user_id);

        public DataAccessResult GetEvent(int event_id);

        public DataAccessResult RateMovieEvent(EventMovieRatings event_movie_ratings);

        public DataAccessResult UpdateEventMovieRating(EventMovieRatings event_movie_ratings);

        public DataAccessResult GetEventRating(int event_id);

        public DataAccessResult ChangeEventVotingMode(int event_id, VotingMode voting_mode);

        public DataAccessResult RemoveEvent(int event_id);

        public DataAccessResult GetUsers(int group_id);

        public DataAccessResult DeleteGroup(int group_id);

        public DataAccessResult DeleteUserGroup(int user_id, int group_id);

        public DataAccessResult ChangeAdmin(int group_id, int user_id, bool is_admin);

        public DataAccessResult GetGroup(int group_id);

        public DataAccessResult ChangeMaxMovies(int group_id, int max_user_movies);

        public DataAccessResult GetEvents(int group_id);

        public DataAccessResult UpdateRateMovie(int user_id, int group_id, MovieRatings ratings);

        public DataAccessResult ChangeGroupName(int group_id, string group_name);

        public DataAccessResult GenerateNewCode(int group_id);

        public DataAccessResult ForgotPassword(string username);

        public DataAccessResult ChangeEventMovie(int event_id, int tmdb_movie_id);

        public DataAccessResult RemoveEventMovies(int event_id);

        public DataAccessResult ChangePassword(int user_id, string password);

        public bool CheckClaims(ClaimsIdentity identity, int user_id, int group_id, bool adminOnly, bool adminAllowed);
    }
}
