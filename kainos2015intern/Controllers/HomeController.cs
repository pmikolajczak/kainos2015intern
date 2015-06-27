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
            using (DataModels.dbContext context = new DataModels.dbContext())
            {
                List<DataModels.movie> moviesList = (from movie in context.movies
                                                     orderby movie.vote_average descending, movie.release_date ascending
                                                     select movie).Take(20).ToList();

                return moviesList;
            }
        }

        public ActionResult Movie(int? id)
        {
            if (!id.HasValue)
                return View("FilmNoId");

            try
            {
                using (DataModels.dbContext context = new DataModels.dbContext())
                {
                    DataModels.movie movieD = (from movie in context.movies
                                               where movie.id == id.Value
                                               select movie).SingleOrDefault();

                    List<DataModels.genre> genres = (from mg in context.movie_genre
                                                     where mg.moviegenremovieidfkey.id == movieD.id
                                                     select mg.moviegenregenreidfkey).ToList();

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

                    return View("Movie");
                }
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
                using (DataModels.dbContext context = new DataModels.dbContext())
                {
                    string score = Request["scoreText"];
                    string genresArr = Request["genres"];
                    List<DataModels.genre> genresSelected = new List<DataModels.genre>();

                    List<DataModels.genre> genres = (from genre in context.genres
                                                     select genre).ToList();

                    if (!String.IsNullOrEmpty(genresArr))
                    {
                        genresSelected = (from genre in genres
                                          where genresArr.Split(',').Contains(genre.id.ToString())
                                          select genre).ToList();
                    }

                    ViewBag.score = score;
                    ViewBag.genres = genres;
                    ViewBag.genresSelected = genresSelected;
                    ViewBag.movieList = getMoviesSearchList(genresSelected, score);

                    return View("Search");
                }
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
                using (DataModels.dbContext context = new DataModels.dbContext())
                {
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

        private List<DataModels.movie> getMoviesSearchList(List<DataModels.genre> name, string score)
        {
            List<DataModels.movie> movies = new List<DataModels.movie>();

            using (DataModels.dbContext context = new DataModels.dbContext())
            {
                if (name.Any() && !String.IsNullOrEmpty(score))
                {
                    float f = float.Parse(score, CultureInfo.InvariantCulture);
                    List<int> nameIdList = name.Select(n => n.id).ToList();

                    var qq = (from genre in
                                  (from g in context.genres where nameIdList.Contains(g.id) select g)
                              join mg in context.movie_genre on genre.id equals mg.genre_id
                              group mg.moviegenremovieidfkey.id by mg.movie_id into movie
                              select new
                              {
                                  movieCount = movie.Count(),
                                  movieId = movie.Key
                              });
                    movies = (from mvs in qq
                              join movie in context.movies on mvs.movieId equals movie.id
                              where movie.vote_average >= f && mvs.movieCount == name.Count
                              orderby movie.title ascending, movie.vote_average descending
                              select movie).ToList();
                }
                else if (name.Any())
                {
                    List<int> nameIdList = name.Select(n => n.id).ToList();

                    var qq = (from genre in
                                  (from g in context.genres where nameIdList.Contains(g.id) select g)
                              join mg in context.movie_genre on genre.id equals mg.genre_id
                              group mg.moviegenremovieidfkey.id by mg.movie_id into movie
                              select new
                              {
                                  movieCount = movie.Count(),
                                  movieId = movie.Key
                              });
                    movies = (from mvs in qq
                              join movie in context.movies on mvs.movieId equals movie.id
                              where mvs.movieCount == name.Count
                              orderby movie.title ascending, movie.vote_average descending
                              select movie).ToList();
                }
                else if (!String.IsNullOrEmpty(score))
                {
                    float f = float.Parse(score, CultureInfo.InvariantCulture);

                    movies = (from movie in context.movies
                              where movie.vote_average >= f
                              orderby movie.title ascending, movie.vote_average descending
                              select movie).ToList();
                }
            }

            return movies;
        }
    }
}