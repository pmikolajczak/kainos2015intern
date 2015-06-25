using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Net;
using System.Globalization;

namespace kainos2015intern.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            try
            {
                ViewBag.moviesRanking = getMoviesRanking();

                return View();
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        public List<dbContext.Movie> getMoviesRanking()
        {
            dbContext.dbDataContext context = new dbContext.dbDataContext();

            List<dbContext.Movie> moviesList = (from movie in context.Movies
                                                orderby movie.VoteAverage descending, movie.ReleaseDate ascending
                                                select movie).Take(20).ToList();

            return moviesList;
        }

        public ActionResult Movie(int? id)
        {
            if (!id.HasValue)
                return View("FilmNoId");

            try
            {
                dbContext.dbDataContext context = new dbContext.dbDataContext();
                dbContext.Movie movieD = (from movie in context.Movies
                                          where movie.Id == id.Value
                                          select movie).SingleOrDefault();

                List<dbContext.MovieGenre> mgs = new List<dbContext.MovieGenre>(movieD.MovieGenres);
                string genres = "";

                foreach (dbContext.MovieGenre mg in mgs)
                    genres += mg.Genre.Name.ToString() + " ";

                string ombdRequest = "http://www.omdbapi.com/?t=" + HttpUtility.UrlEncode(movieD.Title) + "&y=&plot=full&r=xml";
                XmlDocument ombdRespond = MakeOmbdRequest(ombdRequest);

                if (ombdRespond != null)
                    ViewBag.plot = ombdRespond.GetElementsByTagName("movie")[0].Attributes.GetNamedItem("plot").InnerText;
                else
                    ViewBag.plot = "Error requesting plot";


                ViewBag.genres = genres;
                ViewBag.movie = movieD;

                return View("Movie");
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        public ActionResult Search()
        {
            try
            {
                string filmName = Request["filmText"];
                string score = Request["scoreText"];

                ViewBag.movies = getMoviesSearchList(filmName, score);
                ViewBag.filmName = filmName;
                ViewBag.score = score;

                return View("Search");
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        public ActionResult GetGenre()
        {
            try
            {
                dbContext.dbDataContext context = new dbContext.dbDataContext();

                var genresq = (from link in context.MovieGenres
                               join genres in context.Genres on link.GenreId equals genres.Id
                               group link.GenreId by genres.Name into genre
                               select new
                               {
                                   genreName = genre.Key,
                                   genreCount = genre.Count()
                               });

                List<Tuple<string, int>> genresList = new List<Tuple<string, int>>();
                foreach (var list in genresq)
                    genresList.Add(new Tuple<string, int>(list.genreName, list.genreCount));

                ViewBag.genres = genresList;

                return View("TopGenre");
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        private XmlDocument MakeOmbdRequest(string requestString)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(requestString) as HttpWebRequest;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;

                XmlDocument xml = new XmlDocument();
                xml.Load(response.GetResponseStream());

                return xml;
            }
            catch (Exception e)
            {
                return null;
            }
        }

        private List<dbContext.Movie> getMoviesSearchList(string name, string score)
        {
            dbContext.dbDataContext context = new dbContext.dbDataContext();
            List<dbContext.Movie> movies = new List<dbContext.Movie>();

            if (!String.IsNullOrEmpty(name) && !String.IsNullOrEmpty(score))
            {
                float f = float.Parse(score, CultureInfo.InvariantCulture);

                movies = (from movie in context.Movies
                          where (movie.Title.Contains(name) &&
                                 movie.VoteAverage >= f)
                          orderby movie.Title
                          select movie).ToList();
            }
            else if (!String.IsNullOrEmpty(name))
            {
                movies = (from movie in context.Movies
                          where movie.Title.Contains(name)
                          orderby movie.Title
                          select movie).ToList();
            }
            else if (!String.IsNullOrEmpty(score))
            {
                float f = float.Parse(score, CultureInfo.InvariantCulture);

                movies = (from movie in context.Movies
                          where movie.VoteAverage >= f
                          orderby movie.Title
                          select movie).ToList();
            }

            return movies;
        }
    }
}