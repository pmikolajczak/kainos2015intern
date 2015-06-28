using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.DataVisualization.Charting;
using System.IO;

namespace kainos2015intern.Controllers
{
    public class TopGenreController : Controller
    {
        [Route("topGenre")]
        public ActionResult Index()
        {
            return View("TopGenre");
        }

        [Route("topGenre/getGenresChart")]
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
	}
}