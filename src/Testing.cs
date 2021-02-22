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
    // TODO or FIXME - Set which parts you want to run.
    public static bool PrintSchedules = true;
    public static string GTFSFilename = "ddot_gtfs.zip";
    // public static bool PrintStopLists = true; // this one doesn't do anything atm

    static void Main(string[] args)
    {
      var reader = new GTFSReader<GTFSFeed>();
      GTFSFeed feed = reader.Read("gtfs/" + GTFSFilename);

      TimepointStrategy strat = TimepointFinder.GetTimepointStrategy(feed);

      var dateRange = CalendarTester.GetFeedDateRange(feed);

      var allUsedServices =
        from trips in feed.Trips
        group trips by trips.ServiceId into narrowTrips
        let servId = narrowTrips.First().ServiceId
        select new
        {
          Key = servId,
          Value = CalendarTester.GetDescription(feed, servId, dateRange)
        };

      Dictionary<string, string> serviceDescriptions = allUsedServices.ToDictionary(x => x.Key, x => x.Value);

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

        if (PrintSchedules)
        {
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
              output.WriteLine("## Service " + service);
              output.WriteLine("*" + serviceDescriptions[service] + "*");
              output.WriteLine();

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
                  tripLine += "|" + (tripTimes.ContainsKey(stop) ? TimeOfDay.FromTotalSeconds(tripTimes[stop].TotalSeconds % 86400).ToString() : "");
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
      }
    }
  }
}