﻿using ElevatorBackend.Data;
using ElevatorBackend.Hubs;
using ElevatorBackend.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

public class ElevatorService
{
    private readonly AppDbContext _dbContext;
    private readonly IHubContext<ElevatorHub> _hubContext;
    private readonly IServiceScopeFactory _scopeFactory;

    public ElevatorService(AppDbContext dbContext, IHubContext<ElevatorHub> hubContext, IServiceScopeFactory scopeFactory)
    {
        _dbContext = dbContext;
        _hubContext = hubContext;
        _scopeFactory = scopeFactory;
    }

    public List<Elevator> GetAll()
    {
        return _dbContext.Elevators.Include(e => e.AllRequests).ToList();
    }

    public Elevator? GetById(int id)
    {
        return _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == id);
    }

    public void AddElevator(Elevator elevator)
    {
        elevator.CurrentFloor = 0;
        elevator.Status = ElevatorStatus.Idle;
        elevator.Direction = Direction.None;
        elevator.DoorStatus = DoorStatus.Closed;

        _dbContext.Elevators.Add(elevator);
        _dbContext.SaveChanges();
    }

    public async Task MoveToFloor(int elevatorId)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var elevator = dbContext.Elevators
            .Include(e => e.AllRequests)
            .FirstOrDefault(e => e.Id == elevatorId);

        if (elevator == null)
            return;
        if (elevator.Status == ElevatorStatus.WaitingForDestination)
        {
            Console.WriteLine($"[SERVER] Elevator {elevator.Id} is waiting for destination. Skipping movement.");
            return;
        }
        int? lastCallId = null;

        Console.WriteLine($"[Check] Requests: {elevator.Requests.Count}, Destination: {elevator.DestinationRequests.Count}");

        while (elevator.Requests.Any() || elevator.DestinationRequests.Any())
        {
            int? targetFloor = elevator.DestinationRequests.Any()
                ? elevator.DestinationRequests.First().Floor
                : elevator.Requests
                    .Where(r => r.Direction == Direction.Up || r.Direction == Direction.Down)
                    .OrderBy(r => Math.Abs(r.Floor - elevator.CurrentFloor)) // Closest
                    .ThenBy(r => r.RequestedAt) // If equal by request time order
                    .Select(r => (int?)r.Floor)
                    .FirstOrDefault();



            // Calculate direction
            if (targetFloor > elevator.CurrentFloor)
            {
                elevator.Direction = Direction.Up;
                elevator.Status = ElevatorStatus.MovingUp;
            }
            else if (targetFloor < elevator.CurrentFloor)
            {
                elevator.Direction = Direction.Down;
                elevator.Status = ElevatorStatus.MovingDown;
            }
            else //elevator already at the target floor
            {
                Console.WriteLine($"[Move] Elevator {elevator.Id} already at floor {elevator.CurrentFloor}");


                lastCallId = dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();




                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });

                await Task.Delay(500);


                // remove the request at the current floor
                // Try to remove both Regular and Destination requests
                var requestToRemove = elevator.AllRequests
                    .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Regular);
                var destinationToRemoveAtArrival = elevator.AllRequests
                    .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Destination);


                if (requestToRemove != null)
                {
                    elevator.AllRequests.Remove(requestToRemove);
                    dbContext.ElevatorRequests.Remove(requestToRemove);
                    elevator.Status = ElevatorStatus.WaitingForDestination;
                    Console.WriteLine($"[114 - SERVER] Elevator {elevator.Id} status → {elevator.Status}");
                }
                else if (destinationToRemoveAtArrival != null)
                {
                    elevator.AllRequests.Remove(destinationToRemoveAtArrival);
                    dbContext.ElevatorRequests.Remove(destinationToRemoveAtArrival);
                    elevator.Status = ElevatorStatus.Idle;
                    elevator.Direction = Direction.None;
                    elevator.DoorStatus = DoorStatus.Closed;
                }

                Console.WriteLine($"[125-SERVER] Elevator {elevator.Id} status → {elevator.Status}");

                await Task.Delay(1500);

                await dbContext.SaveChangesAsync();

                lastCallId = dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();


                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });

                break; // Wait for destination input from the passenger
            }

            // Actual movement
            elevator.CurrentFloor += elevator.Direction == Direction.Up ? 1 : -1;

            // // Check if someone wants to enter at this floor, based on the current direction

            var pickupRequest = elevator.Requests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Direction == elevator.Direction);

            if (pickupRequest != null)
            {
                Console.WriteLine($"[!!!!] CurrentFloor : {elevator.CurrentFloor} elevator Direction : {elevator.Direction} pickupRequest Direction :{pickupRequest.Direction}");
                Console.WriteLine($"[SERVER] Picking up from floor {elevator.CurrentFloor} on the way");
                elevator.AllRequests.Remove(pickupRequest);
                dbContext.ElevatorRequests.Remove(pickupRequest);
                elevator.Status = ElevatorStatus.WaitingForDestination;


                lastCallId = dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();

                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId 
                });

                Console.WriteLine($"[165- SERVER] Elevator {elevator.Id} status → {elevator.Status}");
                elevator.DoorStatus = DoorStatus.Open;
                await Task.Delay(1000);
                elevator.DoorStatus = DoorStatus.Closed;

                await dbContext.SaveChangesAsync();

                break;
            }


            lastCallId = dbContext.ElevatorCallAssignments
                .Where(a => a.ElevatorId == elevator.Id)
                .OrderByDescending(a => a.AssignmentTime)
                .Select(a => (int?)a.ElevatorCallId)
                .FirstOrDefault();


            await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
            {
                ElevatorId = elevator.Id,
                CurrentFloor = elevator.CurrentFloor,
                Direction = elevator.Direction.ToString(),
                Status = elevator.Status.ToString(),
                DoorStatus = elevator.DoorStatus.ToString(),
                LastCallId = lastCallId
            });

            await Task.Delay(2000);

            bool shouldStop = false;
            bool isDestination = false;

            var regularToRemove = elevator.AllRequests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Regular);

            if (regularToRemove != null)
            {
                elevator.AllRequests.Remove(regularToRemove);
                dbContext.ElevatorRequests.Remove(regularToRemove);
                shouldStop = true;
            }

            var destinationToRemove = elevator.AllRequests
                .FirstOrDefault(r => r.Floor == elevator.CurrentFloor && r.Type == RequestType.Destination);

            if (destinationToRemove != null)
            {
                Console.WriteLine($"[SERVER] Reached Destination Floor {elevator.CurrentFloor}");
                elevator.AllRequests.Remove(destinationToRemove);
                dbContext.ElevatorRequests.Remove(destinationToRemove);
                shouldStop = true;
                isDestination = true;
            }

            if (shouldStop)
            {
                if (isDestination)
                {
                    // Returned from destination, reset to initial state
                    elevator.Status = ElevatorStatus.Idle;
                    elevator.Direction = Direction.None;
                    elevator.DoorStatus = DoorStatus.Closed;
                }

                else
                {

                    // Stop for a call, open doors and wait for destination
                    OpenAndChooseFloor(elevator);
                }

                lastCallId = dbContext.ElevatorCallAssignments
                    .Where(a => a.ElevatorId == elevator.Id)
                    .OrderByDescending(a => a.AssignmentTime)
                    .Select(a => (int?)a.ElevatorCallId)
                    .FirstOrDefault();


                await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
                {
                    ElevatorId = elevator.Id,
                    CurrentFloor = elevator.CurrentFloor,
                    Direction = elevator.Direction.ToString(),
                    Status = elevator.Status.ToString(),
                    DoorStatus = elevator.DoorStatus.ToString(),
                    LastCallId = lastCallId
                });
            }
            await Task.Delay(1500);
            await dbContext.SaveChangesAsync();
        }

        if (elevator.Status != ElevatorStatus.WaitingForDestination)
        {
            elevator.Status = ElevatorStatus.Idle;
            elevator.Direction = Direction.None;
            await dbContext.SaveChangesAsync();
        }
        Console.WriteLine($"[SignalR] Sending Elevator Update: ID={elevator.Id}, Status={elevator.Status}, StatusStr={elevator.Status.ToString()}");
        Console.WriteLine($"[SERVER] Final status for elevator {elevator.Id}: {elevator.Status}");

        lastCallId = dbContext.ElevatorCallAssignments
           .Where(a => a.ElevatorId == elevator.Id)
           .OrderByDescending(a => a.AssignmentTime)
           .Select(a => (int?)a.ElevatorCallId)
           .FirstOrDefault();


        await _hubContext.Clients.All.SendAsync("ElevatorUpdated", new ElevatorStatusDto
        {

            ElevatorId = elevator.Id,
            CurrentFloor = elevator.CurrentFloor,
            Direction = elevator.Direction.ToString(),
            Status = elevator.Status.ToString(),
            DoorStatus = elevator.DoorStatus.ToString(),
            LastCallId = lastCallId
        });
    }


    private void OpenAndChooseFloor(Elevator elevator)
    {
        elevator.Status = ElevatorStatus.OpeningDoors;
        elevator.DoorStatus = DoorStatus.Open;
        Thread.Sleep(3000);

        elevator.Status = ElevatorStatus.WaitingForDestination;
        Console.WriteLine($"[291-SERVER] Elevator {elevator.Id} status → WaitingForDestination");



    }

    public void RequestElevator(int elevatorId, int floor)
    {
        var elevator = _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == elevatorId);
        if (elevator == null) return;

        var direction = floor > elevator.CurrentFloor ? Direction.Up :
                        floor < elevator.CurrentFloor ? Direction.Down : Direction.None;

        // Check if elevator can handle the request now
        if (elevator.Status == ElevatorStatus.Idle ||
            (elevator.Direction == Direction.Up && floor >= elevator.CurrentFloor) ||
            (elevator.Direction == Direction.Down && floor <= elevator.CurrentFloor))
        {
            if (!elevator.Requests.Any(r => r.Floor == floor))
            {
                elevator.AddRequest(new ElevatorRequest
                {
                    Floor = floor,
                    Direction = direction,
                    Type = RequestType.Regular
                });
            }
        }
        else
        {
            
        }

        _dbContext.SaveChanges();

        if (elevator.Status == ElevatorStatus.Idle)
        {
            _ = Task.Run(() => MoveToFloor(elevatorId));
        }
    }

    public void SelectFloor(int elevatorId, int floor)
    {
        var elevator = _dbContext.Elevators.Include(e => e.AllRequests).FirstOrDefault(e => e.Id == elevatorId);
        if (elevator == null) return;

        if (!elevator.DestinationRequests.Any(r => r.Floor == floor))
        {
            var direction = floor > elevator.CurrentFloor ? Direction.Up :
                            floor < elevator.CurrentFloor ? Direction.Down : Direction.None;

            elevator.AddRequest(new ElevatorRequest
            {
                Floor = floor,
                Direction = direction,
                Type = RequestType.Destination
            });
        }

        _dbContext.SaveChanges();

        if (elevator.Status == ElevatorStatus.Idle)
        {
            _ = Task.Run(() => MoveToFloor(elevatorId));
        }

    }

    public List<Elevator> GetElevatorsWithRequests()
    {
        return _dbContext.Elevators
            .Include(e => e.AllRequests)
            .ToList()
            .Where(e => e.Requests.Any() || e.DestinationRequests.Any())
            .ToList();
    }
}