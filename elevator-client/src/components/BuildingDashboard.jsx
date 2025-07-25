// Updated UI labels to English and fixed destination input visibility
import { useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import axios from 'axios';

function BuildingDashboard({ building, onBack }) {
  const [elevators, setElevators] = useState([]);
  const [connection, setConnection] = useState(null);
  const [error, setError] = useState('');
  const [destinationMap, setDestinationMap] = useState({});

  const ElevatorStatusMap = {
    0: 'Idle',
    1: 'MovingUp',
    2: 'MovingDown',
    3: 'OpeningDoors',
    4: 'ClosingDoors',
    5: 'Waiting for Destination',
  };

  const DirectionMap = {
    0: 'None',
    1: 'Up',
    2: 'Down',
  };

  useEffect(() => {
    const newConnection = new HubConnectionBuilder()
      .withUrl('https://localhost:5001/elevatorHub')
      .withAutomaticReconnect()
      .build();

    setConnection(newConnection);
  }, []);

  useEffect(() => {
    if (!connection) return;

    connection
      .start()
      .then(() => {
        console.log('SignalR connected');

        connection.on('ElevatorUpdated', (updatedElevator) => {
          console.log("Direction from server:", updatedElevator.direction);

          setElevators((prev) =>
            prev.map((e) =>
              e.id === updatedElevator.elevatorId
                ? { ...e, ...updatedElevator }
                : e
            )
          );
        });

        const loadElevators = async () => {
          try {
            const response = await axios.get(
              `https://localhost:5001/api/Elevator/by-building/${building.id}`,
              { withCredentials: true }
            );
            setElevators(response.data?.$values || []);
          } catch (err) {
            setError('Failed to load elevators.');
          }
        };

        loadElevators();
      })
      .catch((err) => {
        setError('Failed to connect to elevator system.');
      });

    return () => {
      connection.stop();
    };
  }, [connection, building.id]);

  const requestElevator = async (floorNumber, direction) => {
    try {
      await axios.post('https://localhost:5001/api/ElevatorCall', {
        buildingId: building.id,
        requestedFloor: floorNumber,
        destinationFloor: floorNumber,
        direction: direction
      });
    } catch (err) {
      setError('Failed to call elevator.');
    }
  };

  const handleDestinationSelect = async (elevatorCallId, floor, elevatorId) => {
    try {
      await axios.put(
        `https://localhost:5001/api/ElevatorCall/${elevatorCallId}/selectDestination`,
        JSON.stringify(floor),
        {
          headers: { 'Content-Type': 'application/json' }
        }
      );

      setDestinationMap((prev) => ({ ...prev, [elevatorId]: '' }));
    } catch (err) {
      setError('Failed to select destination.');
    }
  };

  return (
    <div style={{ padding: '20px' }}>
      <button onClick={onBack}>⬅ Back to buildings</button>
      <h2>Building: {building.name}</h2>
      <p>Total Floors: {building.numberOfFloors}</p>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <div style={{ width: '600px', margin: 'auto' }}>
        {[...Array(building.numberOfFloors)].map((_, i) => {
          const floor = building.numberOfFloors - i - 1;
          const elevatorsOnFloor = elevators.filter(
            (e) => e.currentFloor === floor
          );

          return (
            <div
              key={floor}
              style={{
                display: 'flex',
                justifyContent: 'space-between',
                alignItems: 'center',
                padding: '12px 20px',
                marginBottom: '12px',
                backgroundColor: '#f5f5f5',
                border: '2px solid black',
                borderRadius: '10px',
              }}
            >
              <div>
                <p style={{ margin: '4px 0', fontSize: '18px' }}>Floor {floor}</p>
                <div style={{ display: 'flex', gap: '6px' }}>
                  <button onClick={() => requestElevator(floor, 1)}>Up</button>
                  <button onClick={() => requestElevator(floor, 2)}>Down</button>
                </div>
              </div>

              <div style={{ display: 'flex', gap: '10px' }}>
                {elevatorsOnFloor.map((elevator) => (
                  <div
                    key={elevator.id}
                    style={{
                      backgroundColor: '#2196f3',
                      color: 'white',
                      padding: '8px 10px',
                      borderRadius: '6px',
                      minWidth: '140px',
                      textAlign: 'center',
                      fontSize: '12px',
                    }}
                  >
<div><strong>ID:</strong> {elevator.id}</div>
<div><strong>Floor:</strong> {elevator.currentFloor}</div>
<div><strong>Status:</strong> {elevator.status}</div>
<div><strong>Direction:</strong> {elevator.direction}</div>



{elevator.status === 'WaitingForDestination' && elevator.lastCallId && (
  <div style={{ marginTop: '6px' }}>
    {[...Array(building.numberOfFloors)].map((_, targetFloor) => (
      targetFloor !== elevator.currentFloor &&
      ((elevator.direction === 'Up' && targetFloor > elevator.currentFloor) ||
        (elevator.direction === 'Down' && targetFloor < elevator.currentFloor)) && (
        <button
          key={targetFloor}
          style={{ margin: '2px', fontSize: '12px' }}
          onClick={() =>
            handleDestinationSelect(
              elevator.lastCallId,
              targetFloor,
              elevator.id
            )
          }
        >
          {targetFloor}
        </button>
                          )
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

export default BuildingDashboard;
