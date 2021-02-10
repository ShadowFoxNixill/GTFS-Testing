using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTFS;
using GTFS.Entities;
using GTFS.Entities.Enumerations;

namespace Nixill.GTFS
{
  public class Testing
  {
    static void Main(string[] args)
    {
      var reader = new GTFSReader<GTFSFeed>();
      GTFSFeed feed = reader.Read("gtfs/transperth_gtfs.zip");

      foreach (Route route in feed.Routes)
      {
        // What direction(s) does the route run?
        IEnumerable<DirectionType?> dirs =
          from trips in feed.Trips
          where trips.RouteId == route.Id
          group trips by trips.Direction into narrowTrips
          select narrowTrips.First().Direction;

        foreach (DirectionType? dir in dirs)
        {
          string dirSuffix = dir switch
          {
            DirectionType.OneDirection => "+",
            DirectionType.OppositeDirection => "-",
            _ => "×"
          };
          using StreamWriter output = new StreamWriter("output/stoplisting/" + route.Id + dirSuffix + ".txt");

          List<Stop> stops = Functions.GetStopOrder(feed, route, dir);

          foreach (Stop stop in stops)
          {
            output.WriteLine(stop.Name);
          }

          output.Flush();
        }
      }
    }
  }
}