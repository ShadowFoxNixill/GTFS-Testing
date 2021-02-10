using System.Collections.Generic;
using System.IO;
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
      GTFSFeed feed = reader.Read("gtfs/smart_gtfs_2018-01.zip");

      using StreamWriter output = new StreamWriter("output/330Stops.txt");

      List<Stop> stops = Functions.GetStopOrder(feed, feed.Routes.Get("330"), DirectionType.OppositeDirection);

      foreach (Stop stop in stops)
      {
        output.WriteLine(stop.Name);
      }

      output.Flush();
      output.Dispose();
    }
  }
}