using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Text;

namespace kainos2015intern.Controllers
{
    public class TopListController : Controller
    {
        [Route("")]
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
    }
}