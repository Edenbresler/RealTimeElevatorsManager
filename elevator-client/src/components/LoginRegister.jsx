import { useState } from 'react';
import axios from 'axios';

function LoginRegister({ onLogin }) {
  const [mode, setMode] = useState('login'); 
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');

  const handleSubmit = async () => {
    try {
      const endpoint =
        mode === 'login'
          ? 'https://localhost:5001/api/User/login'
          : 'https://localhost:5001/api/User/register';

        const response = await axios.post(
          endpoint,
          {
            Email: email,
            Password: password,
          },
          {
            withCredentials: true, 
          }
        );
        console.log(response.data);

    if (response.status === 200) {
      setError('');

      if (mode === 'login') {
      
        onLogin({
          id: response.data.userId,
          username: response.data.email,
        });
      } else {
     
        setMode('login');
        setError('Registration successful! Please login.');
      }
    }
  } catch (err) {
    setError('Login/Register failed. Please try again.');
  }
};

  return (
    <div style={{ maxWidth: '300px', margin: '50px auto', textAlign: 'center' }}>
      <h2>{mode === 'login' ? 'Login' : 'Register'}</h2>

      <input
        type="text"
        placeholder="Email"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
        style={{ width: '100%', marginBottom: '10px', padding: '8px' }}
      />

      <input
        type="password"
        placeholder="Password"
        value={password}
        onChange={(e) => setPassword(e.target.value)}
        style={{ width: '100%', marginBottom: '10px', padding: '8px' }}
      />

      <button onClick={handleSubmit} style={{ width: '100%', padding: '10px' }}>
        {mode === 'login' ? 'Login' : 'Register'}
      </button>

      <p style={{ marginTop: '10px' }}>
        {mode === 'login' ? "Don't have an account?" : 'Already have an account?'}{' '}
        <span
          style={{ color: 'blue', cursor: 'pointer', textDecoration: 'underline' }}
          onClick={() => {
            setError('');
            setMode(mode === 'login' ? 'register' : 'login');
          }}
        >
          {mode === 'login' ? 'Register here' : 'Login here'}
        </span>
      </p>

      {error && <p style={{ color: 'red' }}>{error}</p>}
    </div>
  );
}

export default LoginRegister;
