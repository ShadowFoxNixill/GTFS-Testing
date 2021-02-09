using GTFS;
using GTFS.Entities;
using System.Collections.Generic;
using System.Linq;

namespace Nixill.GTFSTesting
{
  public class Functions
  {
    public static string RunningDays(GTFSFeed feed, Route route)
    {
      // First, the list of service IDs under which the trip runs
      var serviceIds =
        from trips in feed.Trips
        where trips.RouteId == route.Id
        select trips.ServiceId;

      if (serviceIds.Count() == 0)
      {
        return "Route does not run.";
      }

      // Now get the ranges and days of the week the route runs
      var calendars =
        from calendar in feed.Calendars
        where serviceIds.Contains(calendar.ServiceId)
        select calendar;

      var calendarRange = new
      {
        StartDate = calendars.Min(x => x.StartDate),
        EndDate = calendars.Max(x => x.EndDate),
        Monday = calendars.Any(x => x.Monday),
        Tuesday = calendars.Any(x => x.Tuesday),
        Wednesday = calendars.Any(x => x.Wednesday),
        Thursday = calendars.Any(x => x.Thursday),
        Friday = calendars.Any(x => x.Friday),
        Saturday = calendars.Any(x => x.Saturday),
        Sunday = calendars.Any(x => x.Sunday),
        Mask = calendars.Aggregate(0, (mask, cal) => mask | cal.Mask)
      };

      // Translate it to a string

      var calendarDates =
        from calendar_dates in feed.CalendarDates
        where serviceIds.Contains(calendar_dates.ServiceId)
        select calendar_dates;

      var calendarChanges =
        from distinct_calendar_dates in (
          from calendar_dates in calendarDates
          group calendar_dates by calendar_dates.Date into combined_dates
          where combined_dates.Count() == 1
          select combined_dates.First()
        ).ToList()
        group distinct_calendar_dates by distinct_calendar_dates.ExceptionType into exception_count
        select new
        {
          ExceptionType = exception_count.First().ExceptionType,
          Count = exception_count.Count()
        };

      return null;
    }
  }
}