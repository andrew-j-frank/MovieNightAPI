-- Switch to master to drop the db
USE master

-- Drop the database if it exists
DROP DATABASE IF EXISTS Movie_Night_DB;

-- Create the database
IF db_id('Movie_Night_DB') IS NULL 
    CREATE DATABASE Movie_Night_DB
GO

-- Switch to the database to create the tables for it
USE Movie_Night_DB

-- Create the users table
CREATE TABLE users (
	user_id INT PRIMARY KEY IDENTITY (1, 1),
	username VARCHAR(25) NOT NULL,
	password VARCHAR(50) NOT NULL,
	salt VARCHAR(50) NOT NULL,
	email VARCHAR(50) NOT NULL,
	CONSTRAINT AK_username UNIQUE(username)
);

-- Create the groups table
CREATE TABLE groups (
	group_id INT PRIMARY KEY IDENTITY (1, 1),
	group_name VARCHAR(50) NOT NULL,
	created_by INT NOT NULL,
	FOREIGN KEY (created_by) REFERENCES users (user_id)
);

-- Create the group_users table
CREATE TABLE group_users (
	group_id INT,
	user_id INT,
	alias VARCHAR(25) NOT NULL,
	is_admin BIT NOT NULL,
	FOREIGN KEY (group_id) REFERENCES groups (group_id),
	FOREIGN KEY (user_id) REFERENCES users (user_id),
	PRIMARY KEY(group_id, user_id)
);

-- Create the events table
CREATE TABLE events (
	event_id INT PRIMARY KEY IDENTITY (1, 1),
	group_id INT NOT NULL,
	start_time DATETIME NOT NULL,
	location VARCHAR(50) NOT NULL,
	genre INT NOT NULL,
	tmdb_movie_id INT,
	organized_by INT NOT NULL,
	voting_mode INT NOT NULL,
	FOREIGN KEY (group_id) REFERENCES groups (group_id),
	FOREIGN KEY (organized_by) REFERENCES users (user_id)
);

-- Create the group_movies table
CREATE TABLE group_movies (
	group_id INT,
	tmdb_movie_id INT,
	added_by INT NOT NULL,
	FOREIGN KEY (group_id) REFERENCES groups (group_id),
	FOREIGN KEY (added_by) REFERENCES users (user_id),
	PRIMARY KEY(group_id, tmdb_movie_id)
);

-- Create the group_movie_ratings table
CREATE TABLE group_movie_ratings (
	group_id INT,
	user_id INT,
	tmdb_movie_id INT,
	rating INT NOT NULL,
	FOREIGN KEY (group_id) REFERENCES groups (group_id),
	FOREIGN KEY (user_id) REFERENCES users (user_id),
	PRIMARY KEY(group_id, user_id, tmdb_movie_id)
);

-- Create the event_movie_ratings table
CREATE TABLE event_movie_ratings (
	event_id INT,
	user_id INT,
	tmdb_movie_id INT,
	rating INT NOT NULL,
	FOREIGN KEY (event_id) REFERENCES events (event_id),
	FOREIGN KEY (user_id) REFERENCES users (user_id),
	PRIMARY KEY(event_id, user_id, tmdb_movie_id)
);

-- Create the event_movies table
CREATE TABLE event_movies (
	event_id INT,
	tmdb_movie_id INT,
	FOREIGN KEY (event_id) REFERENCES events (event_id),
	PRIMARY KEY(event_id, tmdb_movie_id)
);

-- Create the rsvp table
CREATE TABLE rsvp (
	event_id INT,
	user_id INT,
	is_coming BIT NOT NULL,
	FOREIGN KEY (event_id) REFERENCES events (event_id),
	FOREIGN KEY (user_id) REFERENCES users (user_id),
	PRIMARY KEY(event_id, user_id)
);
