using System.Collections.Generic;
using Godot;

namespace RailConductor;

public partial class ScheduleManager : Node
{
    [Export] private Godot.Collections.Array<Schedule> _initSchedules = [];

    private Dictionary<string, Schedule> _schedules = new();
    private Dictionary<string, ScheduleTrip> _trips = new();
    private Dictionary<string, ScheduleTimePoint> _timePoints = new();

    public override void _Ready()
    {
        foreach (var schedule in _initSchedules)
        {
            RegisterSchedule(schedule);
        }
    }

    public void RegisterSchedule(Schedule schedule)
    {
        _schedules[schedule.Id] = schedule;
        foreach (var trip in schedule.Trips)
        {
            _trips[trip.Id] = trip;
            foreach (var timePoint in trip.TimePoints)
            {
                _timePoints[timePoint.Id] = timePoint;
            }
        }
    }

    public void UnregisterSchedule(Schedule schedule)
    {
        _schedules.Remove(schedule.Id);
        foreach (var trip in schedule.Trips)
        {
            _trips.Remove(trip.Id);
            foreach (var timePoint in trip.TimePoints)
            {
                _timePoints.Remove(timePoint.Id);
            }
        }
    }
}