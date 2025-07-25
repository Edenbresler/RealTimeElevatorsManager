import { useState } from 'react';
import LoginRegister from './components/LoginRegister';
import BuildingList from './components/BuildingList';
import BuildingDashboard from './components/BuildingDashboard';

function App() {
  const [user, setUser] = useState(null);
  const [selectedBuilding, setSelectedBuilding] = useState(null);

  if (!user) {
    return <LoginRegister onLogin={setUser} />;
  }

  if (selectedBuilding) {
    return (
      <BuildingDashboard
        building={selectedBuilding}
        onBack={() => setSelectedBuilding(null)}
      />
    );
  }

  return (
    <div>
      <h1>Welcome, {user.username}!</h1>
      <BuildingList userId={user.id} onSelectBuilding={setSelectedBuilding}
      onLogout={() => setUser(null)}  />
    </div>
  );
}

export default App;