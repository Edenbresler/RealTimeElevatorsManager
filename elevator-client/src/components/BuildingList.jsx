import { useEffect, useState } from 'react';
import axios from 'axios';

function BuildingList({ userId, onSelectBuilding }) {
  const [buildings, setBuildings] = useState([]);
  const [error, setError] = useState('');
  const [newBuildingName, setNewBuildingName] = useState('');
  const [newBuildingFloors, setNewBuildingFloors] = useState('');

  useEffect(() => {
    const fetchBuildings = async () => {
      
      try {
        

        const response = await axios.get(
          `https://localhost:5001/api/Building/user/${userId}`
        );
        setBuildings(response.data.$values || []);
      } catch (err) {
        setError('');
      }
    };

    if (userId) {
      fetchBuildings();
    }
  }, [userId]);

  const handleAddBuilding = async () => {
    if (!newBuildingName || !newBuildingFloors) {
      setError('Please enter building name and number of floors.');
      return;
    }

    try {
      console.log("Sending building with userId:", userId);

      const response = await axios.post(
        `https://localhost:5001/api/Building`,
        {
          name: newBuildingName,
          numberOfFloors: parseInt(newBuildingFloors),
          userId: userId,
        }
      );

      // עדכון הרשימה לאחר ההוספה
      setBuildings((prev) => [...prev, response.data]);
      setNewBuildingName('');
      setNewBuildingFloors('');
      setError('');
    } catch (err) {
      setError('Failed to add building. Please try again.');
    }
  };

  return (
    <div style={{ maxWidth: '400px', margin: '40px auto', textAlign: 'center' }}>
      <h2>Your Buildings</h2>

      {error && <p style={{ color: 'red' }}>{error}</p>}

      {/* טופס להוספת בניין */}
      <div style={{ marginBottom: '30px', border: '1px solid gray', padding: '15px' }}>
        <h3>Add New Building</h3>
        <input
          type="text"
          placeholder="Building name"
          value={newBuildingName}
          onChange={(e) => setNewBuildingName(e.target.value)}
          style={{ marginBottom: '10px', width: '100%', padding: '5px' }}
        />
        <input
          type="number"
          placeholder="Number of floors"
          value={newBuildingFloors}
          onChange={(e) => setNewBuildingFloors(e.target.value)}
          style={{ marginBottom: '10px', width: '100%', padding: '5px' }}
        />
        <button onClick={handleAddBuilding}>Add Building</button>
      </div>

      {/* רשימת בניינים */}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {buildings.map((building) => (
          <li key={building.id} style={{ marginBottom: '10px', border: '1px solid gray', padding: '10px' }}>
            <h3>{building.name}</h3>
            <button onClick={() => onSelectBuilding(building)}>Enter</button>
          </li>
        ))}
      </ul>
    </div>
  );
}

export default BuildingList;
