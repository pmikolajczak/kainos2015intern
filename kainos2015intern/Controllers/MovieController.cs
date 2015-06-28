using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Xml;
using System.Xml.XPath;
using System.Net;

namespace kainos2015intern.Controllers
{
    public class MovieController : Controller
    {
        [Route("movie")]
        public ActionResult Index()
        {
            return View("FilmNoId");
        }

        [Route("movie/{id}")]
        public ActionResult Movie(int id)
        {

            try
            {
                using (DataModels.dbContext context = new DataModels.dbContext())
                {
                    DataModels.movie movieD = (from movie in context.movies
                                               where movie.id == id
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
	}
}