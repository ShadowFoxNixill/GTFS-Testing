using System;
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
      GTFSFeed feed = reader.Read("gtfs/ddot_gtfs.zip");

      TimepointStrategy strat = TimepointFinder.GetTimepointStrategy(feed);

      foreach (Route route in feed.Routes)
      {
        // What direction(s) does the route run?
        IEnumerable<DirectionType?> dirs =
          from trips in feed.Trips
          where trips.RouteId == route.Id
          group trips by trips.Direction into narrowTrips
          select narrowTrips.First().Direction;

        // What service(s) does the route run on?
        IEnumerable<string> services =
          from trips in feed.Trips
          where trips.RouteId == route.Id
          group trips by trips.ServiceId into narrowTrips
          select narrowTrips.First().ServiceId;

        using StreamWriter output = new StreamWriter($"output/schedules/{route.Id}.md");

        foreach (DirectionType? dir in dirs)
        {
          output.WriteLine("# " + dir switch
          {
            DirectionType.OneDirection => "Main Direction",
            DirectionType.OppositeDirection => "Opposite Direction",
            _ => "No Direction"
          });

          var stops = ScheduleBuilder.GetScheduleHeader(feed, route.Id, dir, strat);
          var times = ScheduleBuilder.GetSortTimes(feed, route.Id, dir, stops);

          foreach (string service in services)
          {
            output.WriteLine("## " + service);

            var schedule = ScheduleBuilder.GetSchedule(feed, route.Id, dir, service, stops, times);

            string stopLine = "Trip ID";
            string alignLine = ":-";

            foreach (string stop in stops)
            {
              stopLine += "|" + feed.Stops.Get(stop).Name;
              alignLine += "|:-";
            }

            output.WriteLine(stopLine);
            output.WriteLine(alignLine);

            var trips = schedule.Item2;

            foreach (var trip in trips)
            {
              string tripLine = trip.Item1;
              var tripTimes = trip.Item2;

              foreach (string stop in stops)
              {
                tripLine += "|" + (tripTimes.ContainsKey(stop) ? tripTimes[stop].ToString() : "");
              }

              output.WriteLine(tripLine);
            }

            output.WriteLine();

            output.Flush();
          }

          output.WriteLine();
          output.Flush();
        }
      }

      // Stops with no service
      var unservedStops =
        from stops in feed.Stops
        join stopTimes in feed.StopTimes on stops.Id equals stopTimes.StopId into stopsWithTimes
        from allStops in stopsWithTimes.DefaultIfEmpty()
        where allStops?.TripId == null
        select stops.Name;

      using StreamWriter outputFile = new StreamWriter("output/stoplisting/unserved.txt");

      foreach (string stop in unservedStops)
      {
        outputFile.WriteLine(stop);
      }
      outputFile.Flush();
    }
  }
}