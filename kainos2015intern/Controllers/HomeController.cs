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
using System.Web.UI.DataVisualization.Charting;
using System.IO;

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

        public List<DataModels.movie> getMoviesRanking()
        {
            DataModels.dbContext context = new DataModels.dbContext();

            List<DataModels.movie> moviesList = (from movie in context.movies
                                                orderby movie.vote_average descending, movie.release_date ascending
                                                select movie).Take(20).ToList();

            context.Dispose();
            return moviesList;
        }

        public ActionResult Movie(int? id)
        {
            if (!id.HasValue)
                return View("FilmNoId");

            try
            {
                DataModels.dbContext context = new DataModels.dbContext();
                DataModels.movie movieD = (from movie in context.movies
                                          where movie.id == id.Value
                                          select movie).SingleOrDefault();

                List<DataModels.movie_genre> mgs = (from movieGenre in context.movie_genre //wtf linq2db doesnt get fk entities
                                                    where movieGenre.movie_id == movieD.id //looks worse than sql now
                                                    select movieGenre).ToList();
                
                string genres = "";
                foreach (DataModels.movie_genre mg in mgs)
                    genres += (from genre in context.genres
                               where genre.id == mg.genre_id
                               select genre.name).SingleOrDefault().ToString() + " ";

                string ombdRequest = "http://www.omdbapi.com/?t=" + HttpUtility.UrlEncode(movieD.title) + "&y=&plot=full&r=xml";
                XmlDocument ombdRespond = MakeOmbdRequest(ombdRequest);

                if (ombdRespond != null)
                {
                    try
                    {
                        ViewBag.plot = ombdRespond.GetElementsByTagName("movie")[0].Attributes.GetNamedItem("plot").InnerText;
                    }
                    catch (Exception e)
                    {
                        ViewBag.plot = "Error requesting plot";
                    }
                }
                else
                    ViewBag.plot = "Error requesting plot";


                ViewBag.genres = genres;
                ViewBag.movie = movieD;

                context.Dispose();
                return View("Movie");
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        public ActionResult Search(FormCollection fc)
        {
            try
            {
                DataModels.dbContext context = new DataModels.dbContext();
                string score = Request["scoreText"];
                string genresArr = Request["genres"];
                List<string> genresSelected = new List<string>();

                ViewBag.genres = (List<string>)(from genre in context.genres
                                  select genre.name).ToList();
                ViewBag.score = score;
                if (!String.IsNullOrEmpty(genresArr))
                {
                    genresSelected = fixNamesList(genresArr.Split(',').ToList(), ViewBag.genres);
                }
                ViewBag.genresSelected = genresSelected;
                ViewBag.movieList = getMoviesSearchList(genresSelected, score);

                context.Dispose();
                return View("Search");
            }
            catch (Exception e)
            {
                return View("ErrNoDbConn");
            }
        }

        public ActionResult GetGenre()
        {
            return View("GetGenre");
        }

        public ActionResult GetGenresChart()
        {
            try
            {
                DataModels.dbContext context = new DataModels.dbContext();

                var genresq = (from link in context.movie_genre
                               join genres in context.genres on link.genre_id equals genres.id
                               group link.genre_id by genres.name into genre
                               select new
                               {
                                   genreName = genre.Key,
                                   genreCount = genre.Count()
                               });

                List<string> genreName = new List<string>();
                List<int> genreCount = new List<int>();
                foreach (var list in genresq)
                {
                    genreName.Add(list.genreName);
                    genreCount.Add(list.genreCount);
                }

                context.Dispose();

                Chart chart = new Chart();
                chart.ChartAreas.Add(new ChartArea());
                chart.Width = 1024;
                chart.Height = 768;
                chart.Series.Add(new Series("Genres"));
                chart.Series["Genres"].ChartType = SeriesChartType.Pie;
                chart.Series["Genres"].Points.DataBindXY(
                    genresq.Select(g => g.genreName.ToString()).ToArray(),
                    genresq.Select(g => g.genreCount).ToArray());
                chart.Series["Genres"].Label = "#PERCENT{P0} #VALX";
                chart.Series["Genres"]["PieLabelStyle"] = "Outside";
                chart.Series["Genres"]["PieLineColor"] = "Black";

                MemoryStream ms = new MemoryStream();
                chart.SaveImage(ms, ChartImageFormat.Png);

                return File(ms.ToArray(), "image/png");
            }
            catch (Exception e)
            {
                return null;
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

        private List<string> fixNamesList(List<string> names, List<string> genres)
        {
            List<string> fixedNames = new List<string>();

            foreach (string genre in genres)
            {
                foreach (string name in names)
                {
                    if (genre.Contains(name))
                    {
                        string newName = genre;
                        fixedNames.Add(newName);
                    }
                }
            }

            return fixedNames;
        }

        private List<DataModels.movie> getMoviesSearchList(List<string> name, string score)
        {
            DataModels.dbContext context = new DataModels.dbContext();
            List<DataModels.movie> movies = new List<DataModels.movie>();

            if (name.Any() && !String.IsNullOrEmpty(score))
            {
                float f = float.Parse(score, CultureInfo.InvariantCulture);

                if (name.Count > 1) //intersections didnt work or im too dumb
                {
                    List<DataModels.movie> moviesWduplicates =
                             (from movie in context.movies
                              from genre in context.genres
                              from mg in context.movie_genre
                              where (movie.id == mg.movie_id &&
                                     mg.genre_id == genre.id &&
                                     name.Contains(genre.name) &&
                                     movie.vote_average >= f)
                              select movie).ToList();

                    movies = (from m in moviesWduplicates
                              group m by m.title into mv
                              where mv.Count() == name.Count
                              select mv.First()).ToList();
                }
                else
                {
                    movies = (from movie in context.movies
                              from genre in context.genres
                              from mg in context.movie_genre
                              where (movie.id == mg.movie_id &&
                                     mg.genre_id == genre.id &&
                                     name.Contains(genre.name) &&
                                     movie.vote_average >= f)
                              select movie).ToList();
                }
            }
            else if (name.Any())
            {
                if (name.Count > 1)
                {
                    List<DataModels.movie> moviesWduplicates =
                             (from movie in context.movies
                              from genre in context.genres
                              from mg in context.movie_genre
                              where (movie.id == mg.movie_id &&
                                     mg.genre_id == genre.id &&
                                     name.Contains(genre.name))
                              select movie).ToList();

                    movies = (from m in moviesWduplicates
                              group m by m.title into mv
                              where mv.Count() == name.Count
                              select mv.First()).ToList();
                }
                else
                {
                    movies = (from movie in context.movies
                              from genre in context.genres
                              from mg in context.movie_genre
                              where (movie.id == mg.movie_id &&
                                     mg.genre_id == genre.id &&
                                     name.Contains(genre.name))
                              select movie).ToList();
                }
            }
            else if (!String.IsNullOrEmpty(score))
            {
                float f = float.Parse(score, CultureInfo.InvariantCulture);

                movies = (from movie in context.movies
                          where movie.vote_average >= f
                          select movie).ToList();
            }

            return movies;
        }
    }
}