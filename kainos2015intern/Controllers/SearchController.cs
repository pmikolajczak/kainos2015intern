using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Globalization;

namespace kainos2015intern.Controllers
{
    public class SearchController : Controller
    {
        [Route("search")]
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