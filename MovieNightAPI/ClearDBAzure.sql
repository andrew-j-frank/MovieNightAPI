DROP TABLE rsvp
DROP TABLE event_movies
DROP TABLE event_movie_ratings
DROP TABLE group_movie_ratings
DROP TABLE group_movies
DROP TABLE events
DROP TABLE group_users
DROP TABLE groups
DROP TABLE users
DROP TABLE event_genres
DROP TABLE event_services

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
	group_code VARCHAR(6) NOT NULL,
	group_name VARCHAR(50) NOT NULL,
	created_by INT NOT NULL,
	max_user_movies INT NOT NULL,
	CONSTRAINT AK_group_code UNIQUE(group_code),
	FOREIGN KEY (created_by) REFERENCES users (user_id)
);

-- Create the group_users table
CREATE TABLE group_users (
	group_id INT,
	user_id INT,
	alias VARCHAR(25) NULL,
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

-- Create the event_genres table
CREATE TABLE event_genres (
	event_id INT,
	genre INT,
	FOREIGN KEY (event_id) REFERENCES events (event_id),
	PRIMARY KEY(event_id, genre)
);

-- Create the event_services table
CREATE TABLE event_services (
	event_id INT,
	service INT,
	FOREIGN KEY (event_id) REFERENCES events (event_id),
	PRIMARY KEY(event_id, service)
);
