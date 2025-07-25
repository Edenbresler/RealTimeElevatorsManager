import { useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import axios from 'axios';

function BuildingDashboard({ building, onBack }) {
  const [elevators, setElevators] = useState([]);
  const [connection, setConnection] = useState(null);
  const [error, setError] = useState('');

  const ElevatorStatusMap = {
    0: 'Idle',
    1: 'Moving',
    2: 'DoorOpen',
    3: 'Waiting',
    4: 'ClosingDoors',
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

        connection.on('ElevatorStatusUpdated', (updatedElevator) => {
          setElevators((prev) =>
            prev.map((e) => (e.id === updatedElevator.id ? updatedElevator : e))
          );
        });

        const loadElevators = async () => {
          try {
            const response = await axios.get(
              `https://localhost:5001/api/Elevator/by-building/${building.id}`,
              { withCredentials: true }
            );
            console.log('Elevator response:', response.data);
            setElevators(response.data?.$values || []);
          } catch (err) {
            console.error('Failed to load elevators', err);
            setError('Failed to load elevators.');
          }
        };

        loadElevators();
      })
      .catch((err) => {
        console.error('SignalR connection failed:', err);
        setError('Failed to connect to elevator system.');
      });

    return () => {
      console.log('SignalR disconnected');
      connection.stop();
    };
  }, [connection, building.id]);

  const requestElevator = async (floorNumber) => {
    try {
      await axios.post('https://localhost:5001/api/ElevatorCall', {
        buildingId: building.id,
        floor: floorNumber,
        destinationFloor: floorNumber 
      });
    } catch (err) {
      setError('Failed to call elevator.');
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
          const floor = building.numberOfFloors - i;
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
                <button onClick={() => requestElevator(floor)}>Call</button>
              </div>

              <div style={{ display: 'flex', gap: '10px' }}>
                {elevators
                  .filter((e) => e.currentFloor === floor)
                  .map((elevator) => (
                    <div
                      key={elevator.id}
                      style={{
                        backgroundColor: '#2196f3',
                        color: 'white',
                        padding: '8px 10px',
                        borderRadius: '6px',
                        minWidth: '90px',
                        textAlign: 'center',
                        fontSize: '12px',
                      }}
                    >
                      <div><strong>ID:</strong> {elevator.id}</div>
                      <div>{ElevatorStatusMap[elevator.status]}</div>
                      <div>{DirectionMap[elevator.direction]}</div>
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
